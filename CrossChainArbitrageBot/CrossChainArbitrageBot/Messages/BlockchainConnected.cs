using Agents.Net;
using Nethereum.Web3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossChainArbitrageBot.Messages
{
    internal class BlockchainConnected : Message
    {
        public BlockchainConnected(BlockchainName blockchainName, Web3 connection, Dictionary<string, string> abis) : base(Enumerable.Empty<Message>())
        {
            BlockchainName = blockchainName;
            Connection = connection;
            Abis = abis;
        }

        public BlockchainName BlockchainName { get; }
        public Web3 Connection { get; }
        public Dictionary<string, string> Abis { get; }

        protected override string DataToString()
        {
            return $"{nameof(BlockchainName)}: {BlockchainName}; {nameof(Abis)}: {string.Join(", ", Abis.Keys)}";
        }
    }

    public enum BlockchainName
    {
        Bsc,
        Avalanche
    }
}
