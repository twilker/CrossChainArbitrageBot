using System.Configuration;
using Agents.Net;
using CrossChainArbitrageBot.Base.Messages;
using Telegram.Bot;

namespace CrossChainArbitrageBot.Base.Agents;

[Consumes(typeof(ImportantNotice))]
public class TelegramBot : Agent
{
    private readonly string? telegramApiKey;
    private readonly string? telegramErrorApiKey;
    private readonly string? telegramChatId;
    private readonly TelegramBotClient? botClient;
    private readonly TelegramBotClient? errorBotClient;
    
    public TelegramBot(IMessageBoard messageBoard) : base(messageBoard)
    {
        telegramApiKey = ConfigurationManager.AppSettings["TelegramAPIKey"];
        telegramErrorApiKey = ConfigurationManager.AppSettings["TelegramErrorAPIKey"];
        telegramChatId = ConfigurationManager.AppSettings["TelegramChatId"];
        if (!string.IsNullOrEmpty(telegramApiKey))
        {
            botClient = new TelegramBotClient(telegramApiKey);
        }
        if (!string.IsNullOrEmpty(telegramErrorApiKey))
        {
            errorBotClient = new TelegramBotClient(telegramErrorApiKey);
        }
    }

    protected override void ExecuteCore(Message messageData)
    {
        if (string.IsNullOrEmpty(telegramApiKey) ||
            string.IsNullOrEmpty(telegramChatId) ||
            botClient == null ||
            string.IsNullOrEmpty(telegramErrorApiKey) ||
            errorBotClient == null)
        {
            return;
        }
        ImportantNotice notice = messageData.Get<ImportantNotice>();
        if (notice.Severity == NoticeSeverity.Error)
        {
            errorBotClient.SendTextMessageAsync(telegramChatId, notice.Notice,
                                                disableNotification: notice.Severity != NoticeSeverity.Error);
        }
        if (notice.Severity != NoticeSeverity.Verbose)
        {
            botClient.SendTextMessageAsync(telegramChatId, notice.Notice,
                                           disableNotification: notice.Severity != NoticeSeverity.Error);
        }
    }
}