using System;
using System.Linq;
using System.Windows;
using Agents.Net;
using CrossChainArbitrageBot.Base.Messages;
using CrossChainArbitrageBot.Base.Models;
using CrossChainArbitrageBot.Simulation.ViewModel;
using CrossChainArbitrageBot.SimulationBase.Messages;
using CrossChainArbitrageBot.SimulationBase.Model;

namespace CrossChainArbitrageBot.Simulation.Agents;

[Consumes(typeof(MainWindowCreated))]
[Consumes(typeof(DataUpdated))]
[Consumes(typeof(TransactionsUntilErrorChanged))]
internal class UiBridge : Agent
{
    private SimulationWindow? simulationWindow;
    private SimulationWindowViewModel? viewModel;
    private MainWindowCreated? createdMessage;
    public UiBridge(IMessageBoard messageBoard) : base(messageBoard)
    {
    }

    protected override void ExecuteCore(Message messageData)
    {
        if (messageData.TryGet(out DataUpdated updated))
        {
            HandleDataUpdate(updated);
            return;
        }
        if (messageData.TryGet(out TransactionsUntilErrorChanged untilErrorChanged))
        {
            simulationWindow!.Dispatcher.Invoke(() =>
            {
                viewModel!.TransactionsUntilError = untilErrorChanged.TransactionsUntilError;
            });
            return;
        }
        createdMessage = messageData.Get<MainWindowCreated>();
        Window window = (Window)createdMessage.MainWindow;
        window.Dispatcher.Invoke(() =>
        {
            viewModel = new SimulationWindowViewModel();
            viewModel.SimulationOverride += OnSimulationOverride;
            viewModel.TransactionsUntilErrorChanged += OnTransactionsUntilErrorChanged;
            simulationWindow = new SimulationWindow
            {
                Owner = window,
                DataContext = viewModel
            };
            simulationWindow.Show();
        });
    }

    private void HandleDataUpdate(DataUpdated updated)
    {
        switch (viewModel)
        {
            case null:
                return;
            case { Chain1Name: null }:
                viewModel.Chain1Name = updated.Updates[0].BlockchainName;
                viewModel.Chain1StableSymbol = updated.Updates[0].StableSymbol;
                viewModel.Chain1StableAmount = updated.Updates[0].StableAmount;
                viewModel.Chain1NativeSymbol = viewModel.Chain1Name == BlockchainName.Bsc ? "BNB" : "AVAX";
                viewModel.Chain1NativeAmount = updated.Updates[0].AccountBalance;
                viewModel.Chain1NativePrice = updated.Updates[0].NativePrice;
                viewModel.Chain1NativeValue = updated.Updates[0].NativePrice * viewModel.Chain1NativeAmount;
                viewModel.Chain1UnstableSymbol = updated.Updates[0].UnstableSymbol;
                viewModel.Chain1UnstableAmount = updated.Updates[0].UnstableAmount;
                viewModel.Chain1UnstablePrice = updated.Updates[0].UnstablePrice;
                viewModel.Chain1UnstableValue = updated.Updates[0].UnstablePrice * viewModel.Chain1UnstableAmount;

                viewModel.Chain2Name = updated.Updates[1].BlockchainName;
                viewModel.Chain2StableSymbol = updated.Updates[1].StableSymbol;
                viewModel.Chain2StableAmount = updated.Updates[1].StableAmount;
                viewModel.Chain2NativeSymbol = viewModel.Chain2Name == BlockchainName.Bsc ? "BNB" : "AVAX";
                viewModel.Chain2NativeAmount = updated.Updates[1].AccountBalance;
                viewModel.Chain2NativePrice = updated.Updates[1].NativePrice;
                viewModel.Chain2NativeValue = updated.Updates[1].NativePrice * viewModel.Chain2NativeAmount;
                viewModel.Chain2UnstableSymbol = updated.Updates[1].UnstableSymbol;
                viewModel.Chain2UnstableAmount = updated.Updates[1].UnstableAmount;
                viewModel.Chain2UnstablePrice = updated.Updates[1].UnstablePrice;
                viewModel.Chain2UnstableValue = updated.Updates[1].UnstablePrice * viewModel.Chain2UnstableAmount;
                break;
        }

        DataUpdate chain1 = updated.Updates.First(u => u.BlockchainName == viewModel.Chain1Name);
        viewModel.Chain1UnstablePrice = chain1.UnstablePrice;
        viewModel.Chain1NativePrice = chain1.NativePrice;

        DataUpdate chain2 = updated.Updates.First(u => u.BlockchainName == viewModel.Chain2Name);
        viewModel.Chain2UnstablePrice = chain2.UnstablePrice;
        viewModel.Chain2NativePrice = chain2.NativePrice;
        return;
    }

    private void OnTransactionsUntilErrorChanged(object? sender, EventArgs e)
    {
        OnMessage(new TransactionsUntilErrorChanged(createdMessage!, viewModel!.TransactionsUntilError));
    }

    private void OnSimulationOverride(object? sender, SimulationOverrideEventArgs e)
    {
        switch (e.Type)
        {
            case SimulationOverrideValueType.Unstable:
                double amount = GetUnstableAmount();
                OnMessage(new WalletBalanceUpdated(createdMessage!,
                                                   new WalletBalanceUpdate(e.BlockchainName,
                                                                           TokenType.Unstable,
                                                                           amount)));
                break;
            case SimulationOverrideValueType.Stable:
                amount = GetStableAmount();
                OnMessage(new WalletBalanceUpdated(createdMessage!,
                                                   new WalletBalanceUpdate(e.BlockchainName,
                                                                           TokenType.Stable,
                                                                           amount)));
                break;
            case SimulationOverrideValueType.Native:
                amount = GetNativeAmount();
                OnMessage(new WalletBalanceUpdated(createdMessage!,
                                                   new WalletBalanceUpdate(e.BlockchainName,
                                                                           TokenType.Native,
                                                                           amount)));
                break;
            case SimulationOverrideValueType.UnstablePrice:
                double? price = GetPriceOverride();
                OnMessage(new PriceUpdated(createdMessage!, price, e.BlockchainName));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(e.Type),e.Type,"Not implemented.");
        }

        double GetUnstableAmount()
        {
            double amount;
            if (e.BlockchainName == viewModel?.Chain1Name)
            {
                viewModel.Chain1UnstableAmount = viewModel.Chain1UnstableValue / viewModel.Chain1UnstablePrice;
                amount = viewModel.Chain1UnstableAmount;
            }
            else
            {
                viewModel!.Chain2UnstableAmount = viewModel.Chain2UnstableValue / viewModel.Chain2UnstablePrice;
                amount = viewModel.Chain2UnstableAmount;
            }

            return amount;
        }

        double GetStableAmount()
        {
            return e.BlockchainName == viewModel?.Chain1Name
                       ? viewModel.Chain1StableAmount
                       : viewModel!.Chain2StableAmount;
        }

        double? GetPriceOverride()
        {
            return e.BlockchainName == viewModel?.Chain1Name
                       ? viewModel.Chain1UnstablePriceOverrideValue < 0.000000001
                         ? null
                         : viewModel.Chain1UnstablePriceOverrideValue
                       : viewModel!.Chain2UnstablePriceOverrideValue < 0.000000001
                           ? null
                           : viewModel.Chain2UnstablePriceOverrideValue;
        }

        double GetNativeAmount()
        {
            double amount;
            if (e.BlockchainName == viewModel?.Chain1Name)
            {
                viewModel.Chain1NativeAmount = viewModel.Chain1NativeValue / viewModel.Chain1NativePrice;
                amount = viewModel.Chain1NativeAmount;
            }
            else
            {
                viewModel!.Chain2NativeAmount = viewModel.Chain2NativeValue / viewModel.Chain2NativePrice;
                amount = viewModel.Chain2NativeAmount;
            }

            return amount;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (viewModel != null)
            {
                viewModel.SimulationOverride -= OnSimulationOverride;
                viewModel.TransactionsUntilErrorChanged -= OnTransactionsUntilErrorChanged;
            }
        }
        base.Dispose(disposing);
    }
}