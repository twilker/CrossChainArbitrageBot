using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CrossChainArbitrageBot.Annotations;
using CrossChainArbitrageBot.Base.Models;

namespace CrossChainArbitrageBot.ViewModel;

public class WindowViewModel : INotifyPropertyChanged
{
    private double spread;
    private double minimalSpread;
    private double optimalTokenAmount;
    private double currentProfit;
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
    private double bscNetWorth;
    private double avalancheNetWorth;
    private double totalNetWorth;
    private bool isLoopOnAuto;
    private LoopState loopState;
    private double optimalTokenAmountPrice;

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

    public double MinimalSpread
    {
        get => minimalSpread;
        set
        {
            if (value.Equals(minimalSpread)) return;
            minimalSpread = value;
            OnPropertyChanged();
        }
    }

    public double OptimalTokenAmount
    {
        get => optimalTokenAmount;
        set
        {
            if (value.Equals(optimalTokenAmount)) return;
            optimalTokenAmount = value;
            OnPropertyChanged();
        }
    }

    public double OptimalTokenAmountPrice
    {
        get => optimalTokenAmountPrice;
        set
        {
            if (value.Equals(optimalTokenAmountPrice)) return;
            optimalTokenAmountPrice = value;
            OnPropertyChanged();
        }
    }

    public double CurrentProfit
    {
        get => currentProfit;
        set
        {
            if (value.Equals(currentProfit)) return;
            currentProfit = value;
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

    public double BscNetWorth
    {
        get => bscNetWorth;
        set
        {
            if (value.Equals(bscNetWorth)) return;
            bscNetWorth = value;
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

    public double AvalancheNetWorth
    {
        get => avalancheNetWorth;
        set
        {
            if (value.Equals(avalancheNetWorth)) return;
            avalancheNetWorth = value;
            OnPropertyChanged();
        }
    }

    public double TotalNetWorth
    {
        get => totalNetWorth;
        set
        {
            if (value.Equals(totalNetWorth)) return;
            totalNetWorth = value;
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

    public bool IsLoopOnAuto
    {
        get => isLoopOnAuto;
        set
        {
            if (value == isLoopOnAuto) return;
            isLoopOnAuto = value;
            OnPropertyChanged();
        }
    }

    public LoopState LoopState
    {
        get => loopState;
        set
        {
            if (value == loopState) return;
            loopState = value;
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
    
    public ICommand SynchronizedTradeCommand { get; }
    
    public ICommand SingleLoopCommand { get; }
    
    public ICommand AutoLoopCommand { get; }

    public ObservableCollection<string> ImportantNotices { get; } = new();

    public WindowViewModel()
    {
        BscUnstableToStableCommand = new RelayCommand(BscUnstableToStable, CanExecuteCommand);
        BscStableToUnstableCommand = new RelayCommand(BscStableToUnstable, CanExecuteCommand);
        AvalancheUnstableToStableCommand = new RelayCommand(AvalancheUnstableToStable, CanExecuteCommand);
        AvalancheStableToUnstableCommand = new RelayCommand(AvalancheStableToUnstable, CanExecuteCommand);
        BscBridgeStableCommand = new RelayCommand(BscBridgeStable, CanExecuteCommand);
        BscBridgeUnstableCommand = new RelayCommand(BscBridgeUnstable, CanExecuteCommand);
        AvalancheBridgeStableCommand = new RelayCommand(AvalancheBridgeStable, CanExecuteCommand);
        AvalancheBridgeUnstableCommand = new RelayCommand(AvalancheBridgeUnstable, CanExecuteCommand);
        BscStableToNativeCommand = new RelayCommand(BscStableToNative, CanExecuteCommand);
        BscUnstableToNativeCommand = new RelayCommand(BscUnstableToNative, CanExecuteCommand);
        AvalancheStableToNativeCommand = new RelayCommand(AvalancheStableToNative, CanExecuteCommand);
        AvalancheUnstableToNativeCommand = new RelayCommand(AvalancheUnstableToNative, CanExecuteCommand);
        SynchronizedTradeCommand = new RelayCommand(SynchronizedTrade, CanExecuteCommand);
        SingleLoopCommand = new RelayCommand(SingleLoop, CanExecuteCommand);
        AutoLoopCommand = new RelayCommand(AutoLoop, _ => !string.IsNullOrEmpty(BscUnstableToken));
    }

    private bool CanExecuteCommand(object? arg)
    {
        return LoopState != LoopState.Running &&
               !string.IsNullOrEmpty(BscUnstableToken);
    }

    private void AutoLoop(object? obj)
    {
        OnTransactionInitiated(new TransactionEventArgs(TransactionType.AutoLoop));
    }

    private void SingleLoop(object? obj)
    {
        OnTransactionInitiated(new TransactionEventArgs(TransactionType.SingleLoop));
    }

    private void SynchronizedTrade(object? obj)
    {
        OnTransactionInitiated(new TransactionEventArgs(TransactionType.SynchronizedTrade));
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