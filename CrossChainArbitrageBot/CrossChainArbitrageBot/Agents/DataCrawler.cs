using Agents.Net;
using CrossChainArbitrageBot.Messages;
using CrossChainArbitrageBot.Models;
using Fractions;
using Moralis.Web3Api;
using Moralis.Web3Api.Models;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

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
            Task<string> symbolCall = connected.Connection.Eth.GetContract(connected.Abis["Erc20"],
                ConfigurationManager.AppSettings["BscUnstableCoindId"])
                .GetFunction("symbol").CallAsync<string>();
            symbolCall.Wait();
            string unstableCoin = symbolCall.Result;

            MoralisClient.Initialize(true, ConfigurationManager.AppSettings["MoralisApiKey"]);
            Erc20Price price = MoralisClient.Web3Api.Token.GetTokenPrice(ConfigurationManager.AppSettings["BscUnstableCoindId"], 
                ChainList.bsc);
        }
    }
}
