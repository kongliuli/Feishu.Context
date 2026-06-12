using System;
using System.Windows;
using Feishu.Context.Data;
using Feishu.Context.Services;
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

        try
        {
            // 构建依赖注入容器（Host.CreateDefaultBuilder 自动加载 appsettings.json）
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices(ConfigureServices)
                .Build();

            ServiceProvider = _host.Services;

            await _host.StartAsync();

            // 初始化数据库
            var dbService = ServiceProvider.GetRequiredService<DbService>();
            await dbService.InitializeAsync();

            // 通过 DI 解析主窗口
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"应用程序启动失败：{ex.Message}\n\n{ex.StackTrace}",
                "启动错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    /// <summary>
    /// 配置依赖注入服务
    /// </summary>
    private void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        // 注册配置：从 IConfiguration 读取 appsettings.json
        services.AddSingleton<AppSettings>(sp =>
        {
            var settings = new AppSettings();
            var config = context.Configuration;
            settings.Feishu.WebUrl = config["Feishu:WebUrl"] ?? settings.Feishu.WebUrl;
            settings.Feishu.UserDataFolder = config["Feishu:UserDataFolder"] ?? settings.Feishu.UserDataFolder;
            settings.Feishu.MessagePollingIntervalSeconds = int.TryParse(config["Feishu:MessagePollingIntervalSeconds"], out var mpi) ? mpi : 30;
            settings.Api.DefaultTimeoutSeconds = int.TryParse(config["Api:DefaultTimeoutSeconds"], out var dto) ? dto : 30;
            settings.Api.MaxRetryCount = int.TryParse(config["Api:MaxRetryCount"], out var mrc) ? mrc : 3;
            settings.Database.ConnectionString = config["Database:ConnectionString"] ?? string.Empty;
            return settings;
        });

        // 注册视图
        services.AddTransient<MainWindow>();
        services.AddTransient<Views.FeishuView>();
        services.AddTransient<Views.ApiView>();

        // 注册 ViewModel
        services.AddTransient<ViewModels.MainViewModel>();
        services.AddTransient<ViewModels.FeishuViewModel>();
        services.AddTransient<ViewModels.ApiViewModel>();

        // 注册服务
        services.AddSingleton<DbService>();
        services.AddSingleton<FeishuWebService>();
        services.AddSingleton<ApiService>();
        services.AddSingleton<ScheduleService>();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);

        try
        {
            // 停止所有运行中的调度
            var scheduleService = ServiceProvider.GetService<ScheduleService>();
            scheduleService?.StopAllSchedules();
        }
        catch
        {
            // 忽略退出时的清理异常
        }

        if (_host != null)
        {
            try
            {
                await _host.StopAsync();
            }
            catch
            {
                // 忽略退出时的停止异常
            }
            _host.Dispose();
        }
    }
}
