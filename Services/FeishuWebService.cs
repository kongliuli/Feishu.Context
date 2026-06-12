#nullable enable
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Wpf;

namespace Feishu.Context.Services;

/// <summary>
/// 飞书 WebView2 服务，管理持久登录和消息采集
/// </summary>
public class FeishuWebService
{
    private static readonly string UserDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Feishu.Context",
        "WebView2Data");

    private WebView2? _webView;

    /// <summary>
    /// 初始化 WebView2，使用持久化用户数据目录
    /// </summary>
    public async Task InitializeAsync(WebView2 webView)
    {
        _webView = webView;

        var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(
            userDataFolder: UserDataFolder);

        await _webView.EnsureCoreWebView2Async(env);

        _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
        _webView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
    }

    /// <summary>
    /// 导航到飞书消息页面
    /// </summary>
    public void NavigateToFeishu()
    {
        if (_webView?.CoreWebView2 != null)
        {
            _webView.CoreWebView2.Navigate("https://feishu.cn/messenger/");
        }
    }

    /// <summary>
    /// 检查用户是否已登录飞书
    /// </summary>
    public async Task<bool> IsLoggedInAsync()
    {
        if (_webView?.CoreWebView2 == null)
            return false;

        var url = _webView.CoreWebView2.Source;
        // 已登录时 URL 不会停留在登录页
        if (string.IsNullOrEmpty(url))
            return false;

        // 登录页面通常包含 /login 或 /accounts 路径
        if (url.Contains("/login", StringComparison.OrdinalIgnoreCase) ||
            url.Contains("/accounts", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // 检查页面中是否存在用户头像或用户名元素
        var result = await _webView.CoreWebView2.ExecuteScriptAsync(
            "JSON.stringify(document.querySelector('.avatar-container, .user-avatar, [class*=avatar]') !== null)");

        try
        {
            var jsonDoc = JsonDocument.Parse(result);
            return jsonDoc.RootElement.GetBoolean();
        }
        catch
        {
            return !url.Contains("/login", StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// 从当前聊天中采集消息
    /// </summary>
    public async Task<string> CollectMessagesAsync(string chatId)
    {
        if (_webView?.CoreWebView2 == null)
            return "[]";

        var script = @"
            (function() {
                var messages = [];
                var messageElements = document.querySelectorAll(
                    '[class*=""message-content""], [class*=""msg-content""], [class*=""chat-msg""], [data-message-id]'
                );
                messageElements.forEach(function(el) {
                    var contentEl = el.querySelector('[class*=""text-content""], [class*=""msg-text""], [class*=""message-text""]') || el;
                    var senderEl = el.querySelector('[class*=""sender-name""], [class*=""user-name""], [class*=""author-name""]') || el.closest('[class*=""message-item""]')?.querySelector('[class*=""sender-name""], [class*=""user-name""]') ;
                    var timeEl = el.querySelector('[class*=""time""], [class*=""timestamp""]') || el.closest('[class*=""message-item""]')?.querySelector('[class*=""time""], [class*=""timestamp""]');

                    messages.push({
                        messageId: el.getAttribute('data-message-id') || '',
                        content: contentEl ? contentEl.innerText.trim() : '',
                        senderName: senderEl ? senderEl.innerText.trim() : '',
                        senderId: senderEl ? (senderEl.getAttribute('data-user-id') || '') : '',
                        messageType: 'text',
                        messageTime: timeEl ? timeEl.innerText.trim() : ''
                    });
                });
                return JSON.stringify(messages);
            })();
        ";

        var result = await _webView.CoreWebView2.ExecuteScriptAsync(script);
        return result;
    }

    /// <summary>
    /// 获取聊天列表
    /// </summary>
    public async Task<string> GetChatListAsync()
    {
        if (_webView?.CoreWebView2 == null)
            return "[]";

        var script = @"
            (function() {
                var chats = [];
                var chatElements = document.querySelectorAll(
                    '[class*=""chat-item""], [class*=""conversation-item""], [class*=""nav-item""]'
                );
                chatElements.forEach(function(el) {
                    var nameEl = el.querySelector('[class*=""chat-name""], [class*=""conv-name""], [class*=""title""]') || el;
                    chats.push({
                        chatId: el.getAttribute('data-chat-id') || el.getAttribute('data-id') || '',
                        chatName: nameEl ? nameEl.innerText.trim() : ''
                    });
                });
                return JSON.stringify(chats);
            })();
        ";

        var result = await _webView.CoreWebView2.ExecuteScriptAsync(script);
        return result;
    }

    /// <summary>
    /// 导航到指定聊天
    /// </summary>
    public void NavigateToChat(string chatId)
    {
        if (_webView?.CoreWebView2 != null)
        {
            _webView.CoreWebView2.Navigate($"https://feishu.cn/messenger/chats/{chatId}");
        }
    }

    /// <summary>
    /// 获取当前页面 URL
    /// </summary>
    public string? GetCurrentUrl()
    {
        return _webView?.CoreWebView2?.Source;
    }

    private void OnNavigationCompleted(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
    {
        NavigationCompleted?.Invoke(this, e.IsSuccess);
    }

    private void OnWebMessageReceived(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
    {
        WebMessageReceived?.Invoke(this, e.WebMessageAsJson);
    }

    /// <summary>
    /// 页面导航完成事件
    /// </summary>
    public event Action<FeishuWebService, bool>? NavigationCompleted;

    /// <summary>
    /// 接收到 Web 消息事件
    /// </summary>
    public event Action<FeishuWebService, string>? WebMessageReceived;
}
