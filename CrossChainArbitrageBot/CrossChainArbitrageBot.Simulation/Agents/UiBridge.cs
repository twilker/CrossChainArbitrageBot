using System;
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
            if (viewModel is { Chain1Name: null })
            {
                viewModel.Chain1Name = updated.Updates[0].BlockchainName;
                viewModel.Chain1UnstableSymbol = updated.Updates[0].UnstableSymbol;
                viewModel.Chain1UnstableAmount = updated.Updates[0].UnstableAmount;
            }
            return;
        }
        createdMessage = messageData.Get<MainWindowCreated>();
        Window window = (Window)createdMessage.MainWindow;
        window.Dispatcher.Invoke(() =>
        {
            viewModel = new SimulationWindowViewModel();
            viewModel.SimulationOverride += OnSimulationOverride;
            simulationWindow = new SimulationWindow
            {
                Owner = window,
                DataContext = viewModel
            };
            simulationWindow.Show();
        });
    }

    private void OnSimulationOverride(object? sender, SimulationOverrideEventArgs e)
    {
        switch (e.Type)
        {
            case SimulationOverrideValueType.Unstable:
                OnMessage(new WalletBalanceUpdated(createdMessage!,
                                                   new WalletBalanceUpdate(e.BlockchainName,
                                                                           TokenType.Unstable,
                                                                           e.Amount)));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(e.Type),e.Type,"Not implemented.");
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (viewModel != null)
            {
                viewModel.SimulationOverride -= OnSimulationOverride;
            }
        }
        base.Dispose(disposing);
    }
}