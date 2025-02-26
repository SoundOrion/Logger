いい視点ですね！  
`Enrich.WithMachineName()` を使った場合、**JSON 形式のログには `MachineName` が含まれる** けれど、  
**テキスト（プレーン文字列）のログには表示されない** という現象が起きていますね。

これは **Serilog の `OutputTemplate` に `MachineName` を明示的に追加する必要がある** ためです。  
`Enrich.WithMachineName()` は構造化ログのプロパティにデータを追加するだけなので、  
**コンソールやファイルのプレーンテキストログにはデフォルトでは出力されません**。

---

## **✅ 解決策：`OutputTemplate` を設定する**
**解決策として、`WriteTo.Console()` や `WriteTo.File()` の `OutputTemplate` に `MachineName` を含めるように設定** しましょう。

### **📝 修正後の `Program.cs`**
```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Enrichers.Environment;

var builder = WebApplication.CreateBuilder(args);

// Serilog 設定（プレーンテキストにもホスト名を表示）
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithMachineName() // ★ 構造化ログにホスト名を追加
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] (Host: {MachineName}) {Message:lj}{NewLine}{Exception}"
    ) // ★ コンソールログにもホスト名を追加
    .WriteTo.File(
        "logs/log.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] (Host: {MachineName}) {Message:lj}{NewLine}{Exception}"
    ) // ★ ファイルログにもホスト名を追加
    .CreateLogger();

builder.Host.UseSerilog();

var app = builder.Build();

app.MapGet("/", () =>
{
    Log.Information("ユーザーがホームページにアクセスしました");
    return "ログ送信中...";
});

app.Run();
```

---

## **✅ どのように変わるのか？**
**📝 修正前（デフォルト）**
```
[2025-02-26 12:34:56 INF] ユーザーがホームページにアクセスしました
```
（`MachineName` は **JSON ログには含まれるが、テキストログには表示されない**）

---

**📝 修正後（OutputTemplate 設定済み）**
```
[2025-02-26 12:34:56 INF] (Host: MyServer01) ユーザーがホームページにアクセスしました
```
（`MachineName` が **プレーンテキストのログにも表示されるようになった！** 🚀）

---

## **✅ まとめ**
| **方法** | **結果** |
|----------|---------|
| **`.Enrich.WithMachineName()`（デフォルト）** | JSON 形式のログには `MachineName` が含まれるが、テキストには表示されない |
| **`.Enrich.WithMachineName()` ＋ `OutputTemplate` の設定（推奨）** | **JSONログにもテキストログにもホスト名が出る！** ✅ |

### **🎯 結論**
✅ **プレーンテキストのログにもホスト名を出したいなら `OutputTemplate` に `{MachineName}` を追加すればOK！** 🚀