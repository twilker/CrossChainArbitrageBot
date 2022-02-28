using System;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using CrossChainArbitrageBot.Base.Models;

namespace CrossChainArbitrageBot;

[MarkupExtensionReturnType(typeof(IValueConverter))]
public class LoopStateTrafficLightConverter : MarkupExtension, IValueConverter
{
    private static LoopStateTrafficLightConverter? converter;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return converter ??= new LoopStateTrafficLightConverter();
    }

    #region IValueConverter Members

    public object Convert(object? value, Type targetType,
                          object parameter, System.Globalization.CultureInfo culture)
    {
        return value is LoopState loopState
                   ? loopState switch
                   {
                       LoopState.Stopped => new SolidColorBrush(Colors.Red),
                       LoopState.Idle => new SolidColorBrush(Colors.Yellow),
                       LoopState.Running => new SolidColorBrush(Colors.Green),
                       _ => throw new ArgumentOutOfRangeException()
                   }
                   : Binding.DoNothing;
    }

    public object ConvertBack(object? value, Type targetType,
                              object parameter, System.Globalization.CultureInfo culture)
    {
        return Binding.DoNothing;
    }

    #endregion
}