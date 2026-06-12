#nullable enable
using System.Windows.Controls;
using Feishu.Context.Services;
using Feishu.Context.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Feishu.Context.Views;

/// <summary>
/// FeishuView.xaml 的交互逻辑
/// </summary>
public partial class FeishuView : UserControl
{
    private FeishuViewModel _viewModel = null!;

    public FeishuView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        _viewModel = App.ServiceProvider.GetRequiredService<FeishuViewModel>();
        DataContext = _viewModel;

        var feishuWebService = App.ServiceProvider.GetRequiredService<FeishuWebService>();
        await feishuWebService.InitializeAsync(FeishuWebView);
        await _viewModel.OnWebViewInitializedAsync();
    }
}
