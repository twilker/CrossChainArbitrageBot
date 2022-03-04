using System.Collections.Concurrent;
using System.Configuration;
using System.Numerics;
using Agents.Net;
using CrossChainArbitrageBot.Base.Messages;
using CrossChainArbitrageBot.Base.Models;
using Moralis.Web3Api;
using Moralis.Web3Api.Models;
using Nethereum.Contracts.Services;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using Log = Serilog.Log;

namespace CrossChainArbitrageBot.Base.Agents;

[Consumes(typeof(BlockchainConnected))]
[Produces(typeof(DataUpdated))]
internal class DataCrawler : Agent
{
    public DataCrawler(IMessageBoard messageBoard) : base(messageBoard)
    {
        MoralisClient.Initialize(true, ConfigurationManager.AppSettings["MoralisApiKey"]);
    }

    protected override void ExecuteCore(Message messageData)
    {
        BlockchainConnected connected = messageData.Get<BlockchainConnected>();

        List<DataUpdatePackage> packages = new();
        foreach(BlockchainConnection connection in connected.Connections)
        {
            string unstableId;
            string stableId;
            string nativeId;
            switch (connection.BlockchainName)
            {
                case BlockchainName.Bsc:
                    unstableId = ConfigurationManager.AppSettings["BscUnstableCoinId"]??throw new ConfigurationErrorsException("BscUnstableCoinId not found");
                    stableId = ConfigurationManager.AppSettings["BscStableCoinId"]??throw new ConfigurationErrorsException("BscStableCoinId not found");
                    nativeId = ConfigurationManager.AppSettings["BscNativeCoinId"]??throw new ConfigurationErrorsException("BscNativeCoinId not found");
                    break;
                case BlockchainName.Avalanche:
                    unstableId = ConfigurationManager.AppSettings["AvalancheUnstableCoinId"] ?? throw new ConfigurationErrorsException("AvalancheUnstableCoinId not found");
                    stableId = ConfigurationManager.AppSettings["AvalancheStableCoinId"] ?? throw new ConfigurationErrorsException("AvalancheStableCoinId not found");
                    nativeId = ConfigurationManager.AppSettings["AvalancheNativeCoinId"] ?? throw new ConfigurationErrorsException("AvalancheNativeCoinId not found");
                    break;
                default:
                    throw new InvalidOperationException("Not implemented.");
            }

            Task<string> symbolCall = connection.Connection.Eth.GetContract(connection.Abis["Erc20"],
                                                                            unstableId)
                                                .GetFunction("symbol").CallAsync<string>();
            symbolCall.Wait();
            string unstableCoin = symbolCall.Result;

            symbolCall = connection.Connection.Eth.GetContract(connection.Abis["Erc20"],
                                                               stableId)
                                   .GetFunction("symbol").CallAsync<string>();
            symbolCall.Wait();
            string stableCoin = symbolCall.Result;
                
            Task<int> decimalsCall = connection.Connection.Eth.GetContract(connection.Abis["Erc20"],
                                                                           unstableId)
                                               .GetFunction("decimals").CallAsync<int>();
            decimalsCall.Wait();
            int unstableDecimals = decimalsCall.Result;
                
            decimalsCall = connection.Connection.Eth.GetContract(connection.Abis["Erc20"],
                                                                 stableId)
                                     .GetFunction("decimals").CallAsync<int>();
            decimalsCall.Wait();
            int stableDecimals = decimalsCall.Result;

            packages.Add(new DataUpdatePackage(connection.BlockchainName, 
                                               unstableId, unstableCoin, unstableDecimals,
                                               stableId, stableCoin, stableDecimals,
                                               nativeId, connection.Connection.Eth, 
                                               connection.Abis["Erc20"], connection.Abis["Pair"],
                                               connection.Connection.TransactionManager.Account.Address));
        }

        Update(packages, messageData);
    }

    private void Update(List<DataUpdatePackage> packages, Message messageData)
    {
        while (true)
        {
            try
            {
                ConcurrentBag<DataUpdate> dataUpdates = new();

                Task.WaitAll(packages.Select(RunUpdate).ToArray());

                async Task RunUpdate(DataUpdatePackage updatePackage)
                {
                    ChainList chain = updatePackage.BlockchainName switch
                    {
                        BlockchainName.Bsc => ChainList.bsc,
                        BlockchainName.Avalanche => ChainList.avalanche,
                        _ => throw new InvalidOperationException("Not implemented.")
                    };

                    Erc20Price nativePriceInfo = MoralisClient.Web3Api.Token.GetTokenPrice(updatePackage.NativeCoinId, chain);
                    double nativePrice = (double)(nativePriceInfo.UsdPrice ?? 0);

                    Liquidity liquidity = await GetLiquidity(updatePackage, nativePrice);
                    double unstablePrice = liquidity.UsdPaired / liquidity.TokenAmount;

                    BigInteger balance = await updatePackage.ContractService.GetContract(updatePackage.TokenAbi, updatePackage.UnstableCoinId)
                                                            .GetFunction("balanceOf")
                                                            .CallAsync<BigInteger>(updatePackage.WalletAddress);
                    decimal unstableAmount = Web3.Convert.FromWei(balance, updatePackage.UnstableDecimals);

                    balance = await updatePackage.ContractService.GetContract(updatePackage.TokenAbi, updatePackage.StableCoinId)
                                                 .GetFunction("balanceOf")
                                                 .CallAsync<BigInteger>(updatePackage.WalletAddress);
                    decimal stableAmount = Web3.Convert.FromWei(balance, updatePackage.StableDecimals);

                    HexBigInteger accountBalanceResult = await updatePackage.ContractService.GetBalance.SendRequestAsync(updatePackage.WalletAddress);
                    decimal accountBalance = Web3.Convert.FromWei(accountBalanceResult);
                        
                    dataUpdates.Add(new DataUpdate(updatePackage.BlockchainName, unstablePrice, (double)unstableAmount, updatePackage.UnstableCoinSymbol, updatePackage.UnstableCoinId, updatePackage.UnstableDecimals, liquidity, (double)stableAmount, updatePackage.StableCoinSymbol, updatePackage.StableCoinId, updatePackage.StableDecimals, nativePrice, (double)accountBalance, updatePackage.WalletAddress));
                }
                
                OnMessage(new DataUpdated(messageData, dataUpdates.ToArray()));
                Thread.Sleep(2000);
            }
            catch (Exception e)
            {
                Log.Warning(e, $"Error while crawling data {e}");
                Thread.Sleep(2000);
            }
        }
    }

    private static async Task<Liquidity> GetLiquidity(DataUpdatePackage updatePackage, double nativePrice)
    {
        string pairAddress = updatePackage.BlockchainName switch
        {
            BlockchainName.Bsc => ConfigurationManager.AppSettings["BscUnstableLiquidityPairId"],
            BlockchainName.Avalanche => ConfigurationManager.AppSettings["AvalancheUnstableLiquidityPairId"],
            _ => throw new InvalidOperationException("Not implemented.")
        } ?? throw new InvalidOperationException("Not all liquidity pairs are configured.");
            
        Reserves reserves = await updatePackage.ContractService.GetContract(updatePackage.PairAbi,
                                                                            pairAddress)
                                               .GetFunction("getReserves")
                                               .CallDeserializingToObjectAsync<Reserves>();
            
        string token0 = await updatePackage.ContractService.GetContract(updatePackage.PairAbi,
                                                                        pairAddress)
                                           .GetFunction("token0")
                                           .CallAsync<string>();
            
        string token1 = await updatePackage.ContractService.GetContract(updatePackage.PairAbi,
                                                                        pairAddress)
                                           .GetFunction("token1")
                                           .CallAsync<string>();

        BigInteger tokenReserve =
            token0.Equals(updatePackage.UnstableCoinId, StringComparison.OrdinalIgnoreCase)
                ? reserves.Reserve0
                : reserves.Reserve1;
            
        BigInteger pairedReserve =
            token0.Equals(updatePackage.UnstableCoinId, StringComparison.OrdinalIgnoreCase)
                ? reserves.Reserve1
                : reserves.Reserve0;

        string pairedTokenId = token0.Equals(updatePackage.UnstableCoinId, StringComparison.OrdinalIgnoreCase)
                                   ? token1
                                   : token0;
            
        decimal tokenAmount = Web3.Convert.FromWei(tokenReserve, updatePackage.UnstableDecimals);
        double usdValue = pairedTokenId.Equals(updatePackage.NativeCoinId, StringComparison.OrdinalIgnoreCase)
                              ? nativePrice * (double)Web3.Convert.FromWei(pairedReserve)
                              : (double)Web3.Convert.FromWei(pairedReserve, updatePackage.StableDecimals);

        return new Liquidity((double)tokenAmount, usdValue, pairedTokenId, updatePackage.UnstableCoinId);
    }

    public readonly record struct DataUpdatePackage(BlockchainName BlockchainName,
                                                    string UnstableCoinId, string UnstableCoinSymbol,
                                                    int UnstableDecimals,
                                                    string StableCoinId, string StableCoinSymbol,
                                                    int StableDecimals, string NativeCoinId,
                                                    IEthApiContractService ContractService, string TokenAbi,
                                                    string PairAbi, string WalletAddress);
}