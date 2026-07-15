using System.Windows;
using SMZ.Conta.App.Data;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.Views;

public partial class LoginWindow : Window
{
    private readonly AccessService _accessService;
    private readonly bool _isFirstSetup;

    public LoginWindow(AccessService accessService)
    {
        InitializeComponent();
        _accessService = accessService;
        _isFirstSetup = !_accessService.HasUsers();
        ModeTitleText.Text = _isFirstSetup
            ? "Prima configurazione: crea l'account amministratore"
            : "Accedi con il tuo identificativo personale";
        SubmitButton.Content = _isFirstSetup ? "CREA AMMINISTRATORE" : "ACCEDI";
        ConfirmationPanel.Visibility = _isFirstSetup ? Visibility.Visible : Visibility.Collapsed;
        Loaded += (_, _) => PerIdTextBox.Focus();
    }

    public AccessSession? Session { get; private set; }

    private void SubmitButton_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = string.Empty;
        if (!int.TryParse(PerIdTextBox.Text.Trim(), out var perId) || perId <= 0)
        {
            ErrorText.Text = "Inserire un PerID numerico valido.";
            return;
        }

        try
        {
            if (_isFirstSetup)
            {
                if (PasswordInput.Password != PasswordConfirmationInput.Password)
                {
                    throw new InvalidOperationException("Le due password non coincidono.");
                }

                Session = _accessService.CreateFirstAdministrator(perId, PasswordInput.Password);
            }
            else
            {
                Session = _accessService.Authenticate(perId, PasswordInput.Password);
            }

            DialogResult = true;
        }
        catch (Exception ex)
        {
            PasswordInput.Clear();
            PasswordConfirmationInput.Clear();
            ErrorText.Text = ex.Message;
            PasswordInput.Focus();
        }
    }
}
