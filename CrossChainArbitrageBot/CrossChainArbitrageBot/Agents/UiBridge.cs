using System;
using System.Windows.Input;
using System.Windows.Threading;
using Agents.Net;
using CrossChainArbitrageBot.Base.Messages;
using CrossChainArbitrageBot.Base.Models;
using CrossChainArbitrageBot.ViewModel;

namespace CrossChainArbitrageBot.Agents;

[Consumes(typeof(MainWindowCreated))]
[Consumes(typeof(DataUpdated))]
[Consumes(typeof(ImportantNotice))]
[Consumes(typeof(LoopStateChanged))]
internal class UiBridge : Agent
{
    private MainWindow? mainWindow;
    private MainWindowCreated? mainWindowCreated;

    public UiBridge(IMessageBoard messageBoard) : base(messageBoard)
    {
    }

    protected override void ExecuteCore(Message messageData)
    {
        if(messageData.TryGet(out ImportantNotice importantNotice))
        {
            mainWindow!.Dispatcher.Invoke(() =>
            {
                ((WindowViewModel)mainWindow.DataContext).ImportantNotices.Add(importantNotice.Notice);
            }, DispatcherPriority.Send);
            return;
        }
        if(messageData.TryGet(out LoopStateChanged loopState))
        {
            mainWindow!.Dispatcher.Invoke(() =>
            {
                ((WindowViewModel)mainWindow.DataContext).LoopState = loopState.State;
                ((WindowViewModel)mainWindow.DataContext).IsLoopOnAuto = loopState.IsAutoLoop;
            }, DispatcherPriority.Send);
            return;
        }
        if (messageData.TryGet(out DataUpdated updated))
        {
            mainWindow!.Dispatcher.Invoke(() =>
            {
                UpdateViewModel(updated);
            }, DispatcherPriority.Send);
            return;
        }
        mainWindowCreated = messageData.Get<MainWindowCreated>();
        mainWindow = (MainWindow)mainWindowCreated.MainWindow;
        SubscribeToEvents();
    }

    private void UpdateViewModel(DataUpdated updated)
    {
        DataUpdate[] updatedUpdates = updated.Updates;
        WindowViewModel viewModel = (WindowViewModel)mainWindow!.DataContext;
        Liquidity bscLiquidity = new Liquidity();
        Liquidity avalancheLiquidity = new Liquidity();
        foreach (DataUpdate dataUpdate in updatedUpdates)
        {
            switch (dataUpdate.BlockchainName)
            {
                case BlockchainName.Bsc:
                    viewModel.BscStableAmount = dataUpdate.StableAmount;
                    viewModel.BscStableToken = dataUpdate.StableSymbol;
                    viewModel.BscUnstableAmount = dataUpdate.UnstableAmount;
                    viewModel.BscUnstablePrice = dataUpdate.UnstablePrice;
                    viewModel.BscUnstableToken = dataUpdate.UnstableSymbol;
                    viewModel.BscAccountBalance = dataUpdate.AccountBalance;
                    viewModel.BscNetWorth = dataUpdate.StableAmount + 
                                            dataUpdate.UnstableAmount * dataUpdate.UnstablePrice +
                                            dataUpdate.AccountBalance * dataUpdate.NativePrice;
                    bscLiquidity = dataUpdate.Liquidity;
                    break;
                case BlockchainName.Avalanche:
                    viewModel.AvalancheStableAmount = dataUpdate.StableAmount;
                    viewModel.AvalancheStableToken = dataUpdate.StableSymbol;
                    viewModel.AvalancheUnstableAmount = dataUpdate.UnstableAmount;
                    viewModel.AvalancheUnstablePrice = dataUpdate.UnstablePrice;
                    viewModel.AvalancheUnstableToken = dataUpdate.UnstableSymbol;
                    viewModel.AvalancheAccountBalance = dataUpdate.AccountBalance;
                    viewModel.AvalancheNetWorth = dataUpdate.StableAmount +
                                                  dataUpdate.UnstableAmount * dataUpdate.UnstablePrice +
                                                  dataUpdate.AccountBalance * dataUpdate.NativePrice;
                    avalancheLiquidity = dataUpdate.Liquidity;
                    break;
                default:
                    throw new InvalidOperationException("Not implemented.");
            }
        }

        viewModel.TotalNetWorth = viewModel.BscNetWorth + viewModel.AvalancheNetWorth;

        if (updated.TryGet(out SpreadDataUpdated spreadDataUpdated))
        {
            Liquidity sellLiquidity = spreadDataUpdated.Spread > 0
                                          ? avalancheLiquidity
                                          : bscLiquidity;
            viewModel.Spread = spreadDataUpdated.Spread*100;
            viewModel.MinimalSpread = spreadDataUpdated.MinimalSpread*100;
            viewModel.OptimalTokenAmount = spreadDataUpdated.OptimalTokenAmount;
            viewModel.OptimalTokenAmountPrice = spreadDataUpdated.OptimalTokenAmount * sellLiquidity.Price;
            viewModel.CurrentProfit = spreadDataUpdated.CurrentProfit;
        }
        CommandManager.InvalidateRequerySuggested();
    }

    private void SubscribeToEvents()
    {
        mainWindow!.Dispatcher.Invoke(() =>
        {
            WindowViewModel viewModel = new();
            mainWindow.DataContext = viewModel;
            viewModel.TransactionInitiated += OnTransactionInitiated;
        });
    }

    private void UnsubscribedFromEvents()
    {
        ((WindowViewModel)mainWindow!.DataContext).TransactionInitiated -= OnTransactionInitiated;
    }

    private void OnTransactionInitiated(object? sender, TransactionEventArgs e)
    {
        OnMessage(new TransactionStarted(mainWindowCreated!,
                                         e.TransactionAmount != null
                                             ? (double)e.TransactionAmount / 100
                                             : null,
                                         e.Chain,
                                         e.Type));
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