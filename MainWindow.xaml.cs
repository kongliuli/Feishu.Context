using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Feishu.Context;

/// <summary>
/// MainWindow 的交互逻辑
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(ViewModels.MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
