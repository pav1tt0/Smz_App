using System.Windows;
using SMZ.Conta.App.ViewModels;

namespace SMZ.Conta.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}
