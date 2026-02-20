using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using CS2ServerPicker.ViewModels;

namespace CS2ServerPicker.Converters;

/// <summary>
/// Returns a brush based on <see cref="PingStatus"/> — used for latency text color.
/// </summary>
public sealed class PingStatusToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush SuccessBrush  = new(Color.FromRgb( 76, 175,  80));  // Green
    private static readonly SolidColorBrush TimedOutBrush = new(Color.FromRgb(255, 167,  38));  // Orange
    private static readonly SolidColorBrush BlockedBrush  = new(Color.FromRgb(239,  83,  80));  // Red
    private static readonly SolidColorBrush UnknownBrush  = new(Color.FromRgb(158, 158, 158));  // Gray
    private static readonly SolidColorBrush PingingBrush  = new(Color.FromRgb( 92, 107, 192));  // Blue

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is PingStatus status)
        {
            return status switch
            {
                PingStatus.Success  => SuccessBrush,
                PingStatus.TimedOut => TimedOutBrush,
                PingStatus.Blocked  => BlockedBrush,
                PingStatus.Pinging  => PingingBrush,
                PingStatus.Error    => TimedOutBrush,
                _                   => UnknownBrush
            };
        }

        return UnknownBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Maps a latency value (ms) to a bar width in device-independent units.
/// Bar grows from 0 (no data) to MaxWidth at MaxPingBarMs or above.
/// Updated by SettingsViewModel when the user changes the threshold.
/// </summary>
public sealed class PingStatusToBarWidthConverter : IValueConverter
{
    public static int MaxPingBarMs { get; set; } = 120;
    private const double MaxWidth = 92;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is long latencyMs && latencyMs > 0)
        {
            var ratio = Math.Min(1.0, latencyMs / (double)MaxPingBarMs);
            return ratio * MaxWidth;
        }

        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Maps a latency value (ms) to a color: green (low) → orange (medium) → red (high).
/// Returns gray when no valid latency is available.
/// Updated by SettingsViewModel when the user changes the threshold.
/// </summary>
public sealed class LatencyToBrushConverter : IValueConverter
{
    public static int MaxPingBarMs { get; set; } = 120;

    private static readonly SolidColorBrush GoodBrush    = new(Color.FromRgb( 76, 175,  80));  // Green   ≤ 33%
    private static readonly SolidColorBrush MediumBrush  = new(Color.FromRgb(255, 167,  38));  // Orange  33–66%
    private static readonly SolidColorBrush BadBrush     = new(Color.FromRgb(239,  83,  80));  // Red     > 66%
    private static readonly SolidColorBrush UnknownBrush = new(Color.FromRgb(158, 158, 158));  // Gray

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not long latencyMs || latencyMs <= 0)
            return UnknownBrush;

        var ratio = Math.Min(1.0, latencyMs / (double)MaxPingBarMs);

        return ratio switch
        {
            <= 0.33 => GoodBrush,
            <= 0.66 => MediumBrush,
            _       => BadBrush
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Returns Star24 (plain star, rendered filled via Filled binding) for favorites,
/// and StarOff24 (crossed-out star) for non-favorites.
/// </summary>
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
        if (value is bool b) return !b;
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b) return !b;
        return value;
    }
}

/// <summary>
/// Converter for matching radio button string bindings.
/// </summary>
public sealed class StringMatchConverter : MarkupExtensionConverter
{
    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value?.ToString() == parameter?.ToString();

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
