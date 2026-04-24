# DotNewsScraper

A .NET tool and library for scraping articles (title, body, link) from Yahoo! News.
It can be easily executed as a CLI application, or integrated into your own projects as a class library (Core).

## Project Structure

- `DotNewsScraper.Core`: A class library containing the core scraping logic.
- `DotNewsScraper.Cli`: A console application that calls the Core library to output CSV files.

---

## 1. How to Use the CLI Tool

You can easily fetch a list of top articles from Yahoo! News from the command line and output them as a CSV file.

### Execution

Navigate to the `src/DotNewsScraper.Cli` directory in your terminal and run `dotnet run`.

```bash
cd src/DotNewsScraper.Cli

# Output to the default (current directory)
dotnet run

# Specify an output directory
dotnet run -- /path/to/output/directory
```

### Execution Results
Once the process is complete, a CSV file with a name like the following will be generated in the specified directory (or the current directory if not specified).
- `yahoo_news_yyyyMMddHHmm.csv` (e.g., `yahoo_news_202604231610.csv`)

---

## 2. How to Use the Library (Core)

By adding `DotNewsScraper.Core` to your own project (or solution), you can use the scraping functionality from your C# code.

### Bulk Fetch Top News
Calling `ScrapeTopNewsAsync` follows the links from the Yahoo! News top (Pickup) section to the detailed articles and fetches a list of articles.

```csharp
using DotNewsScraper.Core;
using DotNewsScraper.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Create an instance
var scraper = new YahooNewsScraper();

// Prepare a list (you can keep appending to this)
var myArticles = new List<NewsArticle>();

// Fetch top news in bulk
// You can receive progress messages via the onProgress callback
await scraper.ScrapeTopNewsAsync(
    onProgress: msg => Console.WriteLine(msg),
    appendTo: myArticles
);

foreach (var article in myArticles)
{
    Console.WriteLine($"Title: {article.Title}");
    Console.WriteLine($"URL: {article.Url}");
}
```

### Fetch a Single Article from a Specific Detail Page URL
By passing the URL of a specific Yahoo! News article to `ScrapeArticleAsync`, you can fetch only that specific article. This can be used to sequentially add fetched articles to an existing list.

```csharp
using DotNewsScraper.Core;
using DotNewsScraper.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

var scraper = new YahooNewsScraper();
var articleList = new List<NewsArticle>();

string targetUrl = "https://news.yahoo.co.jp/articles/xxxxxxxxx";

// Fetch an individual article by specifying its URL
var singleArticle = await scraper.ScrapeArticleAsync(targetUrl, msg => Console.WriteLine(msg));

if (singleArticle != null)
{
    // You can add it to an existing list each time the method is called
    articleList.Add(singleArticle);
    
    Console.WriteLine($"[Success] {singleArticle.Title}");
    Console.WriteLine(singleArticle.Body);
}
```

### Dependencies
This library uses HtmlAgilityPack for HTML parsing.