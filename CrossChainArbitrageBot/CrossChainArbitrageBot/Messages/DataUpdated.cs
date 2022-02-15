using System.Collections.Generic;
using Agents.Net;

namespace CrossChainArbitrageBot.Messages;

public class DataUpdated : Message
{
    public DataUpdated(Message predecessorMessage, DataUpdate[] updates) : base(predecessorMessage)
    {
        Updates = updates;
    }

    public DataUpdated(IEnumerable<Message> predecessorMessages, DataUpdate[] updates) : base(predecessorMessages)
    {
        Updates = updates;
    }
    
    public DataUpdate[] Updates { get; }

    protected override string DataToString()
    {
        return $"{nameof(Updates)}: {string.Join(", ", Updates)}";
    }
}

public readonly record struct DataUpdate(BlockchainName BlockchainName, double UnstablePrice, double UnstableAmount,
                                         string UnstableSymbol, double StableAmount, string StableSymbol);