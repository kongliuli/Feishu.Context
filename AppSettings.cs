namespace Feishu.Context;

/// <summary>
/// 应用配置模型，映射 appsettings.json
/// </summary>
public class AppSettings
{
    public FeishuSettings Feishu { get; set; } = new();
    public ApiSettings Api { get; set; } = new();
    public DatabaseSettings Database { get; set; } = new();
}

public class FeishuSettings
{
    public string WebUrl { get; set; } = "https://feishu.cn/messenger/";
    public string UserDataFolder { get; set; } = string.Empty;
    public int MessagePollingIntervalSeconds { get; set; } = 30;
}

public class ApiSettings
{
    public int DefaultTimeoutSeconds { get; set; } = 30;
    public int MaxRetryCount { get; set; } = 3;
}

public class DatabaseSettings
{
    public string ConnectionString { get; set; } = string.Empty;
}
