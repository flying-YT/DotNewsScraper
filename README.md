# DotNewsScraper

Yahoo!ニュースから記事（タイトル、本文、リンク）をスクレイピングして取得する .NET ツール＆ライブラリです。
CLIアプリとして手軽に実行できるほか、クラスライブラリ（Core）としてご自身のプロジェクトに組み込むことも可能です。

## プロジェクト構成

- `DotNewsScraper.Core`: スクレイピングのコアロジックを担うクラスライブラリ
- `DotNewsScraper.Cli`: Coreライブラリを呼び出してCSVを出力するコンソールアプリ

---

## 1. CLIツールの使い方

コマンドラインから手軽にYahoo!ニュースのトップ記事一覧を取得し、CSVファイルとして出力できます。

### 実行方法

ターミナルで `src/DotNewsScraper.Cli` ディレクトリに移動し、`dotnet run` を実行します。

```bash
cd src/DotNewsScraper.Cli

# デフォルト（カレントディレクトリ）に出力する場合
dotnet run

# 出力先のディレクトリを指定する場合
dotnet run -- /path/to/output/directory
```

### 実行結果
処理が完了すると、指定したディレクトリ（指定がない場合はカレントディレクトリ）に以下のような名前のCSVファイルが生成されます。
- `yahoo_news_yyyyMMddHHmm.csv` (例: `yahoo_news_202604231610.csv`)

---

## 2. ライブラリ（Core）の使い方

`DotNewsScraper.Core` をご自身のプロジェクト（またはソリューション）に追加することで、C#のコードからスクレイピング機能を利用できます。

### トップニュースを一括取得する
`ScrapeTopNewsAsync` を呼び出すと、Yahoo!ニュースのトップ（Pickup）から詳細記事へのリンクを辿り、記事のリストを取得します。

```csharp
using DotNewsScraper.Core;
using DotNewsScraper.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// インスタンスを作成
var scraper = new YahooNewsScraper();

// リストを用意（※ここに追記していくことも可能）
var myArticles = new List<NewsArticle>();

// トップニュースを一括取得
// onProgress コールバックで進捗状況のメッセージを受け取れます
await scraper.ScrapeTopNewsAsync(
    onProgress: msg => Console.WriteLine(msg),
    appendTo: myArticles
);

foreach (var article in myArticles)
{
    Console.WriteLine($"タイトル: {article.Title}");
    Console.WriteLine($"URL: {article.Url}");
}
```

### 指定した詳細ページのURLから単一の記事を取得する
`ScrapeArticleAsync` にYahoo!ニュースの詳細記事のURLを渡すことで、特定の記事のみを取得できます。取得した記事を既存のリストへ逐次追加していくような使い方が可能です。

```csharp
using DotNewsScraper.Core;
using DotNewsScraper.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

var scraper = new YahooNewsScraper();
var articleList = new List<NewsArticle>();

string targetUrl = "https://news.yahoo.co.jp/articles/xxxxxxxxx";

// URLを指定して個別取得
var singleArticle = await scraper.ScrapeArticleAsync(targetUrl, msg => Console.WriteLine(msg));

if (singleArticle != null)
{
    // メソッドを呼ぶたびに既存のリストへ追加できる
    articleList.Add(singleArticle);
    
    Console.WriteLine($"【取得成功】{singleArticle.Title}");
    Console.WriteLine(singleArticle.Body);
}
```

### 依存関係
本ライブラリはHTMLのパースに [HtmlAgilityPack](https://html-agility-pack.net/) を使用しています。