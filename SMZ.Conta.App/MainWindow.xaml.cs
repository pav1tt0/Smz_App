using System.Collections;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows;
using SMZ.Conta.App.Infrastructure;
using SMZ.Conta.App.ViewModels;

namespace SMZ.Conta.App;

public partial class MainWindow : Window
{
    private readonly DiveAmbiencePlayer _diveAmbiencePlayer = new();
    private readonly MainWindowViewModel _viewModel;
    private readonly Dictionary<string, int> _qualificaOrder;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainWindowViewModel();
        DataContext = _viewModel;
        _qualificaOrder = _viewModel.QualificheDisponibili
            .Select((qualifica, index) => new { qualifica, index })
            .ToDictionary(item => item.qualifica, item => item.index, StringComparer.OrdinalIgnoreCase);

        Loaded += MainWindow_Loaded;
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
            listView.CustomSort = new ServizioPartecipantiComparer(_qualificaOrder, sortMemberPath, direction);
        }
    }

    private sealed class ServizioPartecipantiComparer(
        IReadOnlyDictionary<string, int> qualificaOrder,
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
                ? CompareQualifica(left, right, qualificaOrder)
                : StringComparer.CurrentCultureIgnoreCase.Compare(left.Nominativo, right.Nominativo);

            if (result == 0)
            {
                result = sortMemberPath == nameof(ServizioPartecipanteDraftViewModel.Qualifica)
                    ? StringComparer.CurrentCultureIgnoreCase.Compare(left.Nominativo, right.Nominativo)
                    : CompareQualifica(left, right, qualificaOrder);
            }

            return direction == ListSortDirection.Ascending ? result : -result;
        }

        private static int CompareQualifica(
            ServizioPartecipanteDraftViewModel left,
            ServizioPartecipanteDraftViewModel right,
            IReadOnlyDictionary<string, int> qualificaOrder)
        {
            var leftRank = GetRank(left.Qualifica, qualificaOrder);
            var rightRank = GetRank(right.Qualifica, qualificaOrder);
            var rankCompare = leftRank.CompareTo(rightRank);
            if (rankCompare != 0)
            {
                return rankCompare;
            }

            return StringComparer.CurrentCultureIgnoreCase.Compare(left.Qualifica, right.Qualifica);
        }

        private static int GetRank(string? qualifica, IReadOnlyDictionary<string, int> qualificaOrder) =>
            !string.IsNullOrWhiteSpace(qualifica) && qualificaOrder.TryGetValue(qualifica.Trim(), out var rank)
                ? rank
                : int.MaxValue;
    }
}
