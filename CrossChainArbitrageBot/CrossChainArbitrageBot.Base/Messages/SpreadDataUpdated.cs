using Agents.Net;

namespace CrossChainArbitrageBot.Base.Messages;

public class SpreadDataUpdated : MessageDecorator
{
    private SpreadDataUpdated(Message decoratedMessage, double spread, double targetSpread, double maximumVolumeToTargetSpread, double profitByMaximumVolume, IEnumerable<Message>? additionalPredecessors = null) : base(decoratedMessage, additionalPredecessors)
    {
        Spread = spread;
        TargetSpread = targetSpread;
        MaximumVolumeToTargetSpread = maximumVolumeToTargetSpread;
        ProfitByMaximumVolume = profitByMaximumVolume;
    }

    public static SpreadDataUpdated Decorate(DataUpdated dataUpdated, double spread, double targetSpread,
                                             double maximumVolumeToTargetSpread, double profitByMaximumVolume,
                                             IEnumerable<Message>? additionalPredecessors = null)
    {
        return new SpreadDataUpdated(dataUpdated, spread, targetSpread, maximumVolumeToTargetSpread,
                                     profitByMaximumVolume, additionalPredecessors);
    }
    
    public double Spread { get; }
    public double TargetSpread { get; }
    public double MaximumVolumeToTargetSpread { get; }
    public double ProfitByMaximumVolume { get; }

    protected override string DataToString()
    {
        return $"{nameof(Spread)}: {Spread}, {nameof(TargetSpread)}: {TargetSpread}, {nameof(MaximumVolumeToTargetSpread)}: {MaximumVolumeToTargetSpread}, {nameof(ProfitByMaximumVolume)}: {ProfitByMaximumVolume}";
    }
}