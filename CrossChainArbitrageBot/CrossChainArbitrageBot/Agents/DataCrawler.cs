using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Agents.Net;
using CrossChainArbitrageBot.Messages;
using CrossChainArbitrageBot.Models;
using Moralis.Web3Api;
using Moralis.Web3Api.Models;
using Nethereum.Contracts.Services;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using Log = Serilog.Log;

namespace CrossChainArbitrageBot.Agents
{
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
                    List<DataUpdate> dataUpdates = new();
                    foreach (DataUpdatePackage updatePackage in packages)
                    {
                        ChainList chain = updatePackage.BlockchainName switch
                        {
                            BlockchainName.Bsc => ChainList.bsc,
                            BlockchainName.Avalanche => ChainList.avalanche,
                            _ => throw new InvalidOperationException("Not implemented.")
                        };
                        Erc20Price unstablePrice = MoralisClient.Web3Api.Token.GetTokenPrice(updatePackage.UnstableCoinId,
                            chain);
                        
                        Erc20Price nativePriceInfo = MoralisClient.Web3Api.Token.GetTokenPrice(updatePackage.NativeCoinId,
                            chain);
                        double nativePrice = (double)(nativePriceInfo.UsdPrice ?? 0);

                        Liquidity liquidity = GetLiquidity(updatePackage, nativePrice);

                        Task<BigInteger> balanceCall = updatePackage.ContractService.GetContract(updatePackage.TokenAbi,
                                                                         updatePackage.UnstableCoinId)
                                                                    .GetFunction("balanceOf")
                                                                    .CallAsync<BigInteger>(updatePackage.WalletAddress);
                        balanceCall.Wait();
                        decimal unstableAmount = Web3.Convert.FromWei(balanceCall.Result, updatePackage.UnstableDecimals);
                    
                        balanceCall = updatePackage.ContractService.GetContract(updatePackage.TokenAbi,
                                                        updatePackage.StableCoinId)
                                                   .GetFunction("balanceOf")
                                                   .CallAsync<BigInteger>(updatePackage.WalletAddress);
                        balanceCall.Wait();
                        decimal stableAmount = Web3.Convert.FromWei(balanceCall.Result, updatePackage.StableDecimals);

                        Task<HexBigInteger> accountBalanceCall = updatePackage.ContractService.GetBalance.SendRequestAsync(updatePackage.WalletAddress);
                        accountBalanceCall.Wait();
                        decimal accountBalance = Web3.Convert.FromWei(accountBalanceCall.Result.Value);
                        dataUpdates.Add(new DataUpdate(updatePackage.BlockchainName,
                                                       (double)(unstablePrice.UsdPrice ?? 0),
                                                       (double)unstableAmount, updatePackage.UnstableCoinSymbol,
                                                       updatePackage.UnstableCoinId,
                                                       updatePackage.UnstableDecimals,
                                                       liquidity,
                                                       (double)stableAmount, updatePackage.StableCoinSymbol,
                                                       updatePackage.StableCoinId,
                                                       updatePackage.StableDecimals,
                                                       nativePrice,
                                                       (double)accountBalance,
                                                       updatePackage.WalletAddress));
                    }
                
                    OnMessage(new DataUpdated(messageData, dataUpdates.ToArray()));
                    Thread.Sleep(1000);
                }
                catch (Exception e)
                {
                    Log.Warning(e, $"Error while crawling data {e}");
                }
            }
        }

        private Liquidity GetLiquidity(DataUpdatePackage updatePackage, double nativePrice)
        {
            bool isNativePair;
            string pairAddress;
            switch (updatePackage.BlockchainName)
            {
                case BlockchainName.Bsc:
                    if (ConfigurationManager.AppSettings.AllKeys.Contains("BscUnstableBNBPairId"))
                    {
                        pairAddress = ConfigurationManager.AppSettings["BscUnstableBNBPairId"] ?? string.Empty;
                        isNativePair = true;
                    }
                    else
                    {
                        return new Liquidity();
                    }
                    break;
                case BlockchainName.Avalanche:
                    if (ConfigurationManager.AppSettings.AllKeys.Contains("AvalancheUnstableAVAXPairId"))
                    {
                        pairAddress = ConfigurationManager.AppSettings["AvalancheUnstableAVAXPairId"] ?? string.Empty;
                        isNativePair = true;
                    }
                    else
                    {
                        return new Liquidity();
                    }
                    break;
                default:
                    throw new InvalidOperationException("Not Implemented");
            }
            
            Task<Reserves>? reservesCall = updatePackage.ContractService.GetContract(updatePackage.PairAbi,
                                                                                     pairAddress)
                                                        .GetFunction("getReserves")
                                                        .CallDeserializingToObjectAsync<Reserves>();
            if (reservesCall == null)
            {
                throw new InvalidOperationException("Reserves call not found.");
            }
            reservesCall.Wait();
            
            Task<string>? token0Call = updatePackage.ContractService.GetContract(updatePackage.PairAbi,
                                                             pairAddress)
                                                        .GetFunction("token0")
                                                        .CallAsync<string>();
            if (token0Call == null)
            {
                throw new InvalidOperationException("Token0 reserves call not found.");
            }
            token0Call.Wait();

            BigInteger tokenReserve =
                token0Call.Result.Equals(updatePackage.UnstableCoinId, StringComparison.OrdinalIgnoreCase)
                    ? reservesCall.Result.Reserve0
                    : reservesCall.Result.Reserve1;
            
            BigInteger pairedReserve =
                token0Call.Result.Equals(updatePackage.UnstableCoinId, StringComparison.OrdinalIgnoreCase)
                    ? reservesCall.Result.Reserve1
                    : reservesCall.Result.Reserve0;
            
            decimal tokenAmount = Web3.Convert.FromWei(tokenReserve, updatePackage.UnstableDecimals);
            if (isNativePair)
            {
                decimal nativeAmount = Web3.Convert.FromWei(pairedReserve);
                double usdValue = nativePrice * (double)nativeAmount;
                return new Liquidity((double)tokenAmount, usdValue);
            }

            throw new InvalidOperationException("Not implemented.");
        }

        public readonly record struct DataUpdatePackage(BlockchainName BlockchainName,
                                                        string UnstableCoinId, string UnstableCoinSymbol,
                                                        int UnstableDecimals,
                                                        string StableCoinId, string StableCoinSymbol,
                                                        int StableDecimals, string NativeCoinId,
                                                        IEthApiContractService ContractService, string TokenAbi,
                                                        string PairAbi, string WalletAddress);
    }
}
