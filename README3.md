using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZLogger;

// 🚀 DIコンテナにロギングサービスを登録
var services = new ServiceCollection();
services.AddLogging(loggingBuilder =>
{
    loggingBuilder.ClearProviders();
    loggingBuilder.AddZLoggerConsole(); // ZLogger（コンソール出力）
    loggingBuilder.AddZLoggerFile("logs/zlog.txt"); // ZLogger（ファイル出力）
});

var serviceProvider = services.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

// 🚀 ログの出力テスト
logger.LogInformation("✅ [{MachineName}]ZLogger + Loki + File ロギングテスト", Environment.MachineName);
logger.LogWarning("⚠️ [{MachineName}]警告メッセージのテスト", Environment.MachineName);
logger.LogError("❌ [{MachineName}]エラーメッセージのテスト: {ErrorDetail}", new Exception("サンプル例外"), Environment.MachineName);

Console.WriteLine("🔹 ロギング完了");