using Agents.Net;
using CrossChainArbitrageBot.Messages;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrossChainArbitrageBot.ViewModel;

namespace CrossChainArbitrageBot.Agents
{
    [Consumes(typeof(MainWindowCreated))]
    [Consumes(typeof(DataUpdated))]
    internal class UiBot : Agent
    {
        private MainWindow mainWindow;

        public UiBot(IMessageBoard messageBoard) : base(messageBoard)
        {
        }

        protected override void ExecuteCore(Message messageData)
        {
            if (messageData.TryGet(out DataUpdated updated))
            {
                mainWindow.Dispatcher.Invoke(() =>
                {
                    UpdateViewModel(updated.Updates);
                });
                return;
            }
            MainWindowCreated mainWindowCreated = messageData.Get<MainWindowCreated>();
            mainWindow = mainWindowCreated.MainWindow;
            SubscribeToEvents();
        }

        private void UpdateViewModel(DataUpdate[] updatedUpdates)
        {
            WindowViewModel viewModel = (WindowViewModel)mainWindow.DataContext;
            double bscPrice = 0;
            double avalanchePrice = 0;
            foreach (DataUpdate dataUpdate in updatedUpdates)
            {
                switch (dataUpdate.BlockchainName)
                {
                    case BlockchainName.Bsc:
                        bscPrice = dataUpdate.UnstablePrice;
                        viewModel.BscStableAmount = dataUpdate.StableAmount;
                        viewModel.BscStableToken = dataUpdate.StableSymbol;
                        viewModel.BscUnstableAmount = dataUpdate.UnstableAmount;
                        viewModel.BscUnstablePrice = dataUpdate.UnstablePrice;
                        viewModel.BscUnstableToken = dataUpdate.UnstableSymbol;
                        break;
                    case BlockchainName.Avalanche:
                        avalanchePrice = dataUpdate.UnstablePrice;
                        viewModel.AvalancheStableAmount = dataUpdate.StableAmount;
                        viewModel.AvalancheStableToken = dataUpdate.StableSymbol;
                        viewModel.AvalancheUnstableAmount = dataUpdate.UnstableAmount;
                        viewModel.AvalancheUnstablePrice = dataUpdate.UnstablePrice;
                        viewModel.AvalancheUnstableToken = dataUpdate.UnstableSymbol;
                        break;
                    default:
                        throw new InvalidOperationException("Not implemented.");
                }

                viewModel.Spread = (avalanchePrice - bscPrice) / bscPrice * 100;
            }
        }

        private void SubscribeToEvents()
        {
            mainWindow.Dispatcher.Invoke(() =>
            {
                mainWindow.DataContext = new WindowViewModel();
            });
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
