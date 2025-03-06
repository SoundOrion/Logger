using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using ZLogger;

var services = new ServiceCollection();

// ZLogger を `ILogger` に統合（ファイル出力 & コンソール出力）
services.AddLogging(builder =>
{
    builder.ClearProviders();
    builder.AddZLoggerConsole(); // コンソールログ
    builder.AddZLoggerFile("logs/zlogger.log"); // ファイル出力
    //builder.AddZLoggerLogProcessor(new BatchingHttpLogProcessor(10, null));
});

// `ServiceProvider` を作成
await using var provider = services.BuildServiceProvider();

// `ILoggerFactory` を取得
var loggerFactory = provider.GetRequiredService<ILoggerFactory>();

// `ILogger<Program>` を取得
var logger = provider.GetRequiredService<ILogger<Program>>();

// 🚀 ZLogger でログを出力
logger.LogInformation("ユーザー {UserId} が {Action} を実行しました", 12345, "ログイン");
logger.LogWarning("CPU使用率が {CpuUsage}% に達しました", 85);
logger.LogError(new Exception("接続エラー"), "データベース接続失敗。サーバー: {Server}", "db01");

using System.Buffers;
using ZLogger;

public class BatchingHttpLogProcessor : BatchingAsyncLogProcessor
{
    HttpClient httpClient;
    ArrayBufferWriter<byte> bufferWriter;
    IZLoggerFormatter formatter;

    public BatchingHttpLogProcessor(int batchSize, ZLoggerOptions options)
        : base(batchSize, options)
    {
        httpClient = new HttpClient();
        bufferWriter = new ArrayBufferWriter<byte>();
        formatter = options.CreateFormatter();
    }

    protected override async ValueTask ProcessAsync(IReadOnlyList<INonReturnableZLoggerEntry> list)
    {
        foreach (var item in list)
        {
            item.FormatUtf8(bufferWriter, formatter);
        }

        var byteArrayContent = new ByteArrayContent(bufferWriter.WrittenSpan.ToArray());
        await httpClient.PostAsync("http://foo", byteArrayContent).ConfigureAwait(false);

        bufferWriter.Clear();
    }

    protected override ValueTask DisposeAsyncCore()
    {
        httpClient.Dispose();
        return default;
    }
}