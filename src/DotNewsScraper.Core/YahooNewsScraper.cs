using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using DotNewsScraper.Core.Models;

namespace DotNewsScraper.Core
{
    public class YahooNewsScraper : INewsScraper
    {
        // 複数ページにわたる記事で取得する最大ページ数（ページャーの異常による無限ループ防止）
        private const int MaxArticlePages = 10;

        public YahooNewsScraper()
        {
            // エンコーディングプロバイダの登録（Shift-JISなどの対策）
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public async Task<List<NewsArticle>> ScrapeTopNewsAsync(Action<string>? onProgress = null, List<NewsArticle>? appendTo = null)
        {
            var articles = appendTo ?? new List<NewsArticle>();
            string topUrl = "https://news.yahoo.co.jp/";

            using var scraper = new HtmlScraperClient();

            onProgress?.Invoke("Yahoo!ニュースのトップページを取得中...");
            HtmlDocument topDoc;
            try {
                topDoc = await scraper.GetHtmlDocumentAsync(topUrl);
            } catch (Exception e) {
                onProgress?.Invoke("トップページの取得に失敗しました: " + e.Message);
                return articles;
            }

            var pickupNodes = topDoc.DocumentNode.SelectNodes("//a[contains(@href, '/pickup/')]");
            if (pickupNodes == null)
            {
                onProgress?.Invoke("トップ記事のリンクが見つかりませんでした。");
                return articles;
            }

            var pickupUrls = pickupNodes.Select(n => n.GetAttributeValue("href", "")).Distinct().ToList();
            onProgress?.Invoke($"{pickupUrls.Count} 件のトップ記事（pickup）リンクを見つけました。");

            foreach (var pickupUrl in pickupUrls)
            {
                try
                {
                    onProgress?.Invoke($"\nアクセス中 (pickup): {pickupUrl}");
                    var (pickupDoc, finalUrl) = await scraper.GetHtmlDocumentWithFinalUrlAsync(pickupUrl);

                    var titleNode = pickupDoc.DocumentNode.SelectSingleNode("//title");
                    string title = CleanTitle(titleNode?.InnerText);

                    // pickupから記事ページ（例: /expert/articles/...）へ直接リダイレクトされた場合は、そのページから本文を取得する
                    if (finalUrl.Contains("/articles/"))
                    {
                        onProgress?.Invoke($"記事ページへリダイレクトされました: {finalUrl}");
                        articles.Add(await ParseArticleAsync(pickupDoc, finalUrl, title, onProgress, scraper));
                        await Task.Delay(1000);
                        continue;
                    }

                    var articleLinkNode = pickupDoc.DocumentNode.SelectSingleNode("//a[contains(@href, '/articles/')]");
                    string articleUrl = articleLinkNode?.GetAttributeValue("href", "");

                    if (string.IsNullOrEmpty(articleUrl))
                    {
                        onProgress?.Invoke("詳細記事へのリンクが見つかりませんでした。スキップします。");
                        continue;
                    }

                    if (!articleUrl.StartsWith("http"))
                    {
                        articleUrl = "https://news.yahoo.co.jp" + articleUrl;
                    }

                    // 個別取得処理を呼び出して結果をリストに追加
                    var article = await ScrapeArticleInternalAsync(articleUrl, title, onProgress, scraper);
                    if (article != null)
                    {
                        articles.Add(article);
                    }
                    
                    // サーバー負荷軽減の待機時間
                    await Task.Delay(1000); 
                }
                catch (Exception ex)
                {
                    onProgress?.Invoke($"記事処理エラー: {ex.Message}");
                }
            }

            return articles;
        }

        public async Task<NewsArticle?> ScrapeArticleAsync(string articleUrl, Action<string>? onProgress = null)
        {
            using var scraper = new HtmlScraperClient();
            return await ScrapeArticleInternalAsync(articleUrl, null, onProgress, scraper);
        }

        // 内部用の個別記事取得メソッド
        private async Task<NewsArticle?> ScrapeArticleInternalAsync(string articleUrl, string? knownTitle, Action<string>? onProgress, HtmlScraperClient scraper)
        {
            try
            {
                onProgress?.Invoke($"アクセス中 (詳細): {articleUrl}");
                var articleDoc = await scraper.GetHtmlDocumentAsync(articleUrl);
                return await ParseArticleAsync(articleDoc, articleUrl, knownTitle, onProgress, scraper);
            }
            catch (Exception ex)
            {
                onProgress?.Invoke($"個別記事取得エラー: {ex.Message}");
                return null;
            }
        }

        // 取得済みの記事ページHTMLからタイトルと本文を抽出する（複数ページの記事は次ページ以降の本文も連結する）
        private async Task<NewsArticle> ParseArticleAsync(HtmlDocument articleDoc, string articleUrl, string? knownTitle, Action<string>? onProgress, HtmlScraperClient scraper)
        {
            string title = knownTitle ?? "";
            if (string.IsNullOrEmpty(title))
            {
                var titleNode = articleDoc.DocumentNode.SelectSingleNode("//title");
                title = CleanTitle(titleNode?.InnerText);
            }

            var bodyParts = new List<string> { ExtractBody(articleDoc) };

            var pageDoc = articleDoc;
            for (int page = 2; page <= MaxArticlePages; page++)
            {
                string? nextUrl = FindNextPageUrl(pageDoc);
                if (nextUrl == null) break;

                // サーバー負荷軽減の待機時間
                await Task.Delay(1000);
                try
                {
                    onProgress?.Invoke($"アクセス中 (詳細 {page}ページ目): {nextUrl}");
                    pageDoc = await scraper.GetHtmlDocumentAsync(nextUrl);
                }
                catch (Exception ex)
                {
                    // 存在しないページへのリンクが残っている場合があるため、取得済みの本文で打ち切る
                    onProgress?.Invoke($"{page}ページ目の取得に失敗したため、{page - 1}ページ目までの本文を使用します: {ex.Message}");
                    break;
                }
                bodyParts.Add(ExtractBody(pageDoc));
            }

            string body = string.Join(" ", bodyParts.Where(b => b.Length > 0));

            // CsvHelperを利用するため、不要な改行だけを消去（ダブルクォートのサニタイズはCsvHelperにお任せ）
            body = body.Replace("\r", "").Replace("\n", " ");
            title = title.Replace("\r", "").Replace("\n", " ");

            return new NewsArticle { Title = title, Body = body, Url = articleUrl };
        }

        // ページャーの「次へ」リンクから次ページのURLを取得する（無ければnull）
        private static string? FindNextPageUrl(HtmlDocument doc)
        {
            var nextNode = doc.DocumentNode.SelectSingleNode("//a[contains(@data-cl-params, '_cl_vmodule:page;_cl_link:next;')]");
            string href = nextNode?.GetAttributeValue("href", "") ?? "";
            if (string.IsNullOrEmpty(href)) return null;

            return href.StartsWith("http") ? href : "https://news.yahoo.co.jp" + href;
        }

        // 1ページ分の記事本文を抽出する
        private static string ExtractBody(HtmlDocument articleDoc)
        {
            var bodyContainer = articleDoc.DocumentNode.SelectSingleNode("//div[contains(@class, 'article_body')]");
            var pNodes = bodyContainer?.SelectNodes(".//p");

            // エキスパート記事（/expert/articles/）は article_body が無く、段落に data-role="paragraph" が付く
            if (pNodes == null)
            {
                pNodes = articleDoc.DocumentNode.SelectNodes("//article//p[@data-role='paragraph']");
            }

            string body = "";
            if (pNodes != null)
            {
                body = string.Join(" ", pNodes.Select(n => n.InnerText.Trim())
                                              .Where(t => t.Length > 0
                                                       && !t.Contains("JavaScriptが無効")
                                                       && !t.Contains("JavaScriptの設定")));
            }

            return body;
        }

        // <title>からサイト名やエキスパート記事の付記（#エキスパートトピ（著者名）、 - エキスパート）を取り除く
        private static string CleanTitle(string? rawTitle)
        {
            if (string.IsNullOrWhiteSpace(rawTitle)) return "タイトル不明";

            string title = rawTitle.Replace(" - Yahoo!ニュース", "").Replace(" - エキスパート", "");
            title = Regex.Replace(title, @"\s*#エキスパートトピ（[^）]*）", "");
            return title.Trim();
        }
    }
}