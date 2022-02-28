using Agents.Net;
using CrossChainArbitrageBot.Base.Models;

namespace CrossChainArbitrageBot.Base.Messages;

public class LoopStarted : Message
{
    public LoopStarted(Message predecessorMessage, LoopKind kind) : base(predecessorMessage)
    {
        Kind = kind;
    }
    
    public LoopStarted(IEnumerable<Message> predecessorMessages, LoopKind kind) : base(predecessorMessages)
    {
        Kind = kind;
    }
    
    public LoopKind Kind { get; }

    protected override string DataToString()
    {
        return $"{nameof(Kind)}: {Kind}";
    }
}