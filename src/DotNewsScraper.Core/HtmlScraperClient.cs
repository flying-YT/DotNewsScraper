using System;
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
        private readonly HttpClient _client;

        public HtmlScraperClient()
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
            string html = await _client.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            return doc;
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}