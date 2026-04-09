using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace SMZ.Conta.App.Controls;

public partial class DateInputPicker : UserControl
{
    private static readonly CultureInfo ItalianCulture = CultureInfo.GetCultureInfo("it-IT");
    private bool _isSynchronizing;

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(DateInputPicker),
            new FrameworkPropertyMetadata(
                string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnTextChanged));

    public DateInputPicker()
    {
        InitializeComponent();
        Loaded += (_, _) => SyncCalendarFromText();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DateInputPicker picker)
        {
            picker.SyncCalendarFromText();
        }
    }

    private void CalendarButton_Click(object sender, RoutedEventArgs e)
    {
        SyncCalendarFromText();
        CalendarPopup.IsOpen = !CalendarPopup.IsOpen;
    }

    private void CalendarHost_SelectedDatesChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isSynchronizing || CalendarHost.SelectedDate is not DateTime selectedDate)
        {
            return;
        }

        _isSynchronizing = true;
        try
        {
            Text = selectedDate.ToString("dd/MM/yyyy", ItalianCulture);
            CalendarPopup.IsOpen = false;
        }
        finally
        {
            _isSynchronizing = false;
        }
    }

    private void SyncCalendarFromText()
    {
        if (_isSynchronizing)
        {
            return;
        }

        _isSynchronizing = true;
        try
        {
            if (TryParseDate(Text, out var parsedDate))
            {
                var selectedDate = parsedDate.ToDateTime(TimeOnly.MinValue);
                CalendarHost.SelectedDate = selectedDate;
                CalendarHost.DisplayDate = selectedDate;
            }
            else
            {
                CalendarHost.SelectedDate = null;
                CalendarHost.DisplayDate = DateTime.Today;
            }
        }
        finally
        {
            _isSynchronizing = false;
        }
    }

    private static bool TryParseDate(string? value, out DateOnly parsedDate)
    {
        parsedDate = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var compactDigits = new string(value.Where(char.IsDigit).Take(8).ToArray());
        if (compactDigits.Length == 8 &&
            DateOnly.TryParseExact(compactDigits, "ddMMyyyy", ItalianCulture, DateTimeStyles.None, out parsedDate))
        {
            return true;
        }

        var normalizedValue = value.Trim().Replace('-', '/').Replace('.', '/');
        return DateOnly.TryParse(normalizedValue, ItalianCulture, DateTimeStyles.None, out parsedDate);
    }
}
