using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Feishu.Context.Data;
using Feishu.Context.Models;

namespace Feishu.Context.Services;

public class ApiService
{
    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public async Task<ApiScheduleRecord> ExecuteAsync(ApiConfig config)
    {
        var record = new ApiScheduleRecord
        {
            ApiConfigId = config.Id,
            ApiName = config.Name,
            RequestUrl = config.Url,
            RequestMethod = config.Method,
            RequestBody = config.BodyTemplate,
            ExecutedAt = DateTime.Now
        };

        try
        {
            using var request = new HttpRequestMessage(
                new HttpMethod(config.Method.ToUpperInvariant()),
                config.Url);

            // Apply custom headers
            if (!string.IsNullOrWhiteSpace(config.Headers))
            {
                try
                {
                    var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(config.Headers);
                    if (headers != null)
                    {
                        foreach (var kvp in headers)
                        {
                            request.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
                        }
                    }
                }
                catch (JsonException)
                {
                    // Ignore invalid header JSON
                }
            }

            // Apply body for POST
            if (config.Method.Equals("POST", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(config.BodyTemplate))
            {
                request.Content = new StringContent(config.BodyTemplate, Encoding.UTF8, "application/json");
            }

            using var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            record.StatusCode = (int)response.StatusCode;
            record.ResponseBody = responseBody;
            record.IsSuccess = response.IsSuccessStatusCode;

            if (!response.IsSuccessStatusCode)
            {
                record.ErrorMessage = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";
            }
        }
        catch (TaskCanceledException)
        {
            record.StatusCode = 0;
            record.IsSuccess = false;
            record.ErrorMessage = "请求超时";
        }
        catch (HttpRequestException ex)
        {
            record.StatusCode = 0;
            record.IsSuccess = false;
            record.ErrorMessage = $"网络错误: {ex.Message}";
        }
        catch (Exception ex)
        {
            record.StatusCode = 0;
            record.IsSuccess = false;
            record.ErrorMessage = $"执行异常: {ex.Message}";
        }

        return record;
    }

    public async Task<ApiScheduleRecord> ExecuteWithRecordAsync(ApiConfig config, DbService dbService)
    {
        var record = await ExecuteAsync(config);
        await dbService.InsertAsync(record);
        return record;
    }
}
