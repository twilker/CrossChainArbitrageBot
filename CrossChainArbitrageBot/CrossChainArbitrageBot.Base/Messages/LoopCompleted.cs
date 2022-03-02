using Agents.Net;

namespace CrossChainArbitrageBot.Base.Messages;

public class LoopCompleted : Message
{
    public LoopCompleted(Message predecessorMessage, double netWorth, double netWorthInclusiveNative, double stableAmount, double unstableAmount, double nativeBsc, double nativeAvalanche) : base(predecessorMessage)
    {
        NetWorth = netWorth;
        NetWorthInclusiveNative = netWorthInclusiveNative;
        StableAmount = stableAmount;
        UnstableAmount = unstableAmount;
        NativeBsc = nativeBsc;
        NativeAvalanche = nativeAvalanche;
    }

    public LoopCompleted(IEnumerable<Message> predecessorMessages, double netWorth, double netWorthInclusiveNative, double stableAmount, double unstableAmount, double nativeBsc, double nativeAvalanche) : base(predecessorMessages)
    {
        NetWorth = netWorth;
        NetWorthInclusiveNative = netWorthInclusiveNative;
        StableAmount = stableAmount;
        UnstableAmount = unstableAmount;
        NativeBsc = nativeBsc;
        NativeAvalanche = nativeAvalanche;
    }
    
    public double NetWorth { get; }
    public double NetWorthInclusiveNative { get; }
    public double StableAmount { get; }
    public double UnstableAmount { get; }
    public double NativeBsc { get; }
    public double NativeAvalanche { get; }

    protected override string DataToString()
    {
        return $"{nameof(NetWorth)}: {NetWorth}, {nameof(NetWorthInclusiveNative)}: {NetWorthInclusiveNative}, " +
               $"{nameof(StableAmount)}: {StableAmount}, {nameof(UnstableAmount)}: {UnstableAmount}, " +
               $"{nameof(NativeBsc)}: {NativeBsc}, {nameof(NativeAvalanche)}: {NativeAvalanche}";
    }
}