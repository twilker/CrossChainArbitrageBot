using Agents.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
