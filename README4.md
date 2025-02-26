### **✅ Serilog の `OutputTemplate` で何を入れればよいか**
`OutputTemplate` を設定することで、**プレーンテキストのログにどの情報を表示するかを自由にカスタマイズ**できます。  
以下に、**よく使われるテンプレートの例とその意味**を紹介します。

---

## **1️⃣ 基本的な `OutputTemplate`**
```csharp
outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
```

### **📌 何が出力されるのか？**
```
[2025-02-26 12:34:56 INF] ユーザーがホームページにアクセスしました
```

| **プレースホルダー** | **意味** |
|-----------------|------------------------------|
| `{Timestamp:yyyy-MM-dd HH:mm:ss}` | ログのタイムスタンプ |
| `{Level:u3}` | ログレベル（INF, WRN, ERR など） |
| `{Message:lj}` | ログのメッセージ |
| `{NewLine}` | 次の行へ（改行） |
| `{Exception}` | エラーのスタックトレース（エラー発生時のみ） |

---

## **2️⃣ ホスト名（マシン名）を含める**
ホスト名を表示したい場合は **`{MachineName}` を追加** します。

```csharp
outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] (Host: {MachineName}) {Message:lj}{NewLine}{Exception}"
```

### **📌 出力されるログ**
```
[2025-02-26 12:34:56 INF] (Host: MyServer01) ユーザーがホームページにアクセスしました
```

---

## **3️⃣ HTTPリクエストの `TraceId` を含める**
ASP.NET Core の **`TraceId`**（リクエストごとの識別子）をログに含めると、  
**特定のリクエストに関連するログをまとめて分析できる** ようになります。

```csharp
app.Use(async (context, next) =>
{
    using (Serilog.Context.LogContext.PushProperty("TraceId", context.TraceIdentifier))
    {
        await next();
    }
});
```

### **📌 `OutputTemplate` に `TraceId` を追加**
```csharp
outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] (Host: {MachineName}) [TraceId: {TraceId}] {Message:lj}{NewLine}{Exception}"
```

### **📌 出力されるログ**
```
[2025-02-26 12:34:56 INF] (Host: MyServer01) [TraceId: 0HMBT6GB12345] ユーザーがホームページにアクセスしました
```

---

## **4️⃣ ユーザーID や カスタム情報を含める**
たとえば、**リクエストのユーザーIDをログに追加したい場合**：
```csharp
app.MapGet("/user/{userId}", (int userId) =>
{
    using (Serilog.Context.LogContext.PushProperty("UserId", userId))
    {
        Log.Information("ユーザーがプロフィールを閲覧しました");
    }
    return "プロフィールページ";
});
```

### **📌 `OutputTemplate` に `UserId` を追加**
```csharp
outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] (Host: {MachineName}) [UserId: {UserId}] {Message:lj}{NewLine}{Exception}"
```

### **📌 出力されるログ**
```
[2025-02-26 12:35:12 INF] (Host: MyServer01) [UserId: 42] ユーザーがプロフィールを閲覧しました
```

---

## **✅ まとめ**
| **項目** | **プレースホルダー** | **例** |
|----------|----------------|------------------------------|
| **タイムスタンプ** | `{Timestamp:yyyy-MM-dd HH:mm:ss}` | `2025-02-26 12:34:56` |
| **ログレベル** | `{Level:u3}` | `INF`（情報） |
| **メッセージ** | `{Message:lj}` | `ユーザーがアクセスしました` |
| **ホスト名（マシン名）** | `{MachineName}` | `MyServer01` |
| **リクエストID（TraceId）** | `{TraceId}` | `0HMBT6GB12345` |
| **ユーザーID** | `{UserId}` | `42` |
| **改行** | `{NewLine}` | （改行される） |
| **例外情報** | `{Exception}` | `System.Exception: エラー内容...` |

---

## **🎯 結論**
✅ **テキストログにホスト名を表示するには、`OutputTemplate` に `{MachineName}` を追加！**  
✅ **リクエストID (`TraceId`) や ユーザーID (`UserId`) もログに含められる！**  
✅ **プレーンテキストでも、構造化ログと同じ情報を持たせることが可能！** 🚀