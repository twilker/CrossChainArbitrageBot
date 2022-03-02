using Agents.Net;

namespace CrossChainArbitrageBot.Base.Messages;

public class SpreadDataUpdated : MessageDecorator
{
    private SpreadDataUpdated(Message decoratedMessage, double spread, double minimalSpread, double optimalTokenAmount, double currentProfit, IEnumerable<Message>? additionalPredecessors = null) : base(decoratedMessage, additionalPredecessors)
    {
        Spread = spread;
        MinimalSpread = minimalSpread;
        OptimalTokenAmount = optimalTokenAmount;
        CurrentProfit = currentProfit;
    }

    public static SpreadDataUpdated Decorate(DataUpdated dataUpdated, double spread, double minimalSpread,
                                             double optimalTokenAmount, double currentProfit,
                                             IEnumerable<Message>? additionalPredecessors = null)
    {
        return new SpreadDataUpdated(dataUpdated, spread, minimalSpread, optimalTokenAmount,
                                     currentProfit, additionalPredecessors);
    }

    public double Spread { get; }
    public double MinimalSpread { get; }
    public double OptimalTokenAmount { get; }
    public double CurrentProfit { get; }

    protected override string DataToString()
    {
        return $"{nameof(Spread)}: {Spread}, {nameof(MinimalSpread)}: {MinimalSpread}, {nameof(OptimalTokenAmount)}: {OptimalTokenAmount}, {nameof(CurrentProfit)}: {CurrentProfit}";
    }
}