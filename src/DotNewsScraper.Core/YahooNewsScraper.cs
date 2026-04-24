using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using DotNewsScraper.Core.Models;

namespace DotNewsScraper.Core
{
    public class YahooNewsScraper
    {
        public YahooNewsScraper()
        {
            // エンコーディングプロバイダの登録（Shift-JISなどの対策）
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        /// <summary>
        /// Yahoo!ニュースのトップページから主要な記事を一括取得します。
        /// </summary>
        /// <param name="onProgress">進捗を通知するコールバック</param>
        /// <param name="appendTo">取得した記事を追加する既存のリスト（省略時は新規作成）</param>
        /// <returns>記事のリスト</returns>
        public async Task<List<NewsArticle>> ScrapeTopNewsAsync(Action<string> onProgress = null, List<NewsArticle> appendTo = null)
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
                    var pickupDoc = await scraper.GetHtmlDocumentAsync(pickupUrl);

                    var titleNode = pickupDoc.DocumentNode.SelectSingleNode("//title");
                    string title = titleNode?.InnerText.Replace(" - Yahoo!ニュース", "").Trim() ?? "タイトル不明";

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

        /// <summary>
        /// 指定した詳細ページのURLから記事を1件取得します。
        /// </summary>
        /// <param name="articleUrl">詳細記事のURL (例: https://news.yahoo.co.jp/articles/...)</param>
        /// <param name="onProgress">進捗を通知するコールバック</param>
        /// <returns>取得した記事データ。取得失敗時はnull</returns>
        public async Task<NewsArticle> ScrapeArticleAsync(string articleUrl, Action<string> onProgress = null)
        {
            using var scraper = new HtmlScraperClient();
            return await ScrapeArticleInternalAsync(articleUrl, null, onProgress, scraper);
        }

        // 内部用の個別記事取得メソッド（クライアントを使い回すため）
        private async Task<NewsArticle> ScrapeArticleInternalAsync(string articleUrl, string knownTitle, Action<string> onProgress, HtmlScraperClient scraper)
        {
            try
            {
                onProgress?.Invoke($"アクセス中 (詳細): {articleUrl}");
                var articleDoc = await scraper.GetHtmlDocumentAsync(articleUrl);

                string title = knownTitle;
                if (string.IsNullOrEmpty(title))
                {
                    var titleNode = articleDoc.DocumentNode.SelectSingleNode("//title");
                    title = titleNode?.InnerText.Replace(" - Yahoo!ニュース", "").Trim() ?? "タイトル不明";
                }

                var bodyContainer = articleDoc.DocumentNode.SelectSingleNode("//div[contains(@class, 'article_body')]");
                var pNodes = bodyContainer?.SelectNodes(".//p");
                
                string body = "";
                if (pNodes != null)
                {
                    body = string.Join(" ", pNodes.Select(n => n.InnerText.Trim())
                                                  .Where(t => t.Length > 0 
                                                           && !t.Contains("JavaScriptが無効")
                                                           && !t.Contains("JavaScriptの設定")));
                }

                body = body.Replace("\r", "").Replace("\n", " ").Replace("\"", "\"\"");
                title = title.Replace("\r", "").Replace("\n", " ").Replace("\"", "\"\"");

                return new NewsArticle { Title = title, Body = body, Url = articleUrl };
            }
            catch (Exception ex)
            {
                onProgress?.Invoke($"個別記事取得エラー: {ex.Message}");
                return null;
            }
        }
    }
}