using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CrossChainArbitrageBot.Base.Models;
using CrossChainArbitrageBot.Simulation.Annotations;
using CrossChainArbitrageBot.SimulationBase.Model;

namespace CrossChainArbitrageBot.Simulation.ViewModel;

public class SimulationWindowViewModel : INotifyPropertyChanged
{
    private double chain1UnstableAmount;
    private string? chain1UnstableSymbol;
    private BlockchainName? chain1Name;

    public double Chain1UnstableAmount
    {
        get => chain1UnstableAmount;
        set
        {
            if (value.Equals(chain1UnstableAmount)) return;
            chain1UnstableAmount = value;
            OnPropertyChanged();
        }
    }

    public string? Chain1UnstableSymbol
    {
        get => chain1UnstableSymbol;
        set
        {
            if (value == chain1UnstableSymbol) return;
            chain1UnstableSymbol = value;
            OnPropertyChanged();
        }
    }

    public BlockchainName? Chain1Name
    {
        get => chain1Name;
        set
        {
            if (value == chain1Name) return;
            chain1Name = value;
            OnPropertyChanged();
        }
    }

    public ICommand Chain1UnstableAmountOverride { get; }

    public SimulationWindowViewModel()
    {
        Chain1UnstableAmountOverride = new RelayCommand(
            _ => OnSimulationOverride(new SimulationOverrideEventArgs(SimulationOverrideValueType.Unstable, Chain1Name!.Value, Chain1UnstableAmount)),
            _ => Chain1Name.HasValue);
    }

    public event EventHandler<SimulationOverrideEventArgs>? SimulationOverride; 

    public event PropertyChangedEventHandler? PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected virtual void OnSimulationOverride(SimulationOverrideEventArgs e)
    {
        SimulationOverride?.Invoke(this, e);
    }
}