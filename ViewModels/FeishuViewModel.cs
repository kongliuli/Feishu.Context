#nullable enable
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Feishu.Context.Data;
using Feishu.Context.Models;
using Feishu.Context.Services;

namespace Feishu.Context.ViewModels;

/// <summary>
/// 飞书采集 ViewModel
/// </summary>
public partial class FeishuViewModel : ObservableObject
{
    private readonly DbService _dbService;
    private readonly FeishuWebService _feishuWebService;

    public FeishuViewModel(DbService dbService, FeishuWebService feishuWebService)
    {
        _dbService = dbService;
        _feishuWebService = feishuWebService;

        _feishuWebService.NavigationCompleted += OnNavigationCompleted;
    }

    [ObservableProperty]
    private bool _isWebViewReady;

    [ObservableProperty]
    private bool _isLoggedIn;

    [ObservableProperty]
    private string _chatId = string.Empty;

    [ObservableProperty]
    private bool _isCollecting;

    [ObservableProperty]
    private string _statusMessage = "就绪";

    public ObservableCollection<FeishuMessage> CollectedMessages { get; } = new();

    /// <summary>
    /// 登录飞书
    /// </summary>
    [RelayCommand]
    private async Task LoginAsync()
    {
        StatusMessage = "正在打开飞书登录页面...";
        _feishuWebService.NavigateToFeishu();
        await Task.CompletedTask;
    }

    /// <summary>
    /// 采集消息
    /// </summary>
    [RelayCommand]
    private async Task CollectAsync()
    {
        if (string.IsNullOrWhiteSpace(ChatId))
        {
            StatusMessage = "请输入会话 ID";
            return;
        }

        if (!IsWebViewReady)
        {
            StatusMessage = "WebView2 尚未初始化";
            return;
        }

        IsCollecting = true;
        StatusMessage = "正在采集消息...";

        try
        {
            // 导航到指定聊天
            _feishuWebService.NavigateToChat(ChatId);

            // 等待页面加载
            await Task.Delay(3000);

            // 采集消息
            var jsonResult = await _feishuWebService.CollectMessagesAsync(ChatId);

            // 解析 JSON 结果
            var rawMessages = JsonSerializer.Deserialize<JsonElement>(jsonResult);

            if (rawMessages.ValueKind == JsonValueKind.Array)
            {
                int count = 0;
                foreach (var item in rawMessages.EnumerateArray())
                {
                    var message = new FeishuMessage
                    {
                        ChatId = ChatId,
                        ChatName = GetStringOrDefault(item, "chatName"),
                        MessageId = GetStringOrDefault(item, "messageId"),
                        SenderId = GetStringOrDefault(item, "senderId"),
                        SenderName = GetStringOrDefault(item, "senderName"),
                        Content = GetStringOrDefault(item, "content"),
                        MessageType = GetStringOrDefault(item, "messageType", "text"),
                        MessageTime = TryParseDateTime(GetStringOrDefault(item, "messageTime")),
                        CollectedAt = DateTime.Now
                    };

                    await _dbService.InsertAsync(message);

                    // 添加到界面列表（避免重复）
                    if (!CollectedMessages.Any(m => m.MessageId == message.MessageId && m.MessageId != string.Empty))
                    {
                        CollectedMessages.Insert(0, message);
                    }

                    count++;
                }

                StatusMessage = $"采集完成，共采集 {count} 条消息";
            }
            else
            {
                StatusMessage = "未采集到消息，请确认已打开目标聊天";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"采集失败：{ex.Message}";
        }
        finally
        {
            IsCollecting = false;
        }
    }

    /// <summary>
    /// 刷新消息记录
    /// </summary>
    [RelayCommand]
    private async Task RefreshMessagesAsync()
    {
        StatusMessage = "正在刷新记录...";

        try
        {
            var messages = await _dbService.GetAllAsync();
            CollectedMessages.Clear();
            foreach (var msg in messages)
            {
                CollectedMessages.Add(msg);
            }
            StatusMessage = $"已加载 {messages.Count} 条记录";
        }
        catch (Exception ex)
        {
            StatusMessage = $"刷新失败：{ex.Message}";
        }
    }

    /// <summary>
    /// 删除消息记录
    /// </summary>
    [RelayCommand]
    private async Task DeleteMessageAsync(object? parameter)
    {
        if (parameter is FeishuMessage message)
        {
            try
            {
                await _dbService.DeleteAsync(message.Id);
                CollectedMessages.Remove(message);
                StatusMessage = "已删除记录";
            }
            catch (Exception ex)
            {
                StatusMessage = $"删除失败：{ex.Message}";
            }
        }
    }

    /// <summary>
    /// WebView2 初始化完成回调
    /// </summary>
    public async Task OnWebViewInitializedAsync()
    {
        IsWebViewReady = true;
        StatusMessage = "WebView2 已就绪";

        // 检查登录状态
        var loggedIn = await _feishuWebService.IsLoggedInAsync();
        IsLoggedIn = loggedIn;
        StatusMessage = loggedIn ? "已登录飞书" : "未登录，请点击登录";
    }

    private void OnNavigationCompleted(FeishuWebService sender, bool isSuccess)
    {
        if (isSuccess)
        {
            _ = CheckLoginStatusAsync();
        }
    }

    private async Task CheckLoginStatusAsync()
    {
        var loggedIn = await _feishuWebService.IsLoggedInAsync();
        IsLoggedIn = loggedIn;
        if (loggedIn)
        {
            StatusMessage = "已登录飞书";
        }
    }

    private static string GetStringOrDefault(JsonElement element, string propertyName, string defaultValue = "")
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString() ?? defaultValue;
        }
        return defaultValue;
    }

    private static DateTime TryParseDateTime(string dateTimeStr)
    {
        if (DateTime.TryParse(dateTimeStr, out var result))
        {
            return result;
        }
        return DateTime.Now;
    }
}
