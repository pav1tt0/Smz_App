using System.Collections;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows;
using SMZ.Conta.App.Infrastructure;
using SMZ.Conta.App.Models;
using SMZ.Conta.App.ViewModels;

namespace SMZ.Conta.App;

public partial class MainWindow : Window
{
    private readonly DiveAmbiencePlayer _diveAmbiencePlayer = new();
    private readonly MainWindowViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainWindowViewModel();
        DataContext = _viewModel;

        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
        Closed += MainWindow_Closed;
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateWelcomeAudio();
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        _diveAmbiencePlayer.Dispose();
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        var areeConModifiche = _viewModel.GetAreeConModificheNonSalvate();
        if (areeConModifiche.Count == 0)
        {
            return;
        }

        var elencoAree = string.Join(", ", areeConModifiche);
        var result = MessageBox.Show(
            $"Ci sono modifiche non salvate in: {elencoAree}.\n\nVuoi chiudere comunque l'applicazione?",
            "Modifiche non salvate",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (result != MessageBoxResult.Yes)
        {
            e.Cancel = true;
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.IsWelcomeVisible)
            || e.PropertyName == nameof(MainWindowViewModel.IsWelcomeAudioEnabled))
        {
            UpdateWelcomeAudio();
        }
    }

    private void UpdateWelcomeAudio()
    {
        if (_viewModel.IsWelcomeVisible && _viewModel.IsWelcomeAudioEnabled)
        {
            _diveAmbiencePlayer.Start();
            return;
        }

        _diveAmbiencePlayer.Stop();
    }

    private void ServizioPartecipantiDataGrid_Sorting(object sender, DataGridSortingEventArgs e)
    {
        if (sender is not DataGrid dataGrid || dataGrid.ItemsSource is null)
        {
            return;
        }

        var sortMemberPath = e.Column.SortMemberPath;
        if (string.IsNullOrWhiteSpace(sortMemberPath) && e.Column is DataGridBoundColumn boundColumn)
        {
            sortMemberPath = (boundColumn.Binding as Binding)?.Path?.Path;
        }

        if (sortMemberPath is not nameof(ServizioPartecipanteDraftViewModel.Qualifica)
            and not nameof(ServizioPartecipanteDraftViewModel.QualificaDisplay)
            and not nameof(ServizioPartecipanteDraftViewModel.Nominativo))
        {
            return;
        }

        e.Handled = true;

        var direction = e.Column.SortDirection == ListSortDirection.Ascending
            ? ListSortDirection.Descending
            : ListSortDirection.Ascending;

        foreach (var column in dataGrid.Columns)
        {
            if (!ReferenceEquals(column, e.Column))
            {
                column.SortDirection = null;
            }
        }

        e.Column.SortDirection = direction;

        var view = CollectionViewSource.GetDefaultView(dataGrid.ItemsSource);
        view.SortDescriptions.Clear();
        if (view is ListCollectionView listView)
        {
            listView.CustomSort = new ServizioPartecipantiComparer(sortMemberPath, direction);
        }
    }

    private sealed class ServizioPartecipantiComparer(
        string sortMemberPath,
        ListSortDirection direction) : IComparer
    {
        public int Compare(object? x, object? y)
        {
            if (x is not ServizioPartecipanteDraftViewModel left || y is not ServizioPartecipanteDraftViewModel right)
            {
                return 0;
            }

            var result = sortMemberPath == nameof(ServizioPartecipanteDraftViewModel.Qualifica)
                || sortMemberPath == nameof(ServizioPartecipanteDraftViewModel.QualificaDisplay)
                ? CompareQualifica(left, right)
                : StringComparer.CurrentCultureIgnoreCase.Compare(left.Nominativo, right.Nominativo);

            if (result == 0)
            {
                result = sortMemberPath == nameof(ServizioPartecipanteDraftViewModel.Qualifica)
                    || sortMemberPath == nameof(ServizioPartecipanteDraftViewModel.QualificaDisplay)
                    ? StringComparer.CurrentCultureIgnoreCase.Compare(left.Nominativo, right.Nominativo)
                    : CompareQualifica(left, right);
            }

            return direction == ListSortDirection.Ascending ? result : -result;
        }

        private static int CompareQualifica(
            ServizioPartecipanteDraftViewModel left,
            ServizioPartecipanteDraftViewModel right)
        {
            var leftRank = QualificaFormatter.GetGerarchiaOrdine(left.Qualifica, left.IsProfiloSanitario, left.RuoloSanitario);
            var rightRank = QualificaFormatter.GetGerarchiaOrdine(right.Qualifica, right.IsProfiloSanitario, right.RuoloSanitario);
            var rankCompare = leftRank.CompareTo(rightRank);
            if (rankCompare != 0)
            {
                return rankCompare;
            }

            return StringComparer.CurrentCultureIgnoreCase.Compare(left.Qualifica, right.Qualifica);
        }
    }
}
