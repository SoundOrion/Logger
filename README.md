### **📌 Serilog + Loki + 非同期ファイル出力 の設定 (README)**
このプロジェクトでは、**Serilog を使ってログを「非同期ファイル出力」と「Loki への送信」の両方を実現** します。  
この設定により、**リアルタイムログ監視（Loki）とローカルのログバックアップ（ファイル）を両立** できます。

---

## **🚀 機能**
✅ **コンソールログ出力**（標準出力にログを表示）  
✅ **非同期ファイル出力**（ログをローカルファイルに書き込み、I/O 負荷を最適化）  
✅ **Grafana Loki へログを直接送信**（リアルタイム監視と検索が可能）  
✅ **バッチ処理 & メモリ最適化**（Loki へ 50件ごとに送信、最大 1000件のログをキュー）  
✅ **JSON 形式のログフォーマット**（機械的に解析しやすい形式）

---

## **📌 1. インストール**
### **🔹 NuGet パッケージのインストール**
以下のコマンドで **必要な NuGet パッケージ** をインストールしてください。

```sh
dotnet add package Serilog
dotnet add package Serilog.Formatting.Json
dotnet add package Serilog.Sinks.Async
dotnet add package Serilog.Sinks.Grafana.Loki
```

---

## **📌 2. 使用方法**
### **🔹 Serilog の設定**
以下のコードを `Program.cs` に追加してください。

```csharp
using Serilog;
using Serilog.Formatting.Json;
using Serilog.Sinks.Async;
using Serilog.Sinks.Grafana.Loki;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        Log.Logger = new LoggerConfiguration()
            // 🔹 コンソールログ出力
            .WriteTo.Console()

            // 🔹 非同期ファイル出力（I/O 負荷を軽減しつつログをローカルに保存）
            .WriteTo.Async(a => a.File("logs/log.txt",
                rollingInterval: RollingInterval.Day, // 1日ごとに新しいログファイルを作成
                buffered: true, // バッファリングを有効化（メモリを活用して I/O 負荷を軽減）
                flushToDiskInterval: TimeSpan.FromSeconds(5), // 5秒ごとにログを書き込む
                retainedFileCountLimit: 7)) // 7日分のログを保持（古いログは自動削除）

            // 🔹 Grafana Loki にログを送信（リアルタイム監視 & ログ検索用）
            .WriteTo.GrafanaLoki("http://localhost:3100", // Loki のエンドポイント
                new List<LokiLabel> // Loki のラベル（ログ検索のためのメタデータ）
                {
                    new LokiLabel { Key = "app", Value = "my-app" },
                    new LokiLabel { Key = "env", Value = "production" }
                },
                batchPostingLimit: 50, // 50件ごとに Loki に送信
                queueLimit: 1000, // 最大 1000 件のログをキューに保持
                textFormatter: new JsonFormatter()) // JSON 形式でログをフォーマット

            .CreateLogger();

        // 🔹 ログを記録
        Log.Information("Loki にログを送信しつつ、ファイルにも記録中！");
        Log.Warning("これは警告ログです");
        Log.Error("エラーメッセージの送信");

        // 🔹 アプリ終了時にログを確実にフラッシュ
        Log.CloseAndFlush();
    }
}
```

---

## **📌 3. 設定の詳細**
| **設定** | **説明** |
|----------|---------|
| **`.WriteTo.Console()`** | ログを標準出力（コンソール）に表示 |
| **`.WriteTo.Async(a => a.File(...))`** | ログを **非同期でファイルに出力**（I/O 負荷を軽減） |
| **`rollingInterval: RollingInterval.Day`** | 1日ごとに新しいログファイルを作成（`logs/log_YYYYMMDD.txt`） |
| **`buffered: true`** | メモリにログをバッファリングし、まとめて書き込む |
| **`flushToDiskInterval: TimeSpan.FromSeconds(5)`** | 5秒ごとにバッファをディスクに書き込む |
| **`retainedFileCountLimit: 7`** | **ログの自動ローテーション（7日分保持）** |
| **`.WriteTo.GrafanaLoki()`** | Loki にリアルタイムでログを送信 |
| **`batchPostingLimit: 50`** | **50件ごとに Loki へバッチ送信（API 負荷を低減）** |
| **`queueLimit: 1000`** | **最大 1000件のログをメモリキューに保持（過負荷時のログロストを防ぐ）** |
| **`textFormatter: new JsonFormatter()`** | JSON 形式でログをフォーマット |

---

## **📌 4. Grafana Loki でログを確認**
### **🔹 Loki を起動**
```sh
docker run -d --name=loki -p 3100:3100 grafana/loki:latest
```
### **🔹 Grafana を起動**
```sh
docker run -d -p 3000:3000 --name=grafana grafana/grafana
```
### **🔹 Grafana の設定**
1. **データソースに Loki を追加**
   - Grafana の `Settings > Data Sources` で `http://localhost:3100` を追加
2. **`Explore`（探索）タブでクエリを実行**
   ```logql
   {app="my-app", env="production"}
   ```
3. **リアルタイムでログを監視！**

---

## **📌 5. メモリ使用量を抑えるためのチューニング**
**📝 `WriteTo.Async()` はメモリを使用するため、負荷を最適化するには以下の調整が可能**
```csharp
.WriteTo.Async(a => a.File("logs/log.txt",
    buffered: true, // メモリにログをバッファ
    flushToDiskInterval: TimeSpan.FromSeconds(2), // 2秒ごとにディスクに書き込む
    batchPostingLimit: 20)) // 1回のバッチで 20件ずつ書き込む
```
✅ **メモリ消費を抑えるなら `flushToDiskInterval` を短くする（デフォルトは5秒）**  
✅ **バッチサイズを小さくすると、メモリ内のログ保持量を減らせる！**

---

## **📌 6. まとめ**
✅ **リアルタイムログ監視（Loki 直送）とローカルログの保存（ファイル出力）を同時に実現！**  
✅ **非同期ファイル出力で I/O 負荷を抑えつつ、7日分のログを自動ローテーション！**  
✅ **Loki へのバッチ送信 & メモリキューを活用し、パフォーマンス最適化！**  
✅ **Grafana Loki を使って、アプリのログをリアルタイム監視 & 過去ログ分析が可能！**

🚀 **「この設定を使えば、パフォーマンス & 安定性の高いログ管理が簡単にできる！」**