using Agents.Net;

namespace CrossChainArbitrageBot.Base.Messages;

public class MinimalProfitChanged : Message
{
    public MinimalProfitChanged(Message predecessorMessage, int minimalProfit) : base(predecessorMessage)
    {
        MinimalProfit = minimalProfit;
    }
    
    public MinimalProfitChanged(int minimalProfit) : base(Enumerable.Empty<Message>())
    {
        MinimalProfit = minimalProfit;
    }

    public MinimalProfitChanged(IEnumerable<Message> predecessorMessages, int minimalProfit) : base(predecessorMessages)
    {
        MinimalProfit = minimalProfit;
    }
    
    public int MinimalProfit { get; }

    protected override string DataToString()
    {
        return $"{nameof(MinimalProfit)}: {MinimalProfit}";
    }
}