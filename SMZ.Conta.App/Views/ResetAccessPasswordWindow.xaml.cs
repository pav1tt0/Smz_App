using System.Windows;
using SMZ.Conta.App.Data;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.Views;

public partial class ResetAccessPasswordWindow : Window
{
    private readonly AccessService _accessService;
    private readonly int _administratorPerId;
    private readonly int _targetPerId;

    public ResetAccessPasswordWindow(AccessService accessService, int administratorPerId, AccessUserSummary target)
    {
        InitializeComponent();
        _accessService = accessService;
        _administratorPerId = administratorPerId;
        _targetPerId = target.PerId;
        DescriptionText.Text = $"Account {target.DisplayName} (PerID {target.PerId}). Al prossimo accesso sarà richiesto il cambio password.";
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = string.Empty;
        try
        {
            if (PasswordInput.Password != ConfirmationInput.Password)
                throw new InvalidOperationException("Le due password non coincidono.");
            _accessService.ResetPassword(_administratorPerId, _targetPerId, PasswordInput.Password);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            PasswordInput.Clear();
            ConfirmationInput.Clear();
        }
    }
}
