using Agents.Net;

namespace CrossChainArbitrageBot.Base.Messages;

public class ImportantNotice : Message
{
    public ImportantNotice(Message predecessorMessage, string notice, NoticeSeverity severity = NoticeSeverity.Info) : base(predecessorMessage)
    {
        Notice = notice;
        Severity = severity;
    }

    public ImportantNotice(IEnumerable<Message> predecessorMessages, string notice, NoticeSeverity severity = NoticeSeverity.Info) : base(predecessorMessages)
    {
        Notice = notice;
        Severity = severity;
    }

    public string Notice { get; }
    
    public NoticeSeverity Severity { get; }

    protected override string DataToString()
    {
        return $"{nameof(Severity)}: {Severity}; {nameof(Notice)}: {Notice}";
    }
}

public enum NoticeSeverity
{
    Verbose,
    Info,
    Warning,
    Error
}