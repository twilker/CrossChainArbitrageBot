namespace CrossChainArbitrageBot.Base.Models;

public class LoopDataLog
{
    public DateTime Timestamp { get; set; }
    public double NetWorth { get; set; }
    public double NetWorthInclusiveNative { get; set; }
    public double StableAmount { get; set; }
    public double UnstableAmount { get; set; }
    public double NativeBsc { get; set; }
    public double NativeAvalanche { get; set; }
}