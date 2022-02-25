using System.Collections.Generic;
using Agents.Net;

namespace CrossChainArbitrageBot.Messages;

internal class TransactionFinished : Message
{
    public TransactionFinished(Message predecessorMessage, TransactionResult result) : base(predecessorMessage)
    {
        Result = result;
    }

    public TransactionFinished(IEnumerable<Message> predecessorMessages, TransactionResult result) : base(predecessorMessages)
    {
        Result = result;
    }

    public TransactionResult Result { get; }

    protected override string DataToString()
    {
        return $"{nameof(Result)}: {Result}";
    }
}

public enum TransactionResult
{
    Success,
    Failed,
    Rejected
}