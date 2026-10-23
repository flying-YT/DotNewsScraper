using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace DotNewsScraper.Core
{
    /// <summary>
    /// WebページからHTMLを取得し、HtmlDocumentに変換する共通のスクレイピングクライアント
    /// </summary>
    public class HtmlScraperClient : IDisposable
    {
        // ソケット枯渇（Socket Exhaustion）を防ぐため、HttpClientをstatic readonlyで共有する
        private static readonly HttpClient _client;

        static HtmlScraperClient()
        {
            _client = new HttpClient();
            // 一般的なブラウザを偽装するUser-Agentを設定
            _client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }

        /// <summary>
        /// 指定したURLからHTMLを取得し、HtmlDocumentオブジェクトとして返す
        /// </summary>
        public async Task<HtmlDocument> GetHtmlDocumentAsync(string url)
        {
            var (doc, _) = await GetHtmlDocumentWithFinalUrlAsync(url);
            return doc;
        }

        /// <summary>
        /// 指定したURLからHTMLを取得し、HtmlDocumentとリダイレクト後の最終URLを返す
        /// </summary>
        public async Task<(HtmlDocument Document, string FinalUrl)> GetHtmlDocumentWithFinalUrlAsync(string url)
        {
            using var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            // Euc-jpなどのエンコーディングにも自動対応させるため、Streamとして読み込む
            var bytes = await response.Content.ReadAsByteArrayAsync();
            var doc = new HtmlDocument();
            using (var stream = new MemoryStream(bytes))
            {
                doc.Load(stream, true); // true = 文字コード自動判別
            }

            // HttpClientはリダイレクトを自動追従するため、最終的なリクエストURLを取得する
            string finalUrl = response.RequestMessage?.RequestUri?.ToString() ?? url;
            return (doc, finalUrl);
        }

        public void Dispose()
        {
            // 共有しているHttpClientのDisposeは行わない
        }
    }
}
