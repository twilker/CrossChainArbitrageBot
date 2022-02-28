using Agents.Net;
using CrossChainArbitrageBot.Base.Models;

namespace CrossChainArbitrageBot.Base.Messages;

public class LoopStateChanged : Message
{
    public LoopStateChanged(Message predecessorMessage, bool isAutoLoop, LoopState state) : base(predecessorMessage)
    {
        IsAutoLoop = isAutoLoop;
        State = state;
    }

    public LoopStateChanged(IEnumerable<Message> predecessorMessages, bool isAutoLoop, LoopState state) : base(predecessorMessages)
    {
        IsAutoLoop = isAutoLoop;
        State = state;
    }
    
    public bool IsAutoLoop { get; }
    public LoopState State { get; }

    protected override string DataToString()
    {
        return $"{nameof(IsAutoLoop)}: {IsAutoLoop}, {nameof(State)}: {State}";
    }
}