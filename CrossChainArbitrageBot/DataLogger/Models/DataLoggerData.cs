namespace DataLogger.Models;

public class DataLoggerData
{
    public DateTime Timestamp { get; set; }
    public double BscPrice { get; set; }
    public double AvalanchePrice { get; set; }
    public double Spread { get; set; }
}