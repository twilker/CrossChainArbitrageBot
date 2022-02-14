using Agents.Net;
using CrossChainArbitrageBot.Messages;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossChainArbitrageBot.Agents
{
    [Consumes(typeof(MainWindowCreated))]
    internal class UiBot : Agent
    {
        private MainWindow mainWindow;

        public UiBot(IMessageBoard messageBoard) : base(messageBoard)
        {
        }

        protected override void ExecuteCore(Message messageData)
        {
            MainWindowCreated mainWindowCreated = messageData.Get<MainWindowCreated>();
            mainWindow = mainWindowCreated.MainWindow;
            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {

        }

        private void UnsubscribedFromEvents()
        {

        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                if (mainWindow != null)
                {
                    UnsubscribedFromEvents();
                }
            }
        }
    }
}
