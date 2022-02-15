using System.ComponentModel;
using System.Runtime.CompilerServices;
using CrossChainArbitrageBot.Annotations;

namespace CrossChainArbitrageBot.ViewModel;

public class WindowViewModel : INotifyPropertyChanged
{
    private double spread;
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

    public event PropertyChangedEventHandler? PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}