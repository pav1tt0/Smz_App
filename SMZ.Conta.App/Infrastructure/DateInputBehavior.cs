using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SMZ.Conta.App.Infrastructure;

public static class DateInputBehavior
{
    private static readonly CultureInfo ItalianCulture = CultureInfo.GetCultureInfo("it-IT");

    public static readonly DependencyProperty AutoFormatProperty =
        DependencyProperty.RegisterAttached(
            "AutoFormat",
            typeof(bool),
            typeof(DateInputBehavior),
            new PropertyMetadata(false, OnAutoFormatChanged));

    private static readonly DependencyProperty IsInternalUpdateProperty =
        DependencyProperty.RegisterAttached(
            "IsInternalUpdate",
            typeof(bool),
            typeof(DateInputBehavior),
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
        e.Handled = e.Text.Any(character =>
            !char.IsDigit(character) &&
            character != '/' &&
            character != '-' &&
            character != '.');
    }

    private static void HandlePaste(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText))
        {
            e.CancelCommand();
            return;
        }

        var pastedText = e.SourceDataObject.GetData(DataFormats.UnicodeText) as string ?? string.Empty;
        var isValid = pastedText.All(character =>
            char.IsDigit(character) ||
            char.IsWhiteSpace(character) ||
            character == '/' ||
            character == '-' ||
            character == '.');

        if (!isValid)
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

        var formattedValue = FormatPartialDate(textBox.Text);
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

        var normalizedValue = NormalizeDate(textBox.Text);
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

    private static string NormalizeDate(string value)
    {
        var compactDigits = ExtractDigits(value);
        if (compactDigits.Length == 8 &&
            DateOnly.TryParseExact(compactDigits, "ddMMyyyy", ItalianCulture, DateTimeStyles.None, out var compactDate))
        {
            return compactDate.ToString("dd/MM/yyyy", ItalianCulture);
        }

        var normalizedSeparators = value.Trim().Replace('-', '/').Replace('.', '/');
        if (DateOnly.TryParse(normalizedSeparators, ItalianCulture, DateTimeStyles.None, out var parsedDate))
        {
            return parsedDate.ToString("dd/MM/yyyy", ItalianCulture);
        }

        return FormatPartialDate(value);
    }

    private static string FormatPartialDate(string value)
    {
        var digits = ExtractDigits(value);

        return digits.Length switch
        {
            <= 2 => digits,
            <= 4 => $"{digits[..2]}/{digits[2..]}",
            _ => $"{digits[..2]}/{digits[2..4]}/{digits[4..]}",
        };
    }

    private static string ExtractDigits(string value) => new(value.Where(char.IsDigit).Take(8).ToArray());
}
