using System.Linq;
using Agents.Net;

namespace CrossChainArbitrageBot.Messages
{
    internal class MainWindowCreated : Message
    {
        public MainWindowCreated(MainWindow mainWindow) : base(Enumerable.Empty<Message>())
        {
            MainWindow = mainWindow;
        }

        public MainWindow MainWindow { get; }

        protected override string DataToString()
        {
            return string.Empty;
        }
    }
}
