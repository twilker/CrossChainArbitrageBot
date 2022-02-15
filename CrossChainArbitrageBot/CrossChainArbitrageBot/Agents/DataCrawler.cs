using Agents.Net;
using CrossChainArbitrageBot.Messages;
using CrossChainArbitrageBot.Models;
using Fractions;
using Moralis.Web3Api;
using Moralis.Web3Api.Models;
using Nethereum.Contracts.Services;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

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

            Timer updateTimer = new(3000) { AutoReset = false };
            AddDisposable(updateTimer);

            List<DataUpdatePackage> packages = new();
            foreach(BlockchainConnection connection in connected.Connections)
            {
                string unstableId;
                string stableId;
                switch (connection.BlockchainName)
                {
                    case BlockchainName.Bsc:
                        unstableId = ConfigurationManager.AppSettings["BscUnstableCoinId"]??throw new ConfigurationErrorsException("BscUnstableCoinId not found");
                        stableId = ConfigurationManager.AppSettings["BscStableCoinId"]??throw new ConfigurationErrorsException("BscStableCoinId not found");
                        break;
                    case BlockchainName.Avalanche:
                        unstableId = ConfigurationManager.AppSettings["AvalancheUnstableCoinId"] ?? throw new ConfigurationErrorsException("AvalancheUnstableCoinId not found");
                        stableId = ConfigurationManager.AppSettings["AvalancheStableCoinId"] ?? throw new ConfigurationErrorsException("AvalancheStableCoinId not found");
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

                packages.Add(new DataUpdatePackage(connection.BlockchainName, unstableId, unstableCoin, stableId, stableCoin,
                                                   connection.Connection.Eth, connection.Abis["Erc20"],
                                                   connection.Connection.TransactionManager.Account.Address));
            }

            updateTimer.Elapsed += UpdateTimerOnElapsed;
            updateTimer.Start();
            
            void UpdateTimerOnElapsed(object? sender, ElapsedEventArgs e)
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

                    Task<BigInteger> balanceCall = updatePackage.ContractService.GetContract(updatePackage.TokenAbi,
                                                                     updatePackage.UnstableCoinId)
                                                                .GetFunction("balanceOf")
                                                                .CallAsync<BigInteger>(updatePackage.WalletAddress);
                    balanceCall.Wait();
                    decimal unstableAmount = Web3.Convert.FromWei(balanceCall.Result);
                    
                    balanceCall = updatePackage.ContractService.GetContract(updatePackage.TokenAbi,
                                                                            updatePackage.StableCoinId)
                                               .GetFunction("balanceOf")
                                               .CallAsync<BigInteger>(updatePackage.WalletAddress);
                    balanceCall.Wait();
                    decimal stableAmount = Web3.Convert.FromWei(balanceCall.Result);
                    dataUpdates.Add(new DataUpdate(updatePackage.BlockchainName, (double)(unstablePrice.UsdPrice ?? 0),
                                                   (double)unstableAmount, updatePackage.UnstableCoinSymbol,
                                                   (double)stableAmount, updatePackage.StableCoinSymbol));
                }
                
                OnMessage(new DataUpdated(messageData, dataUpdates.ToArray()));
                
                updateTimer.Start();
            }
        }

        public readonly record struct DataUpdatePackage(BlockchainName BlockchainName, string UnstableCoinId, string UnstableCoinSymbol, 
                                                        string StableCoinId, string StableCoinSymbol, 
                                                        IEthApiContractService ContractService, string TokenAbi,
                                                        string WalletAddress);
    }
}
