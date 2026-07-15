using System.Windows;
using System.Windows.Controls;
using SMZ.Conta.App.Data;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.Views;

public partial class CreateAccessUserWindow : Window
{
    private readonly AccessService _accessService;
    private readonly int _administratorPerId;

    public CreateAccessUserWindow(AccessService accessService, int administratorPerId)
    {
        InitializeComponent();
        _accessService = accessService;
        _administratorPerId = administratorPerId;
    }

    private void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = string.Empty;
        try
        {
            if (!int.TryParse(PerIdInput.Text.Trim(), out var perId) || perId <= 0)
                throw new InvalidOperationException("Inserire un PerID numerico valido.");
            if (PasswordInput.Password != ConfirmationInput.Password)
                throw new InvalidOperationException("Le due password non coincidono.");
            var roleTag = (RoleInput.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            var role = roleTag == "Administrator" ? AccessRole.Administrator : AccessRole.Base;
            _accessService.CreateUser(_administratorPerId, perId, role, PasswordInput.Password);
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
