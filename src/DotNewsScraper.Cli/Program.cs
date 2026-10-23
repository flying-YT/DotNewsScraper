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
        static async Task<int> Main(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("      DotNewsScraper CLI Tool");
            Console.WriteLine("========================================\n");

            // 引数のパース
            string siteMode = "yahoo";
            string outputDir = Directory.GetCurrentDirectory();
            string? logDir = null;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg.ToLower() == "--yahoo")
                {
                    siteMode = "yahoo";
                }
                else if (arg.ToLower() == "--livedoor")
                {
                    siteMode = "livedoor";
                }
                else if (arg.ToLower() == "--log-dir")
                {
                    if (i + 1 >= args.Length)
                    {
                        Console.WriteLine("--log-dir の後にログの出力先ディレクトリを指定してください。");
                        return 1;
                    }
                    logDir = args[++i];
                }
                else if (!arg.StartsWith("--"))
                {
                    outputDir = arg;
                }
            }

            // ログファイルは日ごと・サイトごとに1ファイルへ追記する
            string? logFilePath = null;
            if (logDir != null)
            {
                try
                {
                    Directory.CreateDirectory(logDir);
                    logFilePath = Path.Combine(logDir, $"{siteMode}_{DateTime.Now:yyyyMMdd}.log");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ログ出力先ディレクトリの作成に失敗しました: {ex.Message}");
                    return 1;
                }
            }
            var logger = new RunLogger(logFilePath);

            logger.Log("===== 処理開始 =====");
            try
            {
                return await RunAsync(siteMode, outputDir, logger);
            }
            catch (Exception ex)
            {
                // 想定外の例外もログに残してから終了する
                logger.Log($"予期しないエラーが発生しました: {ex}");
                return 1;
            }
            finally
            {
                logger.Log("===== 処理終了 =====");
            }
        }

        static async Task<int> RunAsync(string siteMode, string outputDir, RunLogger logger)
        {
            if (!Directory.Exists(outputDir))
            {
                try
                {
                    Directory.CreateDirectory(outputDir);
                }
                catch (Exception ex)
                {
                    logger.Log($"出力先ディレクトリの作成に失敗しました: {ex.Message}");
                    return 1;
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
                logger.Log("対象サイト: ライブドアニュース\n");
            }
            else
            {
                scraper = new YahooNewsScraper();
                logger.Log("対象サイト: Yahoo!ニュース\n");
            }

            var articles = await scraper.ScrapeTopNewsAsync(logger.Log);

            if (articles.Count == 0)
            {
                logger.Log("\n取得できた記事はありませんでした。");
                return 1;
            }

            logger.Log($"\n{articles.Count} 件の記事を取得しました。CSVを出力します...");

            try
            {
                // CsvHelperを使用して安全にCSVを出力
                using (var writer = new StreamWriter(csvFilePath, false, new UTF8Encoding(true)))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(articles);
                }
                logger.Log($"完了しました！出力先: {Path.GetFullPath(csvFilePath)}");
                return 0;
            }
            catch (Exception ex)
            {
                logger.Log($"CSV出力エラー: {ex.Message}");
                return 1;
            }
        }
    }
}
