#nullable disable
using System;
using System.IO;

namespace SpecWikiEditor
{
    // アプリの既定設定(保存先パス・フォルダ名・起動時の最大化)を、実行ファイルと同じフォルダの
    // "Default.cfg" に保存・読み込みするクラス。設定ファイルはシンプルな Key=Value 形式のテキストで、
    // "#" から始まる行はコメントとして無視する。
    public class AppConfig
    {
        // プロジェクトの保存先フォルダの親パス
        public string SavePath { get; set; }
        // プロジェクトフォルダの名前
        public string FolderName { get; set; }
        // 起動時にウィンドウを最大化するかどうか
        public bool MaximizeOnStartup { get; set; }

        private static string ConfigFilePath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Default.cfg");

        // 既定値（保存先＝実行ファイルと同じ階層、フォルダ名＝"SpecWiki"、起動時最大化＝True）を返す
        public static AppConfig CreateDefault()
        {
            return new AppConfig
            {
                SavePath = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/'),
                FolderName = "SpecWiki",
                MaximizeOnStartup = true,
            };
        }

        // 設定ファイルを読み込んで返す。存在しない場合は既定値で新規作成してから返す。
        // wasCreated には、今回新規作成した場合に true が渡される（既存データの移行判定に使う）。
        public static AppConfig LoadOrCreate(out bool wasCreated)
        {
            if (!File.Exists(ConfigFilePath))
            {
                AppConfig created = CreateDefault();
                created.Save();
                wasCreated = true;
                return created;
            }

            wasCreated = false;
            // 壊れた値・欠けている項目があっても既定値にフォールバックできるよう、
            // 既定値をベースにファイルの内容で上書きしていく
            AppConfig config = CreateDefault();
            try
            {
                foreach (string rawLine in File.ReadAllLines(ConfigFilePath))
                {
                    string line = rawLine.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

                    int separatorIndex = line.IndexOf('=');
                    if (separatorIndex < 0) continue;

                    string key = line.Substring(0, separatorIndex).Trim();
                    string value = line.Substring(separatorIndex + 1).Trim();

                    switch (key)
                    {
                        case "SavePath":
                            if (!string.IsNullOrWhiteSpace(value)) config.SavePath = value;
                            break;
                        case "FolderName":
                            if (!string.IsNullOrWhiteSpace(value)) config.FolderName = value;
                            break;
                        case "MaximizeOnStartup":
                            if (bool.TryParse(value, out bool maximize)) config.MaximizeOnStartup = maximize;
                            break;
                    }
                }
            }
            catch
            {
                // 読み込みに失敗した場合は、ここまでに得られた（＝既定値ベースの）内容をそのまま使う
            }
            return config;
        }

        // 現在の内容を Default.cfg に書き出す
        public void Save()
        {
            string content =
                "# SpecWikiEditor 設定ファイル\r\n" +
                "# SavePath : プロジェクトの保存先フォルダの親パス\r\n" +
                "# FolderName : プロジェクトフォルダの名前\r\n" +
                "# MaximizeOnStartup : 起動時にウィンドウを最大化するか (True / False)\r\n" +
                $"SavePath={SavePath}\r\n" +
                $"FolderName={FolderName}\r\n" +
                $"MaximizeOnStartup={MaximizeOnStartup}\r\n";
            File.WriteAllText(ConfigFilePath, content);
        }

        // 保存先パスとフォルダ名を組み合わせた、プロジェクトルートのフルパスを返す
        public string GetProjectRootPath() => Path.Combine(SavePath, FolderName);
    }
}
