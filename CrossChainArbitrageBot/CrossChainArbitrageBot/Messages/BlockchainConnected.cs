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
        public BlockchainConnected(params BlockchainConnection[] connections) : base(Enumerable.Empty<Message>())
        {
            Connections = connections;
        }

        public BlockchainConnection[] Connections { get; }

        protected override string DataToString()
        {
            return $"{nameof(Connections)}: {string.Join(", ", Connections.Select(c => c.BlockchainName))}";
        }
    }

    public readonly record struct BlockchainConnection(BlockchainName BlockchainName, Web3 Connection, Dictionary<string, string> Abis);

    public enum BlockchainName
    {
        Bsc,
        Avalanche
    }
}
