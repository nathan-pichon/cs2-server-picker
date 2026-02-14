using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using CS2ServerPicker.ViewModels;

namespace CS2ServerPicker.Converters;

public sealed class PingStatusToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush SuccessBrush = new(Color.FromRgb(76, 175, 80));    // Green
    private static readonly SolidColorBrush TimedOutBrush = new(Color.FromRgb(255, 167, 38));  // Orange
    private static readonly SolidColorBrush BlockedBrush = new(Color.FromRgb(239, 83, 80));    // Red
    private static readonly SolidColorBrush UnknownBrush = new(Color.FromRgb(158, 158, 158));  // Gray
    private static readonly SolidColorBrush PingingBrush = new(Color.FromRgb(92, 107, 192));   // Blue

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is PingStatus status)
        {
            return status switch
            {
                PingStatus.Success => SuccessBrush,
                PingStatus.TimedOut => TimedOutBrush,
                PingStatus.Blocked => BlockedBrush,
                PingStatus.Pinging => PingingBrush,
                PingStatus.Error => TimedOutBrush,
                _ => UnknownBrush
            };
        }

        return UnknownBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class PingStatusToBarWidthConverter : IValueConverter
{
    private const double MaxWidth = 92;
    private const double MaxLatency = 200;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is long latencyMs)
        {
            if (latencyMs <= 0) return 0.0;
            // Invert: lower latency = wider bar
            var ratio = Math.Max(0, 1.0 - (latencyMs / MaxLatency));
            return ratio * MaxWidth;
        }

        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class BoolToFavoriteIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isFavorite && isFavorite)
            return Wpf.Ui.Controls.SymbolRegular.Star24;

        return Wpf.Ui.Controls.SymbolRegular.StarOff24;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return value;
    }
}

/// <summary>
/// Converter for matching radio button string bindings.
/// </summary>
public sealed class StringMatchConverter : MarkupExtensionConverter
{
    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString() == parameter?.ToString();
    }

    public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is true)
            return parameter?.ToString() ?? string.Empty;
        return Binding.DoNothing;
    }
}

public abstract class MarkupExtensionConverter : System.Windows.Markup.MarkupExtension, IValueConverter
{
    public override object ProvideValue(IServiceProvider serviceProvider) => this;

    public abstract object Convert(object value, Type targetType, object parameter, CultureInfo culture);
    public abstract object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture);
}
