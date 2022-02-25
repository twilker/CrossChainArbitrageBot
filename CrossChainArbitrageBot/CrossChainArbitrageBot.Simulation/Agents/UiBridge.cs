using System.Windows;
using Agents.Net;
using CrossChainArbitrageBot.Base.Messages;

namespace CrossChainArbitrageBot.Simulation.Agents;

[Consumes(typeof(MainWindowCreated))]
internal class UiBridge : Agent
{
    private SimulationWindow simulationWindow;
    public UiBridge(IMessageBoard messageBoard) : base(messageBoard)
    {
    }

    protected override void ExecuteCore(Message messageData)
    {
        MainWindowCreated windowCreated = messageData.Get<MainWindowCreated>();
        Window window = (Window)windowCreated.MainWindow;
        window.Dispatcher.Invoke(() =>
        {
            simulationWindow = new SimulationWindow
            {
                Owner = window
            };
            simulationWindow.Show();
        });
    }
}