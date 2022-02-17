using System;
using Agents.Net;
using CrossChainArbitrageBot.Messages;
using CrossChainArbitrageBot.Models;
using CrossChainArbitrageBot.ViewModel;

namespace CrossChainArbitrageBot.Agents
{
    [Consumes(typeof(MainWindowCreated))]
    [Consumes(typeof(DataUpdated))]
    [Consumes(typeof(ImportantNotice))]
    internal class UiBot : Agent
    {
        private MainWindow mainWindow;
        private MainWindowCreated mainWindowCreated;

        public UiBot(IMessageBoard messageBoard) : base(messageBoard)
        {
        }

        protected override void ExecuteCore(Message messageData)
        {
            if(messageData.TryGet(out ImportantNotice importantNotice))
            {
                mainWindow.Dispatcher.Invoke(() =>
                {
                    ((WindowViewModel)mainWindow.DataContext).ImportantNotices.Add(importantNotice.Notice);
                });
                return;
            }
            if (messageData.TryGet(out DataUpdated updated))
            {
                mainWindow.Dispatcher.Invoke(() =>
                {
                    UpdateViewModel(updated.Updates);
                });
                return;
            }
            mainWindowCreated = messageData.Get<MainWindowCreated>();
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
                        viewModel.BscAccountBalance = dataUpdate.AccountBalance;
                        break;
                    case BlockchainName.Avalanche:
                        avalanchePrice = dataUpdate.UnstablePrice;
                        viewModel.AvalancheStableAmount = dataUpdate.StableAmount;
                        viewModel.AvalancheStableToken = dataUpdate.StableSymbol;
                        viewModel.AvalancheUnstableAmount = dataUpdate.UnstableAmount;
                        viewModel.AvalancheUnstablePrice = dataUpdate.UnstablePrice;
                        viewModel.AvalancheUnstableToken = dataUpdate.UnstableSymbol;
                        viewModel.AvalancheAccountBalance = dataUpdate.AccountBalance;
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
                WindowViewModel viewModel = new();
                mainWindow.DataContext = viewModel;
                viewModel.TransactionInitiated += OnTransactionInitiated;
            });
        }

        private void UnsubscribedFromEvents()
        {
            ((WindowViewModel)mainWindow.DataContext).TransactionInitiated -= OnTransactionInitiated;
        }

        private void OnTransactionInitiated(object? sender, TransactionEventArgs e)
        {
            OnMessage(new TransactionStarted(mainWindowCreated, (double)e.TransactionAmount / 100, e.Chain, e.Type));
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
