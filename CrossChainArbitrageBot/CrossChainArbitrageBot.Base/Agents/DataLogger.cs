using System.Globalization;
using Agents.Net;
using CrossChainArbitrageBot.Base.Messages;
using CrossChainArbitrageBot.Base.Models;
using CsvHelper;

namespace CrossChainArbitrageBot.Base.Agents;

[Consumes(typeof(LoopCompleted))]
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
        writer.WriteHeader<LoopDataLog>();
        writer.NextRecord();
    }

    protected override void ExecuteCore(Message messageData)
    {
        LoopCompleted loopCompleted = messageData.Get<LoopCompleted>();
        lock (writer)
        {
            LoopDataLog log = new()
            {
                Timestamp = DateTime.Now,
                NativeAvalanche = loopCompleted.NativeAvalanche,
                NativeBsc = loopCompleted.NativeBsc,
                NetWorth = loopCompleted.NetWorth,
                StableAmount = loopCompleted.StableAmount,
                UnstableAmount = loopCompleted.UnstableAmount,
                NetWorthInclusiveNative = loopCompleted.NetWorthInclusiveNative
            };
            writer.WriteRecord(log);
            writer.NextRecord();
            writer.Flush();
        }
    }
}