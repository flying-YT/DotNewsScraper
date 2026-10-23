using System;
using System.IO;
using System.Text;

namespace DotNewsScraper.Cli
{
    /// <summary>
    /// 進捗メッセージをコンソールに表示し、ログファイルが指定されていれば日時付きで追記するロガー
    /// </summary>
    public class RunLogger
    {
        private readonly string? _logFilePath;

        public RunLogger(string? logFilePath)
        {
            _logFilePath = logFilePath;
        }

        public void Log(string message)
        {
            Console.WriteLine(message);

            if (_logFilePath == null) return;

            try
            {
                // 空行（見た目の区切り）はログに残さず、各行に日時を付けて追記する
                var sb = new StringBuilder();
                foreach (var line in message.Split('\n'))
                {
                    if (line.Trim().Length == 0) continue;
                    sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {line.TrimEnd('\r')}");
                }
                File.AppendAllText(_logFilePath, sb.ToString(), new UTF8Encoding(false));
            }
            catch (Exception ex)
            {
                // ログ出力の失敗で処理自体は止めない
                Console.WriteLine($"ログファイルへの書き込みに失敗しました: {ex.Message}");
            }
        }
    }
}
