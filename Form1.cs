#nullable disable
using System;
using System.IO;
using System.Windows.Forms;
using Markdig;

namespace SpecWikiEditor
{
    // メインフォームクラス。UIイベントとファイル操作、プレビュー更新を担当する。
    public partial class Form1 : Form
    {
        // プロジェクトルート（デスクトップ/WikiProject）へのパス
        private string currentProjectDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "WikiProject");
        // 画像などのアセットを保存するフォルダパス
        private string assetsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "WikiProject", "assets");
        // 現在編集中のMarkdownファイルのフルパス
        private string currentFilePath = "";

        public Form1()
        {
            try
            {
                // Windows Forms デザイナによるコンポーネント初期化
                InitializeComponent();
                // WebView を背面に移動（必要なら）
                if (webView2Preview != null) webView2Preview.SendToBack();

                // フォームのロード時イベントを登録
                this.Load += Form1_Load;

                // 主要なUI要素が存在するかチェック（見つからなければ致命的エラー）
                if (tabControlMain == null || lstSidebar == null || txtEditor == null)
                    throw new Exception("UI部品が見つかりません。");

                // タブ切替・サイドバー選択・エディタのイベントを登録
                tabControlMain.SelectedIndexChanged += TabControlMain_SelectedIndexChanged;
                lstSidebar.SelectedIndexChanged += LstSidebar_SelectedIndexChanged;

                // テキスト変更でプレビュー更新、ドラッグ操作で画像挿入をサポート
                txtEditor.TextChanged += TxtEditor_TextChanged;
                txtEditor.DragEnter += TxtEditor_DragEnter;
                txtEditor.DragDrop += TxtEditor_DragDrop;
            }
            catch (Exception ex)
            {
                // 初期化時に致命的なエラーが発生したらメッセージを表示して終了
                MessageBox.Show("初期化エラー: " + ex.Message);
                Environment.Exit(1);
            }
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                // プロジェクトフォルダとアセットフォルダの初期化
                InitializeProjectFolders();
                // フォルダ構成に基づきタブを読み込む
                LoadTabsFromFolders();

                // WebView2 のコア初期化を待機
                await webView2Preview.EnsureCoreWebView2Async(null);

                // 仮想ホスト名をアセットフォルダにマッピングすることで、HTML内から画像に参照可能にする
                webView2Preview.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "wiki-assets", assetsDir, Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);

                // 初回プレビュー更新
                UpdatePreview();
            }
            catch (Exception ex)
            {
                // 起動時のエラーはダイアログで通知
                MessageBox.Show("起動エラー: " + ex.Message);
            }
        }

        private void InitializeProjectFolders()
        {
            // プロジェクトルートとアセットフォルダがなければ作成
            if (!Directory.Exists(currentProjectDir)) Directory.CreateDirectory(currentProjectDir);
            if (!Directory.Exists(assetsDir)) Directory.CreateDirectory(assetsDir);

            // プロジェクトフォルダ内にタブ用フォルダが無ければ、初期フォルダとサンプルファイルを作成
            if (Directory.GetDirectories(currentProjectDir).Length == 0)
            {
                string tab1 = Path.Combine(currentProjectDir, "01_基本仕様");
                Directory.CreateDirectory(tab1);
                File.WriteAllText(Path.Combine(tab1, "1.概要.md"), "# 概要\r\nここから仕様を書き始めます。");
            }
        }

        private void LoadTabsFromFolders()
        {
            // タブをクリアしてからプロジェクトディレクトリ直下のフォルダをタブに追加
            tabControlMain.TabPages.Clear();
            string[] folders = Directory.GetDirectories(currentProjectDir);

            foreach (string folder in folders)
            {
                // assets フォルダはタブとして表示しない
                if (Path.GetFileName(folder).ToLower() == "assets") continue;
                TabPage page = new TabPage { Text = Path.GetFileName(folder), Tag = folder };
                tabControlMain.TabPages.Add(page);
            }
            // 最初のタブを選択してサイドバーを読み込む
            if (tabControlMain.TabPages.Count > 0) TabControlMain_SelectedIndexChanged(null, null);
        }

        private void TabControlMain_SelectedIndexChanged(object sender, EventArgs e)
        {
            // タブが切り替わったら現在の編集中ファイルを保存して、サイドバー（章リスト）を更新する
            if (tabControlMain.SelectedTab == null) return;
            SaveCurrentFile();

            lstSidebar.Items.Clear();
            string[] files = Directory.GetFiles(tabControlMain.SelectedTab.Tag.ToString(), "*.md");
            foreach (string file in files) lstSidebar.Items.Add(Path.GetFileNameWithoutExtension(file));
            if (lstSidebar.Items.Count > 0) lstSidebar.SelectedIndex = 0;
        }

        private void LstSidebar_SelectedIndexChanged(object sender, EventArgs e)
        {
            // サイドバーでファイルが選択されたら編集中のファイルを保存し、新しいファイルを読み込む
            if (lstSidebar.SelectedItem == null) return;
            SaveCurrentFile();
            currentFilePath = Path.Combine(tabControlMain.SelectedTab.Tag.ToString(), lstSidebar.SelectedItem.ToString() + ".md");
            if (File.Exists(currentFilePath)) txtEditor.Text = File.ReadAllText(currentFilePath);
        }

        private void SaveCurrentFile()
        {
            // currentFilePath が設定されていればテキストボックスの内容をファイルに書き込む
            if (!string.IsNullOrEmpty(currentFilePath)) File.WriteAllText(currentFilePath, txtEditor.Text);
        }

        // テキストが変更されたらプレビューを更新する（遅延なし）
        private void TxtEditor_TextChanged(object sender, EventArgs e) => UpdatePreview();

        private void UpdatePreview()
        {
            // WebView2 のコアが初期化されていなければ何もしない
            if (webView2Preview.CoreWebView2 == null) return;

            // エディタのMarkdownテキストをHTMLに変換し、アセットフォルダのファイルパスを仮想ホストのURLに置換して表示
            string markdownText = txtEditor.Text;
            // assetsDir のパス区切りをURL向けに変換してから仮想ホスト名に差し替える
            string htmlBody = Markdown.ToHtml(markdownText).Replace(assetsDir.Replace("\\", "/"), "https://wiki-assets");

            string fullHtml = $@"<html><body>{htmlBody}</body></html>";
            webView2Preview.CoreWebView2.NavigateToString(fullHtml);
        }

        private void TxtEditor_DragEnter(object sender, DragEventArgs e)
        {
            // ファイルのドラッグが来た場合はコピー操作を受け入れる
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        private void TxtEditor_DragDrop(object sender, DragEventArgs e)
        {
            // ドロップされたファイルをアセットフォルダにコピーし、Markdown 画像リンクを挿入する
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                // ファイル名をGUIDで一意化して衝突を避ける
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(files[0]);
                string destPath = Path.Combine(assetsDir, fileName);
                File.Copy(files[0], destPath, true);

                // 仮想ホスト名を使ったURLでMarkdownに挿入（例: https://wiki-assets/xxxxx.png）
                string tag = $"\r\n![画像](https://wiki-assets/{fileName})\r\n";
                txtEditor.Text = txtEditor.Text.Insert(txtEditor.SelectionStart, tag);
                // 挿入後にファイル保存
                SaveCurrentFile();
            }
        }
    }
}