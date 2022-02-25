using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CrossChainArbitrageBot.Annotations;
using CrossChainArbitrageBot.Models;

namespace CrossChainArbitrageBot.ViewModel;

public class WindowViewModel : INotifyPropertyChanged
{
    private double spread;
    private double targetSpread;
    private double maximumVolumeToTargetSpread;
    private double profitByMaximumVolume;
    private double bscUnstableAmount;
    private double bscStableAmount;
    private double bscUnstablePrice;
    private double avalancheUnstableAmount;
    private double avalancheStableAmount;
    private double avalancheUnstablePrice;
    private string bscUnstableToken;
    private string avalancheUnstableToken;
    private string bscStableToken;
    private string avalancheStableToken;
    private double bscAccountBalance;
    private double avalancheAccountBalance;
    private int transactionPercentage;

    public double Spread
    {
        get => spread;
        set
        {
            if (value.Equals(spread)) return;
            spread = value;
            OnPropertyChanged();
        }
    }

    public double TargetSpread
    {
        get => targetSpread;
        set
        {
            if (value.Equals(targetSpread)) return;
            targetSpread = value;
            OnPropertyChanged();
        }
    }

    public double MaximumVolumeToTargetSpread
    {
        get => maximumVolumeToTargetSpread;
        set
        {
            if (value.Equals(maximumVolumeToTargetSpread)) return;
            maximumVolumeToTargetSpread = value;
            OnPropertyChanged();
        }
    }
    
    public double ProfitByMaximumVolume
    {
        get => profitByMaximumVolume;
        set
        {
            if (value.Equals(profitByMaximumVolume)) return;
            profitByMaximumVolume = value;
            OnPropertyChanged();
        }
    }

    public double BscUnstableAmount
    {
        get => bscUnstableAmount;
        set
        {
            if (value.Equals(bscUnstableAmount)) return;
            bscUnstableAmount = value;
            OnPropertyChanged();
        }
    }

    public double BscStableAmount
    {
        get => bscStableAmount;
        set
        {
            if (value.Equals(bscStableAmount)) return;
            bscStableAmount = value;
            OnPropertyChanged();
        }
    }

    public string BscStableToken
    {
        get => bscStableToken;
        set
        {
            if (value == bscStableToken) return;
            bscStableToken = value;
            OnPropertyChanged();
        }
    }

    public double BscUnstablePrice
    {
        get => bscUnstablePrice;
        set
        {
            if (value.Equals(bscUnstablePrice)) return;
            bscUnstablePrice = value;
            OnPropertyChanged();
        }
    }

    public double BscAccountBalance
    {
        get => bscAccountBalance;
        set
        {
            if (value.Equals(bscAccountBalance)) return;
            bscAccountBalance = value;
            OnPropertyChanged();
        }
    }

    public string BscUnstableToken
    {
        get => bscUnstableToken;
        set
        {
            if (value == bscUnstableToken) return;
            bscUnstableToken = value;
            OnPropertyChanged();
        }
    }

    public double AvalancheUnstableAmount
    {
        get => avalancheUnstableAmount;
        set
        {
            if (value.Equals(avalancheUnstableAmount)) return;
            avalancheUnstableAmount = value;
            OnPropertyChanged();
        }
    }

    public double AvalancheAccountBalance
    {
        get => avalancheAccountBalance;
        set
        {
            if (value.Equals(avalancheAccountBalance)) return;
            avalancheAccountBalance = value;
            OnPropertyChanged();
        }
    }

    public string AvalancheStableToken
    {
        get => avalancheStableToken;
        set
        {
            if (value == avalancheStableToken) return;
            avalancheStableToken = value;
            OnPropertyChanged();
        }
    }

    public double AvalancheStableAmount
    {
        get => avalancheStableAmount;
        set
        {
            if (value.Equals(avalancheStableAmount)) return;
            avalancheStableAmount = value;
            OnPropertyChanged();
        }
    }

    public double AvalancheUnstablePrice
    {
        get => avalancheUnstablePrice;
        set
        {
            if (value.Equals(avalancheUnstablePrice)) return;
            avalancheUnstablePrice = value;
            OnPropertyChanged();
        }
    }

    public string AvalancheUnstableToken
    {
        get => avalancheUnstableToken;
        set
        {
            if (value == avalancheUnstableToken) return;
            avalancheUnstableToken = value;
            OnPropertyChanged();
        }
    }

    public int TransactionPercentage
    {
        get => transactionPercentage;
        set
        {
            if (value == transactionPercentage) return;
            transactionPercentage = value;
            OnPropertyChanged();
        }
    }

    public ICommand BscUnstableToStableCommand { get; }

    public ICommand BscStableToUnstableCommand { get; }

    public ICommand AvalancheUnstableToStableCommand { get; }

    public ICommand AvalancheStableToUnstableCommand { get; }

    public ICommand AvalancheBridgeStableCommand { get; }

    public ICommand AvalancheBridgeUnstableCommand { get; }

    public ICommand BscBridgeStableCommand { get; }
    
    public ICommand BscBridgeUnstableCommand { get; }
    
    public ICommand BscStableToNativeCommand { get; }
    
    public ICommand BscUnstableToNativeCommand { get; }
    
    public ICommand AvalancheStableToNativeCommand { get; }
    
    public ICommand AvalancheUnstableToNativeCommand { get; }

    public ObservableCollection<string> ImportantNotices { get; } = new();

    public WindowViewModel()
    {
        BscUnstableToStableCommand = new RelayCommand(BscUnstableToStable);
        BscStableToUnstableCommand = new RelayCommand(BscStableToUnstable);
        AvalancheUnstableToStableCommand = new RelayCommand(AvalancheUnstableToStable);
        AvalancheStableToUnstableCommand = new RelayCommand(AvalancheStableToUnstable);
        BscBridgeStableCommand = new RelayCommand(BscBridgeStable);
        BscBridgeUnstableCommand = new RelayCommand(BscBridgeUnstable);
        AvalancheBridgeStableCommand = new RelayCommand(AvalancheBridgeStable);
        AvalancheBridgeUnstableCommand = new RelayCommand(AvalancheBridgeUnstable);
        BscStableToNativeCommand = new RelayCommand(BscStableToNative);
        BscUnstableToNativeCommand = new RelayCommand(BscUnstableToNative);
        AvalancheStableToNativeCommand = new RelayCommand(AvalancheStableToNative);
        AvalancheUnstableToNativeCommand = new RelayCommand(AvalancheUnstableToNative);
    }

    private void BscStableToNative(object? parameter)
    {
        OnTransactionInitiated(new TransactionEventArgs(TransactionPercentage,
                                                        BlockchainName.Bsc,
                                                        TransactionType.StableToNative));
    }

    private void BscUnstableToNative(object? parameter)
    {
        OnTransactionInitiated(new TransactionEventArgs(TransactionPercentage,
                                                        BlockchainName.Bsc,
                                                        TransactionType.UnstableToNative));
    }

    private void AvalancheStableToNative(object? parameter)
    {
        OnTransactionInitiated(new TransactionEventArgs(TransactionPercentage,
                                                        BlockchainName.Avalanche,
                                                        TransactionType.StableToNative));
    }

    private void AvalancheUnstableToNative(object? parameter)
    {
        OnTransactionInitiated(new TransactionEventArgs(TransactionPercentage,
                                                        BlockchainName.Avalanche,
                                                        TransactionType.UnstableToNative));
    }

    private void AvalancheBridgeUnstable(object? parameter)
    {
        OnTransactionInitiated(new TransactionEventArgs(TransactionPercentage,
                                                        BlockchainName.Avalanche,
                                                        TransactionType.BridgeUnstable));
    }

    private void AvalancheBridgeStable(object? parameter)
    {
        OnTransactionInitiated(new TransactionEventArgs(TransactionPercentage,
                                                        BlockchainName.Avalanche,
                                                        TransactionType.BridgeStable));
    }

    private void BscBridgeUnstable(object? parameter)
    {
        OnTransactionInitiated(new TransactionEventArgs(TransactionPercentage,
                               BlockchainName.Bsc,
                               TransactionType.BridgeUnstable));
    }

    private void BscBridgeStable(object? parameter)
    {
        OnTransactionInitiated(new TransactionEventArgs(TransactionPercentage,
                               BlockchainName.Bsc,
                               TransactionType.BridgeStable));
    }

    private void BscUnstableToStable(object? parameter)
    {
        OnTransactionInitiated(new TransactionEventArgs(TransactionPercentage,
                               BlockchainName.Bsc,
                               TransactionType.UnstableToStable));
    }

    private void BscStableToUnstable(object? parameter)
    {
        OnTransactionInitiated(new TransactionEventArgs(TransactionPercentage,
                               BlockchainName.Bsc,
                               TransactionType.StableToUnstable));
    }

    private void AvalancheUnstableToStable(object? parameter)
    {
        OnTransactionInitiated(new TransactionEventArgs(TransactionPercentage,
                                                        BlockchainName.Avalanche,
                                                        TransactionType.UnstableToStable));
    }

    private void AvalancheStableToUnstable(object? parameter)
    {
        OnTransactionInitiated(new TransactionEventArgs(TransactionPercentage,
                                                        BlockchainName.Avalanche,
                                                        TransactionType.StableToUnstable));
    }

    public event EventHandler<TransactionEventArgs>? TransactionInitiated;

    protected void OnTransactionInitiated(TransactionEventArgs eventArgs)
    {
        TransactionInitiated?.Invoke(this, eventArgs);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}