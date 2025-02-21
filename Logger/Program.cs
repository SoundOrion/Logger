using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Json;
using Serilog.Sinks.Async;
using Serilog.Sinks.Grafana.Loki;
using System.Collections.Generic;
using System.Net.Http;

//Log.Logger = new LoggerConfiguration()
//    .WriteTo.Console()
//    .WriteTo.Async(a => a.File("logs/log.txt",
//        rollingInterval: RollingInterval.Day,
//        buffered: true,
//        flushToDiskInterval: TimeSpan.FromSeconds(5),
//        retainedFileCountLimit: 7))
//    .WriteTo.GrafanaLoki("http://localhost:3100",
//        new List<LokiLabel>
//        {
//                    new LokiLabel { Key = "app", Value = "my-app" },
//                    new LokiLabel { Key = "env", Value = "production" }
//        },
//        batchPostingLimit: 50, // 50件ごとに送信
//        queueLimit: 1000, // キューの上限を1000件に設定
//        textFormatter: new JsonFormatter()) // JSON 形式で送信
//    .CreateLogger();


//エラーログを別ファイルに出力
//.WriteTo.Async(a => a.File("logs/log.txt",
//    rollingInterval: RollingInterval.Day,
//    buffered: true,
//    flushToDiskInterval: TimeSpan.FromSeconds(5),
//    retainedFileCountLimit: 7))
//.WriteTo.Async(a => a.File("logs/error.log",
//    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error, // エラー以上のみ
//    rollingInterval: RollingInterval.Day,
//    buffered: true,
//    flushToDiskInterval: TimeSpan.FromSeconds(5),
//    retainedFileCountLimit: 7))


var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var httpClient = new HttpClient(new CustomLokiHttpHandler())
{
    BaseAddress = new Uri("http://localhost:3100")
};

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .WriteTo.Console()
    .WriteTo.Async(a => a.File("logs/log.txt",
        rollingInterval: RollingInterval.Day,
        buffered: true,
        flushToDiskInterval: TimeSpan.FromSeconds(2),
        retainedFileCountLimit: 7))
.WriteTo.GrafanaLoki(httpClient, // Loki 送信エラーを処理する HttpClient
        new List<LokiLabel> { new LokiLabel { Key = "app", Value = "my-app" } },
        batchPostingLimit: 50,
        queueLimit: 1000,
        textFormatter: new CompactJsonFormatter())
    .CreateLogger();

Log.Information("Loki にログを送信しつつ、ファイルにも記録中！");
Log.Warning("これは警告ログです");
Log.Error("エラーメッセージの送信");

Log.CloseAndFlush(); // アプリ終了時にフラッシュ

public class CustomLokiHttpHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await base.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = $"Loki 送信エラー: {response.StatusCode} {await response.Content.ReadAsStringAsync()}";
                Console.WriteLine(errorMessage);
                BackupLog(request);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Loki 送信例外: {ex.Message}");
            BackupLog(request);
            throw;
        }
        return response;
    }

    private void BackupLog(HttpRequestMessage request)
    {
        try
        {
            var logData = request.Content?.ReadAsStringAsync().Result;
            if (!string.IsNullOrEmpty(logData))
            {
                File.AppendAllText("logs/loki_backup.log", $"{DateTime.UtcNow}: {logData}\n");
                Console.WriteLine("ログをバックアップファイルに保存しました。");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"バックアップログの保存に失敗: {ex.Message}");
        }
    }
}
