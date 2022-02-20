using System.Globalization;
using Agents.Net;
using CsvHelper;
using DataLogger.Messages;
using DataLogger.Models;

namespace DataLogger.Agents;

[Consumes(typeof(DataUpdated))]
public class DataLogger : Agent
{
    private readonly CsvWriter writer;
    public DataLogger(IMessageBoard messageBoard) : base(messageBoard)
    {
        if (File.Exists("data.csv"))
        {
            File.Delete("data.csv");
        }
        StreamWriter streamWriter = new("data.csv");
        writer = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);
        AddDisposable(streamWriter);
        AddDisposable(writer);
        writer.WriteHeader<DataLoggerData>();
        writer.NextRecord();
    }

    protected override void ExecuteCore(Message messageData)
    {
        DataUpdated updated = messageData.Get<DataUpdated>();
        lock (writer)
        {
            DataLoggerData log = new()
            {
                Timestamp = DateTime.Now,
                BscPrice = updated.Updates.First(u => u.BlockchainName == BlockchainName.Bsc)
                                  .UnstablePrice,
                AvalanchePrice = updated.Updates.First(u => u.BlockchainName == BlockchainName.Avalanche)
                                        .UnstablePrice,
            };
            log.Spread = (log.AvalanchePrice - log.BscPrice) / log.BscPrice * 100;
            writer.WriteRecord(log);
            writer.NextRecord();
            writer.Flush();
        }
    }
}