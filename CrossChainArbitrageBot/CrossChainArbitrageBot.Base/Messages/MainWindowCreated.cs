using Agents.Net;

namespace CrossChainArbitrageBot.Base.Messages;

public class MainWindowCreated : Message
{
    public MainWindowCreated(object mainWindow) : base(Enumerable.Empty<Message>())
    {
        MainWindow = mainWindow;
    }

    public object MainWindow { get; }

    protected override string DataToString()
    {
        return string.Empty;
    }
}