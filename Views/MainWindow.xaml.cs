using System.Windows;
using System.Windows.Controls;
using Sibang_generator.Models;
using Sibang_generator.ViewModels;

namespace Sibang_generator.Views;

public partial class MainWindow : Window
{
    MainViewModel Vm => (MainViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();
        Closing += (_, _) => Vm.PersistConfig();
    }

    void Resolved_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is ListViewItem { Content: ResolvedModel rm })
            Vm.OverrideBomCommand.Execute(rm);
    }

    void Group_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is ListViewItem { Content: Models.VersionGroup g })
        {
            Vm.SelectedGroup = g;
            Vm.EditGroupCommand.Execute(null);
        }
    }

    void BackToSettings_Click(object sender, RoutedEventArgs e) => Vm.ShowPreview = false;
}
