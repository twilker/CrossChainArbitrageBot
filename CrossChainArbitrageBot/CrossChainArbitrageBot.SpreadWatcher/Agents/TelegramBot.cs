using System.Configuration;
using Agents.Net;
using CrossChainArbitrageBot.Base.Messages;
using Telegram.Bot;

namespace CrossChainArbitrageBot.SpreadWatcher.Agents;

[Consumes(typeof(SpreadDataUpdated))]
public class TelegramBot : Agent
{
    private readonly string telegramChatId;
    private readonly TelegramBotClient botClient;
    private readonly double targetSpread;
    
    public TelegramBot(IMessageBoard messageBoard) : base(messageBoard)
    {
        string telegramApiKey = ConfigurationManager.AppSettings["TelegramAPIKey"] 
                         ?? throw new ConfigurationErrorsException("TelegramAPIKey not configured.");
        telegramChatId = ConfigurationManager.AppSettings["TelegramChatId"]
                         ?? throw new ConfigurationErrorsException("TelegramChatId not configured");
        targetSpread = double.Parse(ConfigurationManager.AppSettings["SpreadWatcherTarget"]
                                    ?? throw new ConfigurationErrorsException("SpreadWatcherTarget not configured"));
        botClient = new TelegramBotClient(telegramApiKey);
    }

    private int inTargetRange = 0;

    protected override void ExecuteCore(Message messageData)
    {
        SpreadDataUpdated updated = messageData.Get<SpreadDataUpdated>();

        if (updated.Spread > targetSpread &&
            Interlocked.Exchange(ref inTargetRange, 1) == 0)
        {
            botClient.SendTextMessageAsync(telegramChatId,
                                           $"Spread target reached {Math.Abs(updated.Spread * 100):F2}%. " +
                                           $"{(updated.Spread > 0 ? "BSC < Avalanche" : "BSC > Avalanche")}");
        }
        else if (updated.Spread < targetSpread*0.9 &&
                 Interlocked.Exchange(ref inTargetRange, 0) == 1)
        {
            botClient.SendTextMessageAsync(telegramChatId, "Spread no longer in target range.");
        }
        Console.WriteLine($"Current spread: {Math.Abs(updated.Spread * 100):F2}% {(updated.Spread > 0 ? "BSC < Avalanche" : "BSC > Avalanche")}");
    }
}