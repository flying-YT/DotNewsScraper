using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DotNewsScraper.Core;

namespace DotNewsScraper.Cli
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("      DotNewsScraper CLI Tool");
            Console.WriteLine("========================================\n");

            // 1. 引数から出力先ディレクトリを取得（指定がない場合はカレントディレクトリ）
            string outputDir = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
            
            // 出力先ディレクトリが存在しない場合は作成する
            if (!Directory.Exists(outputDir))
            {
                try
                {
                    Directory.CreateDirectory(outputDir);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"出力先ディレクトリの作成に失敗しました: {ex.Message}");
                    return;
                }
            }

            // 2. 実行日時をもとにファイル名を生成
            string fileName = $"yahoo_news_{DateTime.Now:yyyyMMddHHmm}.csv";
            string csvFilePath = Path.Combine(outputDir, fileName);

            var scraper = new YahooNewsScraper();
            
            // Core側の処理進捗をConsoleに表示するためのコールバックを渡す
            var articles = await scraper.ScrapeTopNewsAsync(message => 
            {
                Console.WriteLine(message);
            });

            if (articles.Count == 0)
            {
                Console.WriteLine("\n取得できた記事はありませんでした。");
                return;
            }

            Console.WriteLine($"\n{articles.Count} 件の記事を取得しました。CSVを出力します...");

            try
            {
                using (StreamWriter sw = new StreamWriter(csvFilePath, false, new UTF8Encoding(true)))
                {
                    sw.WriteLine("\"Title\",\"Body\",\"Link\"");
                    foreach (var article in articles)
                    {
                        sw.WriteLine($"\"{article.Title}\",\"{article.Body}\",\"{article.Url}\"");
                    }
                }
                Console.WriteLine($"完了しました！出力先: {Path.GetFullPath(csvFilePath)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CSV出力エラー: {ex.Message}");
            }
        }
    }
}