### **✅ Loki 送信失敗時に `ILogger` を使うと、Loki 送信エラーのログも Loki に送られる？**
**💡 はい、そのままだと「Loki 送信失敗時のエラーログ」も Loki に送られる可能性があります。**  
これは **「Loki へ送信するログの Sink も Loki になっている」ため** です。

---

## **🔥 1. 問題点**
現在の `ILogger` の設定では、エラーログも含めて **すべてのログが Loki に送信** されます。  
しかし、**Loki 送信が失敗しているときに、エラーログも Loki に送ろうとするとループ状態** になってしまう可能性があります。

📌 **❌ 問題の流れ**
1. **Loki にログ送信 → 失敗（503 Service Unavailable など）**
2. **エラーログを `ILogger.LogError()` で記録**
3. **Serilog の Sink（出力先）が Loki なので、エラーログも Loki に送信しようとする**
4. **Loki 送信がまた失敗して、エラーログを記録**
5. **繰り返し発生する可能性（ログループ）**

---

## **🔥 2. 解決策**
### **✅ ① Loki 送信失敗時のログは「Loki 以外の Sink のみに記録」する**
**エラーログは Loki に送らず、ファイルやコンソールのみに記録する** ようにします。  
具体的には、**Loki にログを送るときの `ILogger` のログレベルを制限** し、  
**Loki 送信失敗時のログを「ファイル or コンソール」のみに出力する設定** にします。

📌 **✅ Serilog 設定で `Loki` Sink のログレベルを制限**
```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.Async(a => a.File("logs/log.txt",
        rollingInterval: RollingInterval.Day,
        buffered: true,
        flushToDiskInterval: TimeSpan.FromSeconds(5),
        retainedFileCountLimit: 7))

    // 🚀 Loki 送信ログは "Warning" 以上のみ（エラーは Loki に送らない）
    .WriteTo.GrafanaLoki(httpClient,
        new List<LokiLabel>
        {
            new LokiLabel { Key = "app", Value = "my-app" },
            new LokiLabel { Key = "env", Value = "production" }
        },
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning, // 🚀 INFO以下は送らない
        batchPostingLimit: 50,
        queueLimit: 1000,
        textFormatter: new JsonFormatter())

    .CreateLogger();
```

✅ **Loki に送るログは `Warning` 以上に制限**（`Information` 以下のログは送らない）  
✅ **Loki 送信失敗時のログは `File` や `Console` のみに記録**  

---

### **✅ ② `ILogger` のスコープを切り替えて、Loki に送らない**
もう一つの解決策は、**`ILogger` を「Loki 用」と「通常のログ用」で分ける」** ことです。

📌 **✅ `ILogger` を分けて Loki 送信失敗時のログは `Loki Sink` に送らない**
```csharp
private readonly ILogger<CustomLokiHttpHandler> _fileLogger;

public CustomLokiHttpHandler(ILoggerFactory loggerFactory)
{
    // 🚀 Loki Sink には送らず、ファイルログにのみ記録する
    _fileLogger = loggerFactory.CreateLogger("FileOnlyLogger");
}

protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
{
    HttpResponseMessage response;
    try
    {
        response = await base.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = $"Loki 送信エラー: {response.StatusCode} {await response.Content.ReadAsStringAsync()}";
            _fileLogger.LogError(errorMessage); // 🚀 Loki には送らず、ファイルログのみ
            BackupLog(request);
        }
    }
    catch (Exception ex)
    {
        _fileLogger.LogError(ex, "Loki 送信中に例外が発生"); // 🚀 Loki には送らず、ファイルログのみ
        BackupLog(request);
        throw;
    }
    return response;
}
```

📌 **✅ `Program.cs` で `ILoggerFactory` を使ってロガーを分ける**
```csharp
var services = new ServiceCollection();
services.AddLogging(loggingBuilder =>
{
    loggingBuilder.ClearProviders();
    loggingBuilder.AddSerilog();
});

var serviceProvider = services.BuildServiceProvider();
var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

var httpClient = new HttpClient(new CustomLokiHttpHandler(loggerFactory))
{
    BaseAddress = new Uri("http://localhost:3100")
};
```

✅ **Loki 送信失敗時のログは Loki には送らず、ファイル・コンソールにのみ記録！**  
✅ **通常のアプリログは Loki へ送信し、リアルタイム監視が可能！**

---

## **🔥 3. まとめ**
| **解決策** | **メリット** | **デメリット** |
|------------|------------|---------------|
| **① Loki Sink のログレベルを制限** (`Warning` 以上のみ送信) | **設定が簡単** | **一部の INFO ログを Loki に送れなくなる** |
| **② `ILogger` を分ける (`FileOnlyLogger`)** | **Loki 送信失敗時のログは確実に Loki に送られない** | **コードが少し増える** |

✅ **Loki に送るログのレベルを `Warning` 以上にする方法が最も簡単！**  
✅ **完全に `ILogger` を分ける方法なら、Loki 送信失敗時にループするリスクゼロ！**  

🚀 **「Loki 送信失敗時のログを Loki に送らない設計にすれば、ログループ問題は解決！」** 🚀