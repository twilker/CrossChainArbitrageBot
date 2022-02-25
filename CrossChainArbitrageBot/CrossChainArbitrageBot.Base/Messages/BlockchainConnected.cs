using Agents.Net;
using CrossChainArbitrageBot.Base.Models;
using Nethereum.Web3;

namespace CrossChainArbitrageBot.Base.Messages;

public class BlockchainConnected : Message
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