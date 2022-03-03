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
    private double chain1UnstablePrice;
    private double chain1UnstableValue;
    private string? chain1UnstableSymbol;
    private BlockchainName? chain1Name;
    
    private double chain2UnstableAmount;
    private double chain2UnstablePrice;
    private double chain2UnstableValue;
    private string? chain2UnstableSymbol;
    private BlockchainName? chain2Name;
    private double chain1StableAmount;
    private double chain1NativeAmount;
    private double chain1NativePrice;
    private double chain1NativeValue;
    private double chain2StableAmount;
    private double chain2NativeAmount;
    private double chain2NativePrice;
    private double chain2NativeValue;
    private string? chain1StableSymbol;
    private string? chain1NativeSymbol;
    private string? chain2StableSymbol;
    private string? chain2NativeSymbol;
    private double chain1UnstablePriceOverrideValue;
    private double chain2UnstablePriceOverrideValue;
    private int transactionsUntilError;

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

    public double Chain1UnstablePrice
    {
        get => chain1UnstablePrice;
        set
        {
            if (value.Equals(chain1UnstablePrice)) return;
            chain1UnstablePrice = value;
            OnPropertyChanged();
        }
    }

    public double Chain1UnstablePriceOverrideValue
    {
        get => chain1UnstablePriceOverrideValue;
        set
        {
            if (value.Equals(chain1UnstablePriceOverrideValue)) return;
            chain1UnstablePriceOverrideValue = value;
            OnPropertyChanged();
        }
    }

    public double Chain1UnstableValue
    {
        get => chain1UnstableValue;
        set
        {
            if (value.Equals(chain1UnstableValue)) return;
            chain1UnstableValue = value;
            OnPropertyChanged();
        }
    }

    public double Chain1StableAmount
    {
        get => chain1StableAmount;
        set
        {
            if (value.Equals(chain1StableAmount)) return;
            chain1StableAmount = value;
            OnPropertyChanged();
        }
    }

    public double Chain1NativeAmount
    {
        get => chain1NativeAmount;
        set
        {
            if (value.Equals(chain1NativeAmount)) return;
            chain1NativeAmount = value;
            OnPropertyChanged();
        }
    }

    public double Chain1NativePrice
    {
        get => chain1NativePrice;
        set
        {
            if (value.Equals(chain1NativePrice)) return;
            chain1NativePrice = value;
            OnPropertyChanged();
        }
    }

    public double Chain1NativeValue
    {
        get => chain1NativeValue;
        set
        {
            if (value.Equals(chain1NativeValue)) return;
            chain1NativeValue = value;
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

    public string? Chain1StableSymbol
    {
        get => chain1StableSymbol;
        set
        {
            if (value == chain1StableSymbol) return;
            chain1StableSymbol = value;
            OnPropertyChanged();
        }
    }

    public string? Chain1NativeSymbol
    {
        get => chain1NativeSymbol;
        set
        {
            if (value == chain1NativeSymbol) return;
            chain1NativeSymbol = value;
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

    public double Chain2UnstableAmount
    {
        get => chain2UnstableAmount;
        set
        {
            if (value.Equals(chain2UnstableAmount)) return;
            chain2UnstableAmount = value;
            OnPropertyChanged();
        }
    }

    public double Chain2UnstablePrice
    {
        get => chain2UnstablePrice;
        set
        {
            if (value.Equals(chain2UnstablePrice)) return;
            chain2UnstablePrice = value;
            OnPropertyChanged();
        }
    }

    public double Chain2UnstablePriceOverrideValue
    {
        get => chain2UnstablePriceOverrideValue;
        set
        {
            if (value.Equals(chain2UnstablePriceOverrideValue)) return;
            chain2UnstablePriceOverrideValue = value;
            OnPropertyChanged();
        }
    }

    public double Chain2UnstableValue
    {
        get => chain2UnstableValue;
        set
        {
            if (value.Equals(chain2UnstableValue)) return;
            chain2UnstableValue = value;
            OnPropertyChanged();
        }
    }

    public double Chain2StableAmount
    {
        get => chain2StableAmount;
        set
        {
            if (value.Equals(chain2StableAmount)) return;
            chain2StableAmount = value;
            OnPropertyChanged();
        }
    }

    public double Chain2NativeAmount
    {
        get => chain2NativeAmount;
        set
        {
            if (value.Equals(chain2NativeAmount)) return;
            chain2NativeAmount = value;
            OnPropertyChanged();
        }
    }

    public double Chain2NativePrice
    {
        get => chain2NativePrice;
        set
        {
            if (value.Equals(chain2NativePrice)) return;
            chain2NativePrice = value;
            OnPropertyChanged();
        }
    }

    public double Chain2NativeValue
    {
        get => chain2NativeValue;
        set
        {
            if (value.Equals(chain2NativeValue)) return;
            chain2NativeValue = value;
            OnPropertyChanged();
        }
    }

    public string? Chain2StableSymbol
    {
        get => chain2StableSymbol;
        set
        {
            if (value == chain2StableSymbol) return;
            chain2StableSymbol = value;
            OnPropertyChanged();
        }
    }

    public string? Chain2NativeSymbol
    {
        get => chain2NativeSymbol;
        set
        {
            if (value == chain2NativeSymbol) return;
            chain2NativeSymbol = value;
            OnPropertyChanged();
        }
    }

    public string? Chain2UnstableSymbol
    {
        get => chain2UnstableSymbol;
        set
        {
            if (value == chain2UnstableSymbol) return;
            chain2UnstableSymbol = value;
            OnPropertyChanged();
        }
    }

    public BlockchainName? Chain2Name
    {
        get => chain2Name;
        set
        {
            if (value == chain2Name) return;
            chain2Name = value;
            OnPropertyChanged();
        }
    }

    public int TransactionsUntilError
    {
        get => transactionsUntilError;
        set
        {
            if (value == transactionsUntilError) return;
            transactionsUntilError = value;
            OnPropertyChanged();
        }
    }

    public ICommand Chain1UnstableAmountOverride { get; }
    public ICommand Chain1UnstablePriceOverride { get; }
    public ICommand Chain1StableAmountOverride { get; }
    public ICommand Chain1NativeAmountOverride { get; }
    public ICommand Chain2UnstableAmountOverride { get; }
    public ICommand Chain2UnstablePriceOverride { get; }
    public ICommand Chain2StableAmountOverride { get; }
    public ICommand Chain2NativeAmountOverride { get; }
    public ICommand SetFailingTransactions { get; }

    public SimulationWindowViewModel()
    {
        Chain1UnstableAmountOverride = new RelayCommand(
            _ => OnSimulationOverride(new SimulationOverrideEventArgs(SimulationOverrideValueType.Unstable, Chain1Name!.Value)),
            _ => Chain1Name.HasValue);
        Chain1UnstablePriceOverride = new RelayCommand(
            _ => OnSimulationOverride(new SimulationOverrideEventArgs(SimulationOverrideValueType.UnstablePrice, Chain1Name!.Value)),
            _ => Chain1Name.HasValue);
        Chain1StableAmountOverride = new RelayCommand(
            _ => OnSimulationOverride(new SimulationOverrideEventArgs(SimulationOverrideValueType.Stable, Chain1Name!.Value)),
            _ => Chain1Name.HasValue);
        Chain1NativeAmountOverride = new RelayCommand(
            _ => OnSimulationOverride(new SimulationOverrideEventArgs(SimulationOverrideValueType.Native, Chain1Name!.Value)),
            _ => Chain1Name.HasValue);
        Chain2UnstableAmountOverride = new RelayCommand(
            _ => OnSimulationOverride(new SimulationOverrideEventArgs(SimulationOverrideValueType.Unstable, Chain2Name!.Value)),
            _ => Chain2Name.HasValue);
        Chain2UnstablePriceOverride = new RelayCommand(
            _ => OnSimulationOverride(new SimulationOverrideEventArgs(SimulationOverrideValueType.UnstablePrice, Chain2Name!.Value)),
            _ => Chain2Name.HasValue);
        Chain2StableAmountOverride = new RelayCommand(
            _ => OnSimulationOverride(new SimulationOverrideEventArgs(SimulationOverrideValueType.Stable, Chain2Name!.Value)),
            _ => Chain2Name.HasValue);
        Chain2NativeAmountOverride = new RelayCommand(
            _ => OnSimulationOverride(new SimulationOverrideEventArgs(SimulationOverrideValueType.Native, Chain2Name!.Value)),
            _ => Chain2Name.HasValue);
        SetFailingTransactions = new RelayCommand(_ => OnTransactionsUntilErrorChanged());
    }

    public event EventHandler<SimulationOverrideEventArgs>? SimulationOverride; 
    public event EventHandler<EventArgs>? TransactionsUntilErrorChanged; 

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

    protected virtual void OnTransactionsUntilErrorChanged()
    {
        TransactionsUntilErrorChanged?.Invoke(this, EventArgs.Empty);
    }
}