using System;

namespace Feishu.Context.Models;

public class FeishuMessage
{
    public int Id { get; set; }
    public string ChatId { get; set; } = string.Empty;
    public string ChatName { get; set; } = string.Empty;
    public string MessageId { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string MessageType { get; set; } = string.Empty;
    public DateTime MessageTime { get; set; }
    public DateTime CollectedAt { get; set; }
}
