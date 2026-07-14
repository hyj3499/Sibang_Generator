using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Sibang_generator.Models;

namespace Sibang_generator.Converters;

/// <summary>bool → Visibility. 파라미터 "invert" 로 반전.</summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c)
    {
        bool b = value is bool v && v;
        if (p as string == "invert") b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object? value, Type t, object? p, CultureInfo c) =>
        value is Visibility vis && vis == Visibility.Visible;
}

/// <summary>Severity → 색상.</summary>
public sealed class SeverityToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c) => value switch
    {
        Severity.Error   => new SolidColorBrush(Color.FromRgb(0xD1, 0x3A, 0x3A)),
        Severity.Warning => new SolidColorBrush(Color.FromRgb(0xC8, 0x7F, 0x0A)),
        Severity.Pass    => new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32)),
        _                => new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)),
    };
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) => Binding.DoNothing;
}

/// <summary>enum 값과 파라미터가 같으면 true (RadioButton 바인딩용).</summary>
public sealed class EnumEqualsConverter : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c) =>
        value?.ToString() == p?.ToString();
    public object ConvertBack(object? value, Type t, object? p, CultureInfo c) =>
        (value is bool b && b) ? Enum.Parse(t, p!.ToString()!) : Binding.DoNothing;
}
