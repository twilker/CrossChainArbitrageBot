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
    internal class DataCrawler : Agent
    {
        public DataCrawler(IMessageBoard messageBoard) : base(messageBoard)
        {
        }

        protected override void ExecuteCore(Message messageData)
        {
            BlockchainConnected connected = messageData.Get<BlockchainConnected>();

            Timer updateTimer = new Timer(3000) { AutoReset = false };
            AddDisposable(updateTimer);

            List<DataUpdatePackage> packages = new List<DataUpdatePackage>();
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


            //MoralisClient.Initialize(true, ConfigurationManager.AppSettings["MoralisApiKey"]);
            //Erc20Price price = MoralisClient.Web3Api.Token.GetTokenPrice(ConfigurationManager.AppSettings["BscUnstableCoindId"], 
            //    ChainList.bsc);

            //Task<BigInteger> balanceCall = connected.Connection.Eth.GetContract(connected.Abis["Erc20"],
            //    ConfigurationManager.AppSettings["BscUnstableCoindId"])
            //    .GetFunction("balanceOf").CallAsync<BigInteger>(connected.Connection.TransactionManager.Account.Address);
            //balanceCall.Wait();
            //var amount = Web3.Convert.FromWei(balanceCall.Result);
            //decimal value = price.UsdPrice.Value * amount;
        }

        public readonly record struct DataUpdatePackage(BlockchainName BlockchainName, string UnstableCoinId, string UnstableCoinSymbol, 
                                                        string StableCoinId, string StableCoinSymbol, 
                                                        IEthApiContractService ContractService, string TokenAbi,
                                                        string walletAddress);
    }
}
