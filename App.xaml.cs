using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Feishu.Context;

/// <summary>
/// App.xaml 的交互逻辑
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// 全局服务提供者
    /// </summary>
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    /// <summary>
    /// 主机构建器
    /// </summary>
    private IHost _host = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 构建依赖注入容器
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(ConfigureServices)
            .Build();

        ServiceProvider = _host.Services;

        await _host.StartAsync();

        // 通过 DI 解析主窗口
        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    /// <summary>
    /// 配置依赖注入服务
    /// </summary>
    private void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        // 注册视图
        services.AddTransient<MainWindow>();

        // 注册 ViewModel
        services.AddTransient<ViewModels.MainViewModel>();

        // 注册服务
        // TODO: 在此注册更多服务
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);

        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }
}
