# DotNewsScraper

Yahoo!ニュースやライブドアニュースから記事（タイトル、本文、リンク）をスクレイピングして取得する .NET ツール＆ライブラリです。
CLIアプリとして手軽に実行できるほか、クラスライブラリ（Core）としてご自身のプロジェクトに組み込むことも可能です。

## プロジェクト構成

- `DotNewsScraper.Core`: スクレイピングのコアロジックを担うクラスライブラリ
- `DotNewsScraper.Cli`: Coreライブラリを呼び出してCSVを出力するコンソールアプリ

---

## 1. CLIツールの使い方

コマンドラインから手軽にニュースサイトのトップ記事一覧を取得し、CSVファイルとして出力できます。

### 実行方法

ターミナルで `src/DotNewsScraper.Cli` ディレクトリに移動し、`dotnet run` を実行します。

```bash
cd src/DotNewsScraper.Cli

# デフォルト（Yahoo!ニュース、カレントディレクトリ）に出力する場合
dotnet run

# 対象サイトをライブドアニュースに変更する場合
dotnet run -- --livedoor

# 対象サイトをYahoo!ニュースにして、出力先のディレクトリを指定する場合
dotnet run -- --yahoo /path/to/output/directory
```

### 実行結果
処理が完了すると、指定したディレクトリ（指定がない場合はカレントディレクトリ）に以下のような名前のCSVファイルが生成されます。
- `yahoo_news_yyyyMMddHHmm.csv`
- `livedoor_news_yyyyMMddHHmm.csv`

---

## 2. ライブラリ（Core）の使い方

`DotNewsScraper.Core` をご自身のプロジェクトに追加することで、C#のコードからスクレイピング機能を利用できます。`INewsScraper`インターフェースを実装しており、サイトごとにクラスが分かれています。

### トップニュースを一括取得する

```csharp
using DotNewsScraper.Core;
using DotNewsScraper.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// インスタンスを作成（YahooNewsScraper または LivedoorNewsScraper）
INewsScraper scraper = new LivedoorNewsScraper();

// トップニュースを一括取得
// onProgress コールバックで進捗状況のメッセージを受け取れます
var myArticles = await scraper.ScrapeTopNewsAsync(
    onProgress: msg => Console.WriteLine(msg)
);

foreach (var article in myArticles)
{
    Console.WriteLine($"タイトル: {article.Title}");
    Console.WriteLine($"URL: {article.Url}");
}
```

### 指定した詳細ページのURLから単一の記事を取得する

```csharp
string targetUrl = "https://news.livedoor.com/article/detail/xxxxxxxxx/";

// URLを指定して個別取得
var singleArticle = await scraper.ScrapeArticleAsync(targetUrl, msg => Console.WriteLine(msg));

if (singleArticle != null)
{
    Console.WriteLine($"【取得成功】{singleArticle.Title}");
    Console.WriteLine(singleArticle.Body);
}
```

---

## ⚠️ 免責事項 (Disclaimer)

本ライブラリおよびツールをご利用になる前に、以下の事項を必ずご確認ください。

1. **利用規約の遵守**
   本ツールは特定の対象サイト（Yahoo!ニュース、ライブドアニュース等）のHTML構造に依存したスクレイピングを行います。対象サイトの利用規約（ToS）において、事前の許諾がない自動化されたデータ収集が禁止されている場合があります。本ツールはあくまで「教育目的（プログラミングの学習や技術検証）」で提供されるものであり、ご利用にあたっては対象サイトの利用規約に従い、**自己責任**でご利用ください。

2. **サーバー負荷への配慮（Rate Limit）**
   本ツールはサーバーへの過度な負荷を防ぐため、リクエスト間に1秒の待機時間（`Task.Delay`）を設けています。この設定を削除したり、短時間に大量のリクエストを送信するような改変（DDoS攻撃に繋がる行為）は絶対に行わないでください。

3. **動作保証の免責**
   対象サイトのHTML構造の変更や仕様変更により、予告なく本ツールが動作しなくなる（記事が取得できなくなる）可能性があります。あらかじめご了承ください。