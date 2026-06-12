using System.Windows;
using System.Windows.Controls;
using Feishu.Context.ViewModels;
using Feishu.Context.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Feishu.Context;

/// <summary>
/// MainWindow 的交互逻辑
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // 从 DI 容器解析视图并设置到 TabItem 内容
        var feishuView = App.ServiceProvider.GetRequiredService<FeishuView>();
        FeishuContent.Content = feishuView;

        var apiView = App.ServiceProvider.GetRequiredService<ApiView>();
        ApiContent.Content = apiView;
    }
}
