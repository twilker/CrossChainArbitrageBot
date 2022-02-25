using System;
using System.Windows.Data;
using System.Windows.Markup;

namespace CrossChainArbitrageBot;

[MarkupExtensionReturnType(typeof(IValueConverter))]
public class SpreadDirectionConverter : MarkupExtension, IValueConverter
{
    private static SpreadDirectionConverter? converter;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return converter ??= new SpreadDirectionConverter();
    }

    #region IValueConverter Members

    public object Convert(object? value, Type targetType,
                          object parameter, System.Globalization.CultureInfo culture)
    {
        return value is double spread
                   ? spread > 0
                         ? $"< {Math.Abs(spread):F2}% <"
                         : $"> {Math.Abs(spread):F2}% >"
                   : Binding.DoNothing;
    }

    public object ConvertBack(object? value, Type targetType,
                              object parameter, System.Globalization.CultureInfo culture)
    {
        return Binding.DoNothing;
    }

    #endregion
}