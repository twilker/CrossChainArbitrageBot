using System.Collections.Generic;
using Agents.Net;
using CrossChainArbitrageBot.SimulationBase.Model;

namespace CrossChainArbitrageBot.SimulationBase.Messages;

public class WalletBalanceUpdated : Message
{
    public WalletBalanceUpdated(Message predecessorMessage, IEnumerable<WalletBalanceUpdate> updates) : base(predecessorMessage)
    {
        Updates = updates;
    }

    public WalletBalanceUpdated(IEnumerable<Message> predecessorMessages, IEnumerable<WalletBalanceUpdate> updates) : base(predecessorMessages)
    {
        Updates = updates;
    }
    
    public IEnumerable<WalletBalanceUpdate> Updates { get; }

    protected override string DataToString()
    {
        return $"{nameof(Updates)}: {string.Join("; ", Updates)}";
    }
}