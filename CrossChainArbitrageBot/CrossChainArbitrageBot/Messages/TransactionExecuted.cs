using System.Collections.Generic;
using Agents.Net;

namespace CrossChainArbitrageBot.Messages;

public class TransactionExecuted : Message
{
    public TransactionExecuted(Message predecessorMessage, bool success) : base(predecessorMessage)
    {
        Success = success;
    }

    public TransactionExecuted(IEnumerable<Message> predecessorMessages, bool success) : base(predecessorMessages)
    {
        Success = success;
    }

    public bool Success { get; }

    protected override string DataToString()
    {
        return $"{nameof(Success)}: {Success}";
    }
}