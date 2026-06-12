using System;

namespace Feishu.Context.Models;

public class ApiConfig
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Method { get; set; } = "GET"; // GET or POST
    public string Headers { get; set; } = string.Empty; // JSON format
    public string BodyTemplate { get; set; } = string.Empty; // JSON format for POST
    public int ScheduleIntervalSeconds { get; set; } // 0 means no auto schedule
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
