using System;
using System.Collections.Generic;
using System.Windows.Threading;
using Feishu.Context.Data;
using Feishu.Context.Models;

namespace Feishu.Context.Services;

public class ScheduleService
{
    private readonly Dictionary<int, DispatcherTimer> _timers = new();

    public int ActiveScheduleCount => _timers.Count;

    public void StartSchedule(ApiConfig config, Action<ApiScheduleRecord> onCompleted)
    {
        if (_timers.ContainsKey(config.Id))
        {
            StopSchedule(config.Id);
        }

        if (config.ScheduleIntervalSeconds <= 0)
        {
            return;
        }

        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(config.ScheduleIntervalSeconds)
        };

        timer.Tick += async (_, _) =>
        {
            var apiService = App.ServiceProvider.GetService(typeof(ApiService)) as ApiService;
            var dbService = App.ServiceProvider.GetService(typeof(DbService)) as DbService;

            if (apiService == null || dbService == null) return;

            var record = await apiService.ExecuteWithRecordAsync(config, dbService);
            onCompleted?.Invoke(record);
        };

        _timers[config.Id] = timer;
        timer.Start();
    }

    public void StopSchedule(int apiConfigId)
    {
        if (_timers.TryGetValue(apiConfigId, out var timer))
        {
            timer.Stop();
            _timers.Remove(apiConfigId);
        }
    }

    public void StopAllSchedules()
    {
        foreach (var timer in _timers.Values)
        {
            timer.Stop();
        }
        _timers.Clear();
    }
}
