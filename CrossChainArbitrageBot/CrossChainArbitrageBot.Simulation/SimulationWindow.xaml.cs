using System.Windows;

namespace CrossChainArbitrageBot.Simulation;

public partial class SimulationWindow : Window
{
    public SimulationWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Left = SystemParameters.WorkArea.Right - Width;
        Top = SystemParameters.WorkArea.Height / 2 - Height / 2;
    }
}