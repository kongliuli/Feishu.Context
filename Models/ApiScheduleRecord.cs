using System;

namespace Feishu.Context.Models;

public class ApiScheduleRecord
{
    public int Id { get; set; }
    public int ApiConfigId { get; set; }
    public string ApiName { get; set; } = string.Empty;
    public string RequestUrl { get; set; } = string.Empty;
    public string RequestMethod { get; set; } = string.Empty;
    public string RequestBody { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string ResponseBody { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime ExecutedAt { get; set; }
}
