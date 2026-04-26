using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNewsScraper.Core.Models;

namespace DotNewsScraper.Core
{
    /// <summary>
    /// ニュースサイトから記事を取得するためのインターフェース
    /// </summary>
    public interface INewsScraper
    {
        /// <summary>
        /// トップページから主要な記事を一括取得します。
        /// </summary>
        /// <param name="onProgress">進捗を通知するコールバック</param>
        /// <param name="appendTo">取得した記事を追加する既存のリスト（省略時は新規作成）</param>
        /// <returns>記事のリスト</returns>
        Task<List<NewsArticle>> ScrapeTopNewsAsync(Action<string>? onProgress = null, List<NewsArticle>? appendTo = null);

        /// <summary>
        /// 指定した詳細ページのURLから記事を1件取得します。
        /// </summary>
        /// <param name="articleUrl">詳細記事のURL</param>
        /// <param name="onProgress">進捗を通知するコールバック</param>
        /// <returns>取得した記事データ。取得失敗時はnull</returns>
        Task<NewsArticle?> ScrapeArticleAsync(string articleUrl, Action<string>? onProgress = null);
    }
}