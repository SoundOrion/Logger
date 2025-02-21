using Serilog;
using Serilog.Formatting.Json;
using Serilog.Sinks.Async;
using Serilog.Sinks.Grafana.Loki;
using System.Collections.Generic;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.Async(a => a.File("logs/log.txt",
        rollingInterval: RollingInterval.Day,
        buffered: true,
        flushToDiskInterval: TimeSpan.FromSeconds(5),
        retainedFileCountLimit: 7))
    .WriteTo.GrafanaLoki("http://localhost:3100",
        new List<LokiLabel>
        {
                    new LokiLabel { Key = "app", Value = "my-app" },
                    new LokiLabel { Key = "env", Value = "production" }
        },
        batchPostingLimit: 50, // 50件ごとに送信
        queueLimit: 1000, // キューの上限を1000件に設定
        textFormatter: new JsonFormatter()) // JSON 形式で送信
    .CreateLogger();

Log.Information("Loki にログを送信しつつ、ファイルにも記録中！");
Log.Warning("これは警告ログです");
Log.Error("エラーメッセージの送信");

Log.CloseAndFlush(); // アプリ終了時にフラッシュ