using System.Configuration;
using Agents.Net;
using CrossChainArbitrageBot.Base.Messages;
using Telegram.Bot;

namespace CrossChainArbitrageBot.Base.Agents;

[Consumes(typeof(ImportantNotice))]
public class TelegramBot : Agent
{
    private readonly string? telegramApiKey;
    private readonly string? telegramChatId;
    private readonly TelegramBotClient? botClient;
    
    public TelegramBot(IMessageBoard messageBoard) : base(messageBoard)
    {
        telegramApiKey = ConfigurationManager.AppSettings["TelegramAPIKey"];
        telegramChatId = ConfigurationManager.AppSettings["TelegramChatId"];
        if (!string.IsNullOrEmpty(telegramApiKey))
        {
            botClient = new TelegramBotClient(telegramApiKey);
        }
    }

    protected override void ExecuteCore(Message messageData)
    {
        if (string.IsNullOrEmpty(telegramApiKey) ||
            string.IsNullOrEmpty(telegramChatId) ||
            botClient == null)
        {
            return;
        }
        ImportantNotice notice = messageData.Get<ImportantNotice>();
        if (notice.Severity != NoticeSeverity.Verbose)
        {
            botClient.SendTextMessageAsync(telegramChatId, notice.Notice,
                                           disableNotification: notice.Severity != NoticeSeverity.Error);
        }
    }
}