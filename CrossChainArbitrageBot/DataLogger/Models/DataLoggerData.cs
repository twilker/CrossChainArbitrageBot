namespace DataLogger.Models;

public class DataLoggerData
{
    public DateTime Timestamp { get; set; }
    public double BscPrice { get; set; }
    public double AvalanchePrice { get; set; }
    public double Spread { get; set; }
    public double TotalVolume { get; set; }
    public double SimulatedProfit { get; set; }
    public double SimulatedProfitGasConsidered { get; set; }
}