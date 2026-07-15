using System.Collections.ObjectModel;
using System.Windows;
using SMZ.Conta.App.Data;
using SMZ.Conta.App.Infrastructure;
using SMZ.Conta.App.Models;
using SMZ.Conta.App.Views;

namespace SMZ.Conta.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    private readonly AccessService _accessService = new();
    private AccessSession _session = AccessSession.SystemAdministrator;
    private AccessUserSummary? _selectedAccessUser;

    public ObservableCollection<AccessUserSummary> AccessUsers { get; private set; } = [];
    public RelayCommand CreateAccessUserCommand { get; private set; } = null!;
    public RelayCommand ResetAccessPasswordCommand { get; private set; } = null!;
    public RelayCommand ToggleAccessUserCommand { get; private set; } = null!;
    public RelayCommand ToggleAccessRoleCommand { get; private set; } = null!;
    public RelayCommand ChangeOwnPasswordCommand { get; private set; } = null!;

    public bool IsAdministrator => _session.IsAdministrator;
    public bool IsBaseUser => !IsAdministrator;
    public string CurrentUserDisplay => _session.UserDisplayName;
    public string CurrentRoleDisplay => _session.RoleDisplayName;

    public AccessUserSummary? SelectedAccessUser
    {
        get => _selectedAccessUser;
        set
        {
            if (SetProperty(ref _selectedAccessUser, value))
            {
                ResetAccessPasswordCommand.RaiseCanExecuteChanged();
                ToggleAccessUserCommand.RaiseCanExecuteChanged();
                ToggleAccessRoleCommand.RaiseCanExecuteChanged();
                OnPropertyChanged(nameof(SelectedAccessUserActionLabel));
                OnPropertyChanged(nameof(SelectedAccessUserRoleActionLabel));
            }
        }
    }

    public string SelectedAccessUserActionLabel => SelectedAccessUser?.IsActive == true ? "Sospendi" : "Riattiva";
    public string SelectedAccessUserRoleActionLabel =>
        SelectedAccessUser?.Role == AccessRole.Administrator ? "Imposta Base" : "Rendi amministratore";

    private void InitializeAccessManagement(AccessSession session)
    {
        _session = session;
        AccessUsers = [];
        CreateAccessUserCommand = new RelayCommand(CreateAccessUser, () => IsAdministrator);
        ResetAccessPasswordCommand = new RelayCommand(ResetAccessPassword, () => IsAdministrator && SelectedAccessUser is not null);
        ToggleAccessUserCommand = new RelayCommand(ToggleAccessUser, () => IsAdministrator && SelectedAccessUser is not null);
        ToggleAccessRoleCommand = new RelayCommand(ToggleAccessRole, () => IsAdministrator && SelectedAccessUser is not null);
        ChangeOwnPasswordCommand = new RelayCommand(ChangeOwnPassword, () => _session.PerId > 0);
        if (IsAdministrator && _session.PerId > 0)
        {
            ReloadAccessUsers();
        }
    }

    private void CreateAccessUser()
    {
        var dialog = new CreateAccessUserWindow(_accessService, _session.PerId) { Owner = Application.Current.MainWindow };
        if (dialog.ShowDialog() == true)
        {
            ReloadAccessUsers();
            Stato = "Nuovo account creato. Al primo accesso sara richiesto il cambio password.";
        }
    }

    private void ResetAccessPassword()
    {
        if (SelectedAccessUser is null) return;
        var dialog = new ResetAccessPasswordWindow(_accessService, _session.PerId, SelectedAccessUser)
        {
            Owner = Application.Current.MainWindow,
        };
        if (dialog.ShowDialog() == true)
        {
            ReloadAccessUsers();
            Stato = $"Password del PerID {SelectedAccessUser?.PerId} reimpostata.";
        }
    }

    private void ToggleAccessUser()
    {
        if (SelectedAccessUser is null) return;
        var activate = !SelectedAccessUser.IsActive;
        var action = activate ? "riattivare" : "sospendere";
        if (MessageBox.Show($"Vuoi {action} l'account PerID {SelectedAccessUser.PerId}?", "Gestione accessi",
                MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) return;
        try
        {
            _accessService.SetUserActive(_session.PerId, SelectedAccessUser.PerId, activate);
            ReloadAccessUsers();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Gestione accessi", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ToggleAccessRole()
    {
        if (SelectedAccessUser is null) return;
        var role = SelectedAccessUser.Role == AccessRole.Administrator ? AccessRole.Base : AccessRole.Administrator;
        if (MessageBox.Show($"Assegnare il profilo {role.ToDisplayName()} al PerID {SelectedAccessUser.PerId}?",
                "Gestione accessi", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) return;
        try
        {
            _accessService.SetUserRole(_session.PerId, SelectedAccessUser.PerId, role);
            ReloadAccessUsers();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Gestione accessi", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ChangeOwnPassword()
    {
        var dialog = new PasswordChangeWindow(_accessService, _session, required: false)
        {
            Owner = Application.Current.MainWindow,
        };
        if (dialog.ShowDialog() == true) Stato = "Password modificata correttamente.";
    }

    private void ReloadAccessUsers()
    {
        var selectedPerId = SelectedAccessUser?.PerId;
        AccessUsers.Clear();
        foreach (var user in _accessService.GetUsers()) AccessUsers.Add(user);
        SelectedAccessUser = AccessUsers.FirstOrDefault(user => user.PerId == selectedPerId);
    }

    private bool EnsureAdministratorAccess()
    {
        if (IsAdministrator) return true;
        MessageBox.Show("Operazione riservata agli amministratori.", "Accesso non consentito",
            MessageBoxButton.OK, MessageBoxImage.Warning);
        Stato = "Operazione non consentita al profilo Base.";
        return false;
    }
}
