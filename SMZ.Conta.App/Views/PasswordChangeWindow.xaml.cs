using System.Windows;
using SMZ.Conta.App.Data;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.Views;

public partial class PasswordChangeWindow : Window
{
    private readonly AccessService _accessService;
    private readonly AccessSession _session;

    public PasswordChangeWindow(AccessService accessService, AccessSession session, bool required)
    {
        InitializeComponent();
        _accessService = accessService;
        _session = session;
        DescriptionText.Text = required
            ? "L'amministratore ha reimpostato la password. Devi sceglierne una nuova prima di continuare."
            : $"Account PerID {session.PerId}. La nuova password deve contenere almeno 10 caratteri.";
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = string.Empty;
        try
        {
            if (NewPasswordInput.Password != ConfirmationInput.Password)
                throw new InvalidOperationException("Le due nuove password non coincidono.");
            _accessService.ChangePassword(_session.PerId, CurrentPasswordInput.Password, NewPasswordInput.Password);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            NewPasswordInput.Clear();
            ConfirmationInput.Clear();
        }
    }
}
