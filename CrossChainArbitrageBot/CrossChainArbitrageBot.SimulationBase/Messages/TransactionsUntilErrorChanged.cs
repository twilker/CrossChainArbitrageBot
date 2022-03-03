using System.Collections.Generic;
using Agents.Net;

namespace CrossChainArbitrageBot.SimulationBase.Messages;

public class TransactionsUntilErrorChanged : Message
{
    public TransactionsUntilErrorChanged(Message predecessorMessage, int transactionsUntilError) : base(predecessorMessage)
    {
        TransactionsUntilError = transactionsUntilError;
    }

    public TransactionsUntilErrorChanged(IEnumerable<Message> predecessorMessages, int transactionsUntilError) : base(predecessorMessages)
    {
        TransactionsUntilError = transactionsUntilError;
    }
    
    public int TransactionsUntilError { get; }

    protected override string DataToString()
    {
        return $"{nameof(TransactionsUntilError)}: {TransactionsUntilError}";
    }
}