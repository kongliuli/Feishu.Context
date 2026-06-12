#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Feishu.Context.Models;
using Microsoft.Data.Sqlite;

namespace Feishu.Context.Data;

/// <summary>
/// SQLite 数据库单例服务，管理数据库的创建与 CRUD 操作
/// </summary>
public class DbService
{
    private const string DatabaseFileName = "feishu.db";
    private static readonly string DatabaseDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Feishu.Context");
    private static readonly string DatabasePath = Path.Combine(DatabaseDirectory, DatabaseFileName);

    private string ConnectionString => $"Data Source={DatabasePath}";

    /// <summary>
    /// 初始化数据库，创建目录和表
    /// </summary>
    public async Task InitializeAsync()
    {
        if (!Directory.Exists(DatabaseDirectory))
        {
            Directory.CreateDirectory(DatabaseDirectory);
        }

        using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS feishu_messages (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ChatId TEXT NOT NULL,
                ChatName TEXT NOT NULL DEFAULT '',
                MessageId TEXT NOT NULL,
                SenderId TEXT NOT NULL,
                SenderName TEXT NOT NULL DEFAULT '',
                Content TEXT NOT NULL DEFAULT '',
                MessageType TEXT NOT NULL DEFAULT '',
                MessageTime TEXT NOT NULL,
                CollectedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS api_configs (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Url TEXT NOT NULL,
                Method TEXT NOT NULL DEFAULT 'GET',
                Headers TEXT NOT NULL DEFAULT '',
                BodyTemplate TEXT NOT NULL DEFAULT '',
                ScheduleIntervalSeconds INTEGER NOT NULL DEFAULT 0,
                IsEnabled INTEGER NOT NULL DEFAULT 1,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS api_schedule_records (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ApiConfigId INTEGER NOT NULL,
                ApiName TEXT NOT NULL DEFAULT '',
                RequestUrl TEXT NOT NULL DEFAULT '',
                RequestMethod TEXT NOT NULL DEFAULT '',
                RequestBody TEXT NOT NULL DEFAULT '',
                StatusCode INTEGER NOT NULL DEFAULT 0,
                ResponseBody TEXT NOT NULL DEFAULT '',
                IsSuccess INTEGER NOT NULL DEFAULT 0,
                ErrorMessage TEXT NOT NULL DEFAULT '',
                ExecutedAt TEXT NOT NULL,
                FOREIGN KEY (ApiConfigId) REFERENCES api_configs(Id)
            );
        ";
        await command.ExecuteNonQueryAsync();
    }

    #region FeishuMessage CRUD

    public async Task<int> InsertAsync(FeishuMessage message)
    {
        using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO feishu_messages (ChatId, ChatName, MessageId, SenderId, SenderName, Content, MessageType, MessageTime, CollectedAt)
            VALUES ($ChatId, $ChatName, $MessageId, $SenderId, $SenderName, $Content, $MessageType, $MessageTime, $CollectedAt);
            SELECT last_insert_rowid();
        ";
        command.Parameters.AddWithValue("$ChatId", message.ChatId);
        command.Parameters.AddWithValue("$ChatName", message.ChatName);
        command.Parameters.AddWithValue("$MessageId", message.MessageId);
        command.Parameters.AddWithValue("$SenderId", message.SenderId);
        command.Parameters.AddWithValue("$SenderName", message.SenderName);
        command.Parameters.AddWithValue("$Content", message.Content);
        command.Parameters.AddWithValue("$MessageType", message.MessageType);
        command.Parameters.AddWithValue("$MessageTime", message.MessageTime.ToString("O"));
        command.Parameters.AddWithValue("$CollectedAt", message.CollectedAt.ToString("O"));

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public async Task<List<FeishuMessage>> GetByChatIdAsync(string chatId)
    {
        var messages = new List<FeishuMessage>();
        using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM feishu_messages WHERE ChatId = $ChatId ORDER BY MessageTime DESC";
        command.Parameters.AddWithValue("$ChatId", chatId);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            messages.Add(ReadFeishuMessage(reader));
        }

        return messages;
    }

    public async Task<List<FeishuMessage>> GetByTimeRangeAsync(DateTime start, DateTime end)
    {
        var messages = new List<FeishuMessage>();
        using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM feishu_messages WHERE MessageTime >= $Start AND MessageTime <= $End ORDER BY MessageTime DESC";
        command.Parameters.AddWithValue("$Start", start.ToString("O"));
        command.Parameters.AddWithValue("$End", end.ToString("O"));

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            messages.Add(ReadFeishuMessage(reader));
        }

        return messages;
    }

    public async Task<List<FeishuMessage>> GetAllAsync()
    {
        var messages = new List<FeishuMessage>();
        using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM feishu_messages ORDER BY MessageTime DESC";

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            messages.Add(ReadFeishuMessage(reader));
        }

        return messages;
    }

    public async Task DeleteAsync(int id)
    {
        using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM feishu_messages WHERE Id = $Id";
        command.Parameters.AddWithValue("$Id", id);

        await command.ExecuteNonQueryAsync();
    }

    private static FeishuMessage ReadFeishuMessage(SqliteDataReader reader)
    {
        return new FeishuMessage
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            ChatId = reader.GetString(reader.GetOrdinal("ChatId")),
            ChatName = reader.GetString(reader.GetOrdinal("ChatName")),
            MessageId = reader.GetString(reader.GetOrdinal("MessageId")),
            SenderId = reader.GetString(reader.GetOrdinal("SenderId")),
            SenderName = reader.GetString(reader.GetOrdinal("SenderName")),
            Content = reader.GetString(reader.GetOrdinal("Content")),
            MessageType = reader.GetString(reader.GetOrdinal("MessageType")),
            MessageTime = DateTime.Parse(reader.GetString(reader.GetOrdinal("MessageTime"))),
            CollectedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("CollectedAt")))
        };
    }

    #endregion

    #region ApiConfig CRUD

    public async Task<int> InsertAsync(ApiConfig config)
    {
        using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO api_configs (Name, Url, Method, Headers, BodyTemplate, ScheduleIntervalSeconds, IsEnabled, CreatedAt, UpdatedAt)
            VALUES ($Name, $Url, $Method, $Headers, $BodyTemplate, $ScheduleIntervalSeconds, $IsEnabled, $CreatedAt, $UpdatedAt);
            SELECT last_insert_rowid();
        ";
        command.Parameters.AddWithValue("$Name", config.Name);
        command.Parameters.AddWithValue("$Url", config.Url);
        command.Parameters.AddWithValue("$Method", config.Method);
        command.Parameters.AddWithValue("$Headers", config.Headers);
        command.Parameters.AddWithValue("$BodyTemplate", config.BodyTemplate);
        command.Parameters.AddWithValue("$ScheduleIntervalSeconds", config.ScheduleIntervalSeconds);
        command.Parameters.AddWithValue("$IsEnabled", config.IsEnabled ? 1 : 0);
        command.Parameters.AddWithValue("$CreatedAt", config.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("$UpdatedAt", config.UpdatedAt.ToString("O"));

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public async Task UpdateAsync(ApiConfig config)
    {
        using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE api_configs
            SET Name = $Name, Url = $Url, Method = $Method, Headers = $Headers,
                BodyTemplate = $BodyTemplate, ScheduleIntervalSeconds = $ScheduleIntervalSeconds,
                IsEnabled = $IsEnabled, UpdatedAt = $UpdatedAt
            WHERE Id = $Id
        ";
        command.Parameters.AddWithValue("$Name", config.Name);
        command.Parameters.AddWithValue("$Url", config.Url);
        command.Parameters.AddWithValue("$Method", config.Method);
        command.Parameters.AddWithValue("$Headers", config.Headers);
        command.Parameters.AddWithValue("$BodyTemplate", config.BodyTemplate);
        command.Parameters.AddWithValue("$ScheduleIntervalSeconds", config.ScheduleIntervalSeconds);
        command.Parameters.AddWithValue("$IsEnabled", config.IsEnabled ? 1 : 0);
        command.Parameters.AddWithValue("$UpdatedAt", config.UpdatedAt.ToString("O"));
        command.Parameters.AddWithValue("$Id", config.Id);

        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteApiConfigAsync(int id)
    {
        using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM api_configs WHERE Id = $Id";
        command.Parameters.AddWithValue("$Id", id);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<ApiConfig>> GetAllApiConfigsAsync()
    {
        var configs = new List<ApiConfig>();
        using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM api_configs ORDER BY CreatedAt DESC";

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            configs.Add(ReadApiConfig(reader));
        }

        return configs;
    }

    public async Task<ApiConfig?> GetByIdAsync(int id)
    {
        using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM api_configs WHERE Id = $Id";
        command.Parameters.AddWithValue("$Id", id);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return ReadApiConfig(reader);
        }

        return null;
    }

    private static ApiConfig ReadApiConfig(SqliteDataReader reader)
    {
        return new ApiConfig
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            Url = reader.GetString(reader.GetOrdinal("Url")),
            Method = reader.GetString(reader.GetOrdinal("Method")),
            Headers = reader.GetString(reader.GetOrdinal("Headers")),
            BodyTemplate = reader.GetString(reader.GetOrdinal("BodyTemplate")),
            ScheduleIntervalSeconds = reader.GetInt32(reader.GetOrdinal("ScheduleIntervalSeconds")),
            IsEnabled = reader.GetInt32(reader.GetOrdinal("IsEnabled")) == 1,
            CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("CreatedAt"))),
            UpdatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("UpdatedAt")))
        };
    }

    #endregion

    #region ApiScheduleRecord CRUD

    public async Task<int> InsertAsync(ApiScheduleRecord record)
    {
        using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO api_schedule_records (ApiConfigId, ApiName, RequestUrl, RequestMethod, RequestBody, StatusCode, ResponseBody, IsSuccess, ErrorMessage, ExecutedAt)
            VALUES ($ApiConfigId, $ApiName, $RequestUrl, $RequestMethod, $RequestBody, $StatusCode, $ResponseBody, $IsSuccess, $ErrorMessage, $ExecutedAt);
            SELECT last_insert_rowid();
        ";
        command.Parameters.AddWithValue("$ApiConfigId", record.ApiConfigId);
        command.Parameters.AddWithValue("$ApiName", record.ApiName);
        command.Parameters.AddWithValue("$RequestUrl", record.RequestUrl);
        command.Parameters.AddWithValue("$RequestMethod", record.RequestMethod);
        command.Parameters.AddWithValue("$RequestBody", record.RequestBody);
        command.Parameters.AddWithValue("$StatusCode", record.StatusCode);
        command.Parameters.AddWithValue("$ResponseBody", record.ResponseBody);
        command.Parameters.AddWithValue("$IsSuccess", record.IsSuccess ? 1 : 0);
        command.Parameters.AddWithValue("$ErrorMessage", record.ErrorMessage);
        command.Parameters.AddWithValue("$ExecutedAt", record.ExecutedAt.ToString("O"));

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public async Task<List<ApiScheduleRecord>> GetByApiConfigIdAsync(int apiConfigId)
    {
        var records = new List<ApiScheduleRecord>();
        using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM api_schedule_records WHERE ApiConfigId = $ApiConfigId ORDER BY ExecutedAt DESC";
        command.Parameters.AddWithValue("$ApiConfigId", apiConfigId);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            records.Add(ReadApiScheduleRecord(reader));
        }

        return records;
    }

    public async Task<List<ApiScheduleRecord>> GetAllApiScheduleRecordsAsync()
    {
        var records = new List<ApiScheduleRecord>();
        using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM api_schedule_records ORDER BY ExecutedAt DESC";

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            records.Add(ReadApiScheduleRecord(reader));
        }

        return records;
    }

    private static ApiScheduleRecord ReadApiScheduleRecord(SqliteDataReader reader)
    {
        return new ApiScheduleRecord
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            ApiConfigId = reader.GetInt32(reader.GetOrdinal("ApiConfigId")),
            ApiName = reader.GetString(reader.GetOrdinal("ApiName")),
            RequestUrl = reader.GetString(reader.GetOrdinal("RequestUrl")),
            RequestMethod = reader.GetString(reader.GetOrdinal("RequestMethod")),
            RequestBody = reader.GetString(reader.GetOrdinal("RequestBody")),
            StatusCode = reader.GetInt32(reader.GetOrdinal("StatusCode")),
            ResponseBody = reader.GetString(reader.GetOrdinal("ResponseBody")),
            IsSuccess = reader.GetInt32(reader.GetOrdinal("IsSuccess")) == 1,
            ErrorMessage = reader.GetString(reader.GetOrdinal("ErrorMessage")),
            ExecutedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("ExecutedAt")))
        };
    }

    #endregion
}
