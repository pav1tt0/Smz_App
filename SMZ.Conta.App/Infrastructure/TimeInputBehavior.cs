using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SMZ.Conta.App.Infrastructure;

public static class TimeInputBehavior
{
    public static readonly DependencyProperty AutoFormatProperty =
        DependencyProperty.RegisterAttached(
            "AutoFormat",
            typeof(bool),
            typeof(TimeInputBehavior),
            new PropertyMetadata(false, OnAutoFormatChanged));

    private static readonly DependencyProperty IsInternalUpdateProperty =
        DependencyProperty.RegisterAttached(
            "IsInternalUpdate",
            typeof(bool),
            typeof(TimeInputBehavior),
            new PropertyMetadata(false));

    public static bool GetAutoFormat(DependencyObject obj) => (bool)obj.GetValue(AutoFormatProperty);

    public static void SetAutoFormat(DependencyObject obj, bool value) => obj.SetValue(AutoFormatProperty, value);

    private static bool GetIsInternalUpdate(DependencyObject obj) => (bool)obj.GetValue(IsInternalUpdateProperty);

    private static void SetIsInternalUpdate(DependencyObject obj, bool value) => obj.SetValue(IsInternalUpdateProperty, value);

    private static void OnAutoFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox textBox)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            textBox.PreviewTextInput += HandlePreviewTextInput;
            textBox.TextChanged += HandleTextChanged;
            textBox.LostFocus += HandleLostFocus;
            DataObject.AddPastingHandler(textBox, HandlePaste);
        }
        else
        {
            textBox.PreviewTextInput -= HandlePreviewTextInput;
            textBox.TextChanged -= HandleTextChanged;
            textBox.LostFocus -= HandleLostFocus;
            DataObject.RemovePastingHandler(textBox, HandlePaste);
        }
    }

    private static void HandlePreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = e.Text.Any(character => !char.IsDigit(character));
    }

    private static void HandlePaste(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText))
        {
            e.CancelCommand();
            return;
        }

        var pastedText = e.SourceDataObject.GetData(DataFormats.UnicodeText) as string ?? string.Empty;
        if (pastedText.Any(character => !char.IsDigit(character) && !char.IsWhiteSpace(character)))
        {
            e.CancelCommand();
        }
    }

    private static void HandleTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox || GetIsInternalUpdate(textBox))
        {
            return;
        }

        var formattedValue = FormatPartialTime(textBox.Text);
        if (formattedValue == textBox.Text)
        {
            return;
        }

        SetText(textBox, formattedValue);
    }

    private static void HandleLostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox textBox || string.IsNullOrWhiteSpace(textBox.Text))
        {
            return;
        }

        var normalizedValue = NormalizeTime(textBox.Text);
        if (normalizedValue != textBox.Text)
        {
            SetText(textBox, normalizedValue);
        }
    }

    private static void SetText(TextBox textBox, string value)
    {
        SetIsInternalUpdate(textBox, true);
        textBox.Text = value;
        textBox.CaretIndex = textBox.Text.Length;
        SetIsInternalUpdate(textBox, false);
    }

    private static string NormalizeTime(string value)
    {
        var digits = ExtractDigits(value);

        if (digits.Length == 3 &&
            TimeOnly.TryParseExact($"0{digits[0]}:{digits[1..]}", "HH:mm", out var compactShort))
        {
            return compactShort.ToString("HH:mm");
        }

        if (digits.Length == 4 &&
            TimeOnly.TryParseExact($"{digits[..2]}:{digits[2..]}", "HH:mm", out var compactFull))
        {
            return compactFull.ToString("HH:mm");
        }

        if (TimeOnly.TryParse(value, out var parsed))
        {
            return parsed.ToString("HH:mm");
        }

        return FormatPartialTime(value);
    }

    private static string FormatPartialTime(string value)
    {
        var digits = ExtractDigits(value);

        return digits.Length switch
        {
            <= 2 => digits,
            _ => $"{digits[..2]}:{digits[2..]}",
        };
    }

    private static string ExtractDigits(string value) => new(value.Where(char.IsDigit).Take(4).ToArray());
}
