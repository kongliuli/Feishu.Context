#nullable enable
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Feishu.Context.Data;
using Feishu.Context.Models;
using Feishu.Context.Services;

namespace Feishu.Context.ViewModels;

public partial class ApiViewModel : ObservableObject
{
    private readonly DbService _dbService;
    private readonly ApiService _apiService;
    private readonly ScheduleService _scheduleService;

    public ApiViewModel(DbService dbService, ApiService apiService, ScheduleService scheduleService)
    {
        _dbService = dbService;
        _apiService = apiService;
        _scheduleService = scheduleService;

        _ = LoadConfigsAsync();
    }

    [ObservableProperty]
    private ObservableCollection<ApiConfig> _apiConfigs = [];

    [ObservableProperty]
    private ApiConfig? _selectedApiConfig;

    [ObservableProperty]
    private ObservableCollection<ApiScheduleRecord> _scheduleRecords = [];

    [ObservableProperty]
    private string _configName = string.Empty;

    [ObservableProperty]
    private string _configUrl = string.Empty;

    [ObservableProperty]
    private string _configMethod = "GET";

    [ObservableProperty]
    private string _configHeaders = string.Empty;

    [ObservableProperty]
    private string _configBody = string.Empty;

    [ObservableProperty]
    private int _configInterval;

    [ObservableProperty]
    private bool _isExecuting;

    [ObservableProperty]
    private string _statusMessage = "就绪";

    partial void OnSelectedApiConfigChanged(ApiConfig? value)
    {
        if (value != null)
        {
            ConfigName = value.Name;
            ConfigUrl = value.Url;
            ConfigMethod = value.Method;
            ConfigHeaders = value.Headers;
            ConfigBody = value.BodyTemplate;
            ConfigInterval = value.ScheduleIntervalSeconds;

            _ = LoadRecordsForConfigAsync(value.Id);
        }
    }

    [RelayCommand]
    private async Task AddConfigAsync()
    {
        try
        {
            var config = new ApiConfig
            {
                Name = ConfigName,
                Url = ConfigUrl,
                Method = ConfigMethod,
                Headers = ConfigHeaders,
                BodyTemplate = ConfigBody,
                ScheduleIntervalSeconds = ConfigInterval,
                IsEnabled = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            var id = await _dbService.InsertAsync(config);
            config.Id = id;
            ApiConfigs.Insert(0, config);
            StatusMessage = $"已添加配置: {config.Name}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"添加失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task UpdateConfigAsync()
    {
        if (SelectedApiConfig == null)
        {
            StatusMessage = "请先选择一个配置";
            return;
        }

        try
        {
            SelectedApiConfig.Name = ConfigName;
            SelectedApiConfig.Url = ConfigUrl;
            SelectedApiConfig.Method = ConfigMethod;
            SelectedApiConfig.Headers = ConfigHeaders;
            SelectedApiConfig.BodyTemplate = ConfigBody;
            SelectedApiConfig.ScheduleIntervalSeconds = ConfigInterval;
            SelectedApiConfig.UpdatedAt = DateTime.Now;

            await _dbService.UpdateAsync(SelectedApiConfig);

            // If schedule is active, restart it with updated config
            if (_scheduleService.ActiveScheduleCount > 0)
            {
                // Refresh the item in the collection
                var index = ApiConfigs.IndexOf(SelectedApiConfig);
                if (index >= 0)
                {
                    ApiConfigs[index] = SelectedApiConfig;
                }
            }

            StatusMessage = $"已更新配置: {SelectedApiConfig.Name}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"更新失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteConfigAsync()
    {
        if (SelectedApiConfig == null)
        {
            StatusMessage = "请先选择一个配置";
            return;
        }

        try
        {
            var name = SelectedApiConfig.Name;
            _scheduleService.StopSchedule(SelectedApiConfig.Id);
            await _dbService.DeleteApiConfigAsync(SelectedApiConfig.Id);
            ApiConfigs.Remove(SelectedApiConfig);
            SelectedApiConfig = null;
            ScheduleRecords.Clear();
            StatusMessage = $"已删除配置: {name}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"删除失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ExecuteAsync()
    {
        if (SelectedApiConfig == null)
        {
            StatusMessage = "请先选择一个配置";
            return;
        }

        IsExecuting = true;
        StatusMessage = $"正在执行: {SelectedApiConfig.Name}...";

        try
        {
            var record = await _apiService.ExecuteWithRecordAsync(SelectedApiConfig, _dbService);
            ScheduleRecords.Insert(0, record);
            StatusMessage = record.IsSuccess
                ? $"执行成功: {SelectedApiConfig.Name} (HTTP {record.StatusCode})"
                : $"执行失败: {SelectedApiConfig.Name} - {record.ErrorMessage}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"执行异常: {ex.Message}";
        }
        finally
        {
            IsExecuting = false;
        }
    }

    [RelayCommand]
    private async Task ToggleScheduleAsync()
    {
        if (SelectedApiConfig == null)
        {
            StatusMessage = "请先选择一个配置";
            return;
        }

        if (SelectedApiConfig.ScheduleIntervalSeconds <= 0)
        {
            StatusMessage = "请设置调度间隔（大于0秒）";
            return;
        }

        // Check if schedule is already running for this config
        var isRunning = _scheduleService.ActiveScheduleCount > 0 &&
                        ApiConfigs.Any(c => c.Id == SelectedApiConfig.Id);

        // Simple check: try to stop first; if it was running, we're done
        // We need a better way to check - let's use a helper
        var wasRunning = StopScheduleIfExists(SelectedApiConfig.Id);

        if (wasRunning)
        {
            StatusMessage = $"已停止调度: {SelectedApiConfig.Name}";
        }
        else
        {
            _scheduleService.StartSchedule(SelectedApiConfig, record =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    ScheduleRecords.Insert(0, record);
                    StatusMessage = record.IsSuccess
                        ? $"调度执行成功: {record.ApiName} (HTTP {record.StatusCode})"
                        : $"调度执行失败: {record.ApiName} - {record.ErrorMessage}";
                });
            });
            StatusMessage = $"已启动调度: {SelectedApiConfig.Name} (间隔 {SelectedApiConfig.ScheduleIntervalSeconds} 秒)";
        }

        await Task.CompletedTask;
    }

    private bool StopScheduleIfExists(int apiConfigId)
    {
        // ScheduleService doesn't expose a "Contains" check, so we use a workaround:
        // Try to stop; if ActiveScheduleCount decreases, it was running
        var before = _scheduleService.ActiveScheduleCount;
        _scheduleService.StopSchedule(apiConfigId);
        var after = _scheduleService.ActiveScheduleCount;
        return before > after;
    }

    [RelayCommand]
    private async Task RefreshRecordsAsync()
    {
        if (SelectedApiConfig != null)
        {
            await LoadRecordsForConfigAsync(SelectedApiConfig.Id);
        }
        else
        {
            var records = await _dbService.GetAllApiScheduleRecordsAsync();
            ScheduleRecords = new ObservableCollection<ApiScheduleRecord>(records);
        }
        StatusMessage = "记录已刷新";
    }

    [RelayCommand]
    private async Task RefreshConfigsAsync()
    {
        await LoadConfigsAsync();
        StatusMessage = "配置已刷新";
    }

    private async Task LoadConfigsAsync()
    {
        try
        {
            var configs = await _dbService.GetAllApiConfigsAsync();
            ApiConfigs = new ObservableCollection<ApiConfig>(configs);
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载配置失败: {ex.Message}";
        }
    }

    private async Task LoadRecordsForConfigAsync(int apiConfigId)
    {
        try
        {
            var records = await _dbService.GetByApiConfigIdAsync(apiConfigId);
            ScheduleRecords = new ObservableCollection<ApiScheduleRecord>(records);
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载记录失败: {ex.Message}";
        }
    }
}
