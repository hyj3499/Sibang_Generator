using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
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
        LoadLogo();
    }

    /// <summary>
    /// logo.png 를 창 아이콘과 상단 헤더 이미지에 적용한다.
    /// 리소스로 포함돼 있으면 pack URI 로, 없으면 실행 파일 옆의 logo.png 를 시도한다.
    /// 파일이 없으면 조용히 넘어간다(로고 없이 정상 동작).
    /// </summary>
    void LoadLogo()
    {
        BitmapImage? img = null;

        // 1) 리소스로 포함된 경우
        try
        {
            img = new BitmapImage(new Uri("pack://application:,,,/logo.png", UriKind.Absolute));
        }
        catch { img = null; }

        // 2) 실행 파일 옆의 logo.png
        if (img is null)
        {
            try
            {
                var path = System.IO.Path.Combine(AppContext.BaseDirectory, "logo.png");
                if (System.IO.File.Exists(path))
                {
                    img = new BitmapImage();
                    img.BeginInit();
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.UriSource = new Uri(path, UriKind.Absolute);
                    img.EndInit();
                }
            }
            catch { img = null; }
        }

        if (img is null) return;
        Icon = img;                 // 창/작업표시줄 아이콘
        if (HeaderLogo is not null)
            HeaderLogo.Source = img; // 상단 헤더 로고
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
