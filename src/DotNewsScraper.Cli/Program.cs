using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using CsvHelper;
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

            // 引数のパース
            string siteMode = "yahoo";
            string outputDir = Directory.GetCurrentDirectory();

            foreach (var arg in args)
            {
                if (arg.ToLower() == "--yahoo")
                {
                    siteMode = "yahoo";
                }
                else if (arg.ToLower() == "--livedoor")
                {
                    siteMode = "livedoor";
                }
                else if (!arg.StartsWith("--"))
                {
                    outputDir = arg;
                }
            }

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

            // 実行日時をもとにファイル名を生成
            string fileName = $"{siteMode}_news_{DateTime.Now:yyyyMMddHHmm}.csv";
            string csvFilePath = Path.Combine(outputDir, fileName);

            // サイトに応じたインターフェースの実装クラスを選択
            INewsScraper scraper;
            if (siteMode == "livedoor")
            {
                scraper = new LivedoorNewsScraper();
                Console.WriteLine("対象サイト: ライブドアニュース\n");
            }
            else
            {
                scraper = new YahooNewsScraper();
                Console.WriteLine("対象サイト: Yahoo!ニュース\n");
            }
            
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
                // CsvHelperを使用して安全にCSVを出力
                using (var writer = new StreamWriter(csvFilePath, false, new UTF8Encoding(true)))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(articles);
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