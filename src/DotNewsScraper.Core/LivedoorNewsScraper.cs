using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using DotNewsScraper.Core.Models;

namespace DotNewsScraper.Core
{
    public class LivedoorNewsScraper : INewsScraper
    {
        public LivedoorNewsScraper()
        {
            // エンコーディングプロバイダの登録（EUC-JPなどの対策）
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public async Task<List<NewsArticle>> ScrapeTopNewsAsync(Action<string>? onProgress = null, List<NewsArticle>? appendTo = null)
        {
            var articles = appendTo ?? new List<NewsArticle>();
            string topUrl = "https://news.livedoor.com/";

            using var scraper = new HtmlScraperClient();

            onProgress?.Invoke("ライブドアニュースのトップページを取得中...");
            HtmlDocument topDoc;
            try {
                topDoc = await scraper.GetHtmlDocumentAsync(topUrl);
            } catch (Exception e) {
                onProgress?.Invoke("トップページの取得に失敗しました: " + e.Message);
                return articles;
            }

            // 主要ニュースのURLを取得 (/topics/detail/xxxx/)
            var topicNodes = topDoc.DocumentNode.SelectNodes("//a[contains(@href, '/topics/detail/')]");
            if (topicNodes == null)
            {
                onProgress?.Invoke("トップ記事のリンクが見つかりませんでした。");
                return articles;
            }

            var topicUrls = topicNodes.Select(n => n.GetAttributeValue("href", "")).Distinct().ToList();
            onProgress?.Invoke($"{topicUrls.Count} 件のトップ記事リンクを見つけました。");

            foreach (var topicUrl in topicUrls)
            {
                try
                {
                    onProgress?.Invoke($"\nアクセス中 (topic): {topicUrl}");
                    var topicDoc = await scraper.GetHtmlDocumentAsync(topicUrl);

                    // 「記事を読む」リンクを探す
                    var readArticleNode = topicDoc.DocumentNode.SelectSingleNode("//a[contains(text(), '記事を読む') or contains(@class, 'readArticle') or contains(@href, '/article/detail/')]");
                    string articleUrl = readArticleNode?.GetAttributeValue("href", "");

                    // もし見つからなかった場合はURL置換でフォールバック
                    if (string.IsNullOrEmpty(articleUrl))
                    {
                        articleUrl = topicUrl.Replace("/topics/detail/", "/article/detail/");
                    }

                    if (!articleUrl.StartsWith("http"))
                    {
                        articleUrl = "https://news.livedoor.com" + articleUrl;
                    }

                    // 個別取得処理を呼び出して結果をリストに追加
                    var article = await ScrapeArticleInternalAsync(articleUrl, null, onProgress, scraper);
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

                string title = knownTitle ?? "";
                if (string.IsNullOrEmpty(title))
                {
                    var titleNode = articleDoc.DocumentNode.SelectSingleNode("//title");
                    title = titleNode?.InnerText.Replace(" - ライブドアニュース", "").Trim() ?? "タイトル不明";
                }

                // ライブドアニュースの本文は .articleBody クラス内にある
                var bodyContainer = articleDoc.DocumentNode.SelectSingleNode("//div[contains(@class, 'articleBody')] | //span[@itemprop='articleBody']");
                var pNodes = bodyContainer?.SelectNodes(".//p | text()");
                
                string body = "";
                if (pNodes != null)
                {
                    body = string.Join(" ", pNodes.Select(n => n.InnerText.Trim())
                                                  .Where(t => t.Length > 0));
                }
                else if (bodyContainer != null)
                {
                    body = bodyContainer.InnerText.Trim();
                }

                // CsvHelperを利用するため、不要な改行だけを消去
                body = body.Replace("\r", "").Replace("\n", " ");
                title = title.Replace("\r", "").Replace("\n", " ");

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