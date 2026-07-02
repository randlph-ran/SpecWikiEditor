#nullable disable
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
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

        // 「作業内容のセーブ」以降に変更が加えられたかどうか。終了時の確認ダイアログの判定に使う。
        private bool hasUnsavedChanges = false;

        // プレビュー・出力HTMLの両方で使う共通CSS。
        // ・画像がプレビュー/出力先の表示幅に収まるよう自動縮小させる（元の画像ファイルには一切手を加えない）
        // ・背景色/文字色を明示指定し、閲覧環境のダークモード設定による自動反転（黒背景に黒文字等）を防ぐ
        private const string CommonPreviewCss =
            "html, body { background-color: #ffffff; color: #000000; } img { max-width: 100%; height: auto; }";

        // タブに描画する「×」（閉じる）ボタンの一辺のサイズ(px)
        private const int TabCloseButtonSize = 16;

        // Markdown変換用のパイプライン。
        // ・UseSoftlineBreakAsHardlineBreak() : エディタ内の単一の改行(Enter1回)をそのまま<br>として扱う
        // ・UseAdvancedExtensions()           : 表・チェックリスト・取り消し線など、標準Markdownには無い
        //                                        拡張記法をまとめて有効化する（ツールバーの各機能に対応するため）
        private static readonly MarkdownPipeline markdownPipeline =
            new MarkdownPipelineBuilder().UseAdvancedExtensions().UseSoftlineBreakAsHardlineBreak().Build();

        // 文字サイズ変更ドロップダウンの選択肢と、対応するフォントサイズ(px)
        private static readonly (string Label, int Px)[] FontSizeOptions =
        {
            ("小", 12),
            ("中", 16),
            ("大", 24),
            ("特大", 32),
        };

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
                // 終了時、未保存の変更があれば確認する
                this.FormClosing += Form1_FormClosing;

                // 主要なUI要素が存在するかチェック（見つからなければ致命的エラー）
                if (tabControlMain == null || lstSidebar == null || txtEditor == null || btnAddTab == null || btnExport == null
                    || btnAddFile == null || btnRemoveFile == null || cmbFontSize == null)
                    throw new Exception("UI部品が見つかりません。");

                // タブ切替・サイドバー選択・エディタのイベントを登録
                tabControlMain.SelectedIndexChanged += TabControlMain_SelectedIndexChanged;
                lstSidebar.SelectedIndexChanged += LstSidebar_SelectedIndexChanged;

                // テキスト変更でプレビュー更新、ドラッグ操作で画像挿入をサポート
                txtEditor.TextChanged += TxtEditor_TextChanged;
                txtEditor.DragEnter += TxtEditor_DragEnter;
                txtEditor.DragDrop += TxtEditor_DragDrop;

                // 「+」ボタンで新規タブ追加、「出力」ボタンでHTML出力を行う
                btnAddTab.Click += BtnAddTab_Click;
                btnExport.Click += BtnExport_Click;

                // タブに「×」を自前描画し、クリックされたらタブを閉じる（削除）処理を行う
                tabControlMain.DrawItem += TabControlMain_DrawItem;
                tabControlMain.MouseDown += TabControlMain_MouseDown;

                // 編集ツールバー(pnlEditorToolbar)は、ボタンの折り返し行数がウィンドウ幅によって
                // 変わる。折り返し行数が変わる＝パネル自身の内部レイアウトが変わる瞬間である
                // Layoutイベントを捉えて、そのたびに高さを実際の行数に合わせて再計算する。
                // （Resize/SplitterMovedだけでは反映タイミングが漏れることがあったため、
                // より直接的なLayoutイベントで確実に捕捉する）
                pnlEditorToolbar.Layout += (s, e) => AdjustEditorToolbarHeight();
                this.Resize += (s, e) => AdjustEditorToolbarHeight();
                splitContainer1.SplitterMoved += (s, e) => AdjustEditorToolbarHeight();
                splitContainer2.SplitterMoved += (s, e) => AdjustEditorToolbarHeight();

                // 編集ツールバーの各ボタンにMarkdown/HTML挿入処理を割り当てる
                btnHeading1.Click += (s, e) => InsertPrefixOnSelectedLines(_ => "# ");
                btnHeading2.Click += (s, e) => InsertPrefixOnSelectedLines(_ => "## ");
                btnBulletList.Click += (s, e) => InsertPrefixOnSelectedLines(_ => "- ");
                btnNumberedList.Click += (s, e) => InsertPrefixOnSelectedLines(i => $"{i + 1}. ");
                btnCheckList.Click += (s, e) => InsertPrefixOnSelectedLines(_ => "- [ ] ");
                btnQuote.Click += (s, e) => InsertPrefixOnSelectedLines(_ => "> ");
                btnHr.Click += (s, e) => InsertAtCursor("\r\n\r\n---\r\n\r\n");
                btnBold.Click += (s, e) => WrapSelection("**", "**", "太字");
                btnUnderline.Click += (s, e) => WrapSelection("<u>", "</u>", "下線");
                btnStrikethrough.Click += (s, e) => WrapSelection("~~", "~~", "取消線");
                btnInlineCode.Click += (s, e) => WrapSelection("`", "`", "コード");
                btnCodeBlock.Click += (s, e) => WrapSelection("```\r\n", "\r\n```", "コード");
                btnLink.Click += (s, e) => InsertLink();
                btnTable.Click += (s, e) => InsertAtCursor(
                    "\r\n\r\n| 見出し1 | 見出し2 |\r\n| --- | --- |\r\n| セル1 | セル2 |\r\n\r\n");
                btnTextColor.Click += (s, e) => InsertTextColor();
                btnFindReplace.Click += (s, e) => new FindReplaceDialog(txtEditor).Show(this);

                // 文字サイズドロップダウンの選択肢を用意し、選択されたら選択範囲をそのサイズで囲む
                foreach (var option in FontSizeOptions) cmbFontSize.Items.Add(option.Label);
                cmbFontSize.SelectedIndexChanged += CmbFontSize_SelectedIndexChanged;

                // サイドバーの「+」で段落追加、「-」で選択中の段落を削除する
                btnAddFile.Click += BtnAddFile_Click;
                btnRemoveFile.Click += BtnRemoveFile_Click;

                // 「ファイル」メニューの各項目にイベントを割り当てる
                menuSaveWork.Click += (s, e) => SaveWorkToSpc();
                menuLoadWork.Click += MenuLoadWork_Click;
                menuLoadMdFile.Click += MenuLoadMdFile_Click;
                menuExportHtml.Click += (s, e) => ExportCurrentFileToHtml();
                menuExit.Click += (s, e) => this.Close();
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

                // フォーム表示直後の実際の幅に合わせて、編集ツールバーの高さを計算しておく
                AdjustEditorToolbarHeight();


                // WebView2 のコア初期化を待機
                await webView2Preview.EnsureCoreWebView2Async(null);

                // 仮想ホスト名をアセットフォルダにマッピングすることで、HTML内から画像に参照可能にする
                webView2Preview.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "wiki-assets", assetsDir, Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);

                // WindowsのダークモードにWebView2が追従して背景を黒反転してしまい、
                // 文字色との組み合わせで読めなくなる問題を防ぐため、常にライト表示に固定する
                webView2Preview.CoreWebView2.Profile.PreferredColorScheme =
                    Microsoft.Web.WebView2.Core.CoreWebView2PreferredColorScheme.Light;

                // 初回プレビュー更新
                UpdatePreview();
            }
            catch (Exception ex)
            {
                // 起動時のエラーはダイアログで通知
                MessageBox.Show("起動エラー: " + ex.Message);
            }
        }

        // 編集ツールバー(pnlEditorToolbar)の高さを、実際に配置されたボタンの下端座標に合わせて再計算する。
        // FlowLayoutPanelは既定では収まりきらない子コントロールをクリッピングせず、
        // パネルの外側（＝下にあるtxtEditorの領域）にそのままはみ出して描画してしまうため、
        // 高さを常に正しい値に保つことが、エディタ本体（txtEditor）のTop位置がずれない
        // ようにする唯一の対策となる。
        // ※GetPreferredSize()による見積もりでは実際の描画とわずかにズレることがあったため、
        //   ボタン1つ1つの実際のBottom座標を直接調べて、その最大値をそのまま高さとして採用する。
        private void AdjustEditorToolbarHeight()
        {
            if (pnlEditorToolbar.Width <= 0 || pnlEditorToolbar.Controls.Count == 0) return;

            int maxBottom = 0;
            foreach (Control control in pnlEditorToolbar.Controls)
            {
                int bottom = control.Bottom + control.Margin.Bottom;
                if (bottom > maxBottom) maxBottom = bottom;
            }
            int neededHeight = maxBottom + pnlEditorToolbar.Padding.Bottom;

            if (pnlEditorToolbar.Height == neededHeight) return;

            pnlEditorToolbar.Height = neededHeight;
            // Dockの再計算を即座に反映させ、隣接するtxtEditorの開始位置がずれないようにする
            splitContainer2.Panel2.PerformLayout();

            // txtEditorはDock=Fillのためこのタイミングでサイズが変わるが、Windows標準の
            // マルチラインテキストボックスは「内部スクロール位置(先頭表示行)」をリサイズ後に
            // 自動でリセットしないため、そのままだと本来見えるはずの先頭行が隠れたままになる。
            // 一度カーソルを先頭(位置0)に移動してスクロール位置を確実にリセットしてから、
            // 元のカーソル位置に戻すことで、この表示ズレを解消する。
            int savedSelectionStart = txtEditor.SelectionStart;
            int savedSelectionLength = txtEditor.SelectionLength;
            txtEditor.SelectionStart = 0;
            txtEditor.ScrollToCaret();
            txtEditor.SelectionStart = savedSelectionStart;
            txtEditor.SelectionLength = savedSelectionLength;
            txtEditor.ScrollToCaret();
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
                // 名称変更用のUI（段落名称／タブ名称のテキストボックス＋ボタン）をタブページ内に配置する
                SetupTabPageRenameControls(page);
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

            // タブ切替に伴い、タブページ内のリネーム欄の表示も最新の状態に更新する
            RefreshRenameBoxes();

            // TabControlの標準仕様上、タブをクリックで切り替えるとタブページ内の最初のコントロール
            // （リネーム欄）へ自動的にフォーカスが移ってしまうため、明示的にエディタへフォーカスを戻す
            txtEditor.Focus();
        }

        private void LstSidebar_SelectedIndexChanged(object sender, EventArgs e)
        {
            // サイドバーでファイルが選択されたら編集中のファイルを保存し、新しいファイルを読み込む
            if (lstSidebar.SelectedItem == null) return;
            SaveCurrentFile();
            currentFilePath = Path.Combine(tabControlMain.SelectedTab.Tag.ToString(), lstSidebar.SelectedItem.ToString() + ".md");
            if (File.Exists(currentFilePath)) txtEditor.Text = File.ReadAllText(currentFilePath);

            // 段落の選択に伴い、リネーム欄の「段落名称」表示も最新の状態に更新する
            RefreshRenameBoxes();

            // 段落切替時も、確実にエディタへフォーカスを戻しておく
            txtEditor.Focus();
        }

        private void SaveCurrentFile()
        {
            // currentFilePath が設定されていればテキストボックスの内容をファイルに書き込む
            if (!string.IsNullOrEmpty(currentFilePath)) File.WriteAllText(currentFilePath, txtEditor.Text);
        }

        // タブ名・段落名・ファイル名として入力された文字列から、前後の空白とファイル名に使えない文字を取り除く。
        // タブ追加・段落追加・リネームなど、名前を受け取る処理全体で共通利用する。
        private string SanitizeName(string rawName)
        {
            string name = (rawName ?? "").Trim();
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
                name = name.Replace(invalidChar.ToString(), "");
            return name;
        }

        // テキストが変更されたらプレビューを更新する（遅延なし）。あわせて未保存フラグを立てる。
        private void TxtEditor_TextChanged(object sender, EventArgs e)
        {
            hasUnsavedChanges = true;
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            // WebView2 のコアが初期化されていなければ何もしない
            if (webView2Preview.CoreWebView2 == null) return;

            // プレビューでは画像を仮想ホスト名（wiki-assets）経由で参照させる
            string fullHtml = BuildHtmlDocument(txtEditor.Text, "https://wiki-assets");
            webView2Preview.CoreWebView2.NavigateToString(fullHtml);
        }

        // Markdown文字列を、プレビュー表示と出力(出力ボタン)の両方で共有するHTMLドキュメントに変換する。
        // imageBaseUrl : 画像参照の解決先。プレビュー時は仮想ホストURL("https://wiki-assets")、
        //                出力時は出力先フォルダからの相対パス("assets")などを渡す。
        // 今後CSSやHTMLテンプレートに手を加えたくなった場合も、この1箇所を変更するだけで
        // プレビュー・出力の両方に反映される（拡張性を考慮した共通化）。
        private string BuildHtmlDocument(string markdownText, string imageBaseUrl)
        {
            string htmlBody = Markdown.ToHtml(markdownText, markdownPipeline);
            // assetsDir のパス区切りをURL向けに変換してから、いったん仮想ホスト表記に統一する
            // （ローカルの絶対パスで画像が参照されているケースへの後方互換）
            htmlBody = htmlBody.Replace(assetsDir.Replace("\\", "/"), "https://wiki-assets");
            // 呼び出し元が指定した参照先（プレビュー用/出力用）に置き換える
            if (imageBaseUrl != "https://wiki-assets")
                htmlBody = htmlBody.Replace("https://wiki-assets", imageBaseUrl);

            // 画像がウィンドウ幅に収まるよう自動縮小するCSSを共通で適用する
            return $@"<html><head><meta charset=""utf-8""><style>{CommonPreviewCss}</style></head><body>{htmlBody}</body></html>";
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

        // 現在の選択範囲を含む行（複数行選択時はすべての行）の先頭に、prefixGenerator が返す文字列を挿入する。
        // 見出し・箇条書き・番号付きリスト・チェックリスト・引用など、行頭に記号を付ける系のボタンで共通利用する。
        // prefixGenerator の引数には「選択範囲内での行番号(0始まり)」が渡され、番号付きリストの連番などに使う。
        private void InsertPrefixOnSelectedLines(Func<int, string> prefixGenerator)
        {
            string text = txtEditor.Text;
            int selStart = txtEditor.SelectionStart;
            int selEnd = selStart + txtEditor.SelectionLength;

            // 選択範囲を含む行の開始位置・終了位置を求める
            int lineStart = text.LastIndexOf('\n', Math.Max(selStart - 1, 0)) + 1;
            int lineEnd = text.IndexOf('\n', selEnd);
            if (lineEnd == -1) lineEnd = text.Length;

            string[] lines = text.Substring(lineStart, lineEnd - lineStart).Split('\n');
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                sb.Append(prefixGenerator(i));
                sb.Append(lines[i].TrimEnd('\r'));
                if (i < lines.Length - 1) sb.Append('\n');
            }

            string replaced = sb.ToString();
            txtEditor.Text = text.Substring(0, lineStart) + replaced + text.Substring(lineEnd);

            // カーソルを変更後の範囲の末尾に移動する
            txtEditor.SelectionStart = lineStart + replaced.Length;
            txtEditor.SelectionLength = 0;
            txtEditor.Focus();
        }

        // 選択範囲を prefix と suffix で囲む。未選択の場合は placeholder を挿入し、その部分を選択状態にする
        // （続けて文字を入力すればそのまま置き換えられるようにするため）。
        // 太字・下線・取消線・インラインコード・コードブロック・文字色・文字サイズの各ボタンで共通利用する。
        private void WrapSelection(string prefix, string suffix, string placeholder)
        {
            int start = txtEditor.SelectionStart;
            string selected = txtEditor.SelectionLength > 0 ? txtEditor.SelectedText : placeholder;

            txtEditor.SelectedText = prefix + selected + suffix;

            txtEditor.SelectionStart = start + prefix.Length;
            txtEditor.SelectionLength = selected.Length;
            txtEditor.Focus();
        }

        // カーソル位置（選択中であれば選択範囲を置き換えて）に文字列を挿入する。水平線・テーブルの雛形挿入で使用する。
        private void InsertAtCursor(string text)
        {
            int insertPos = txtEditor.SelectionStart;
            txtEditor.SelectedText = text;
            txtEditor.SelectionStart = insertPos + text.Length;
            txtEditor.SelectionLength = 0;
            txtEditor.Focus();
        }

        // 「リンク」ボタン押下時：表示文字とURLを入力してもらい、Markdownのリンク記法を挿入する
        private void InsertLink()
        {
            string defaultDisplayText = txtEditor.SelectionLength > 0 ? txtEditor.SelectedText : "リンク";
            using (var dialog = new LinkInputDialog(defaultDisplayText))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                if (string.IsNullOrWhiteSpace(dialog.Url)) return;

                string displayText = string.IsNullOrWhiteSpace(dialog.DisplayText) ? dialog.Url : dialog.DisplayText;
                InsertAtCursor($"[{displayText}]({dialog.Url})");
            }
        }

        // 「文字色」ボタン押下時：カラーピッカーで色を選び、選択範囲をHTMLのspanタグ(color指定)で囲む
        private void InsertTextColor()
        {
            using (var dialog = new ColorDialog())
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                string colorHex = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
                WrapSelection($"<span style=\"color:{colorHex}\">", "</span>", "文字色");
            }
        }

        // 文字サイズドロップダウンで選択された際：選択範囲をHTMLのspanタグ(font-size指定)で囲む
        private void CmbFontSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbFontSize.SelectedIndex < 0) return;
            int px = FontSizeOptions[cmbFontSize.SelectedIndex].Px;
            WrapSelection($"<span style=\"font-size:{px}px\">", "</span>", "文字サイズ");

            // 同じ選択肢を続けて選んでも再度反映できるよう、選択状態をリセットしておく
            cmbFontSize.SelectedIndex = -1;
        }

        // タブ矩形から、右端に配置する「×」ボタンの矩形を計算する（描画・クリック判定の両方で使う共通ロジック）
        private Rectangle GetTabCloseButtonRect(Rectangle tabRect)
        {
            return new Rectangle(
                tabRect.Right - TabCloseButtonSize - 6,
                tabRect.Top + (tabRect.Height - TabCloseButtonSize) / 2,
                TabCloseButtonSize,
                TabCloseButtonSize);
        }

        // タブヘッダーのオーナードロー処理：タブ名と「×」ボタンを自前で描画する
        private void TabControlMain_DrawItem(object sender, DrawItemEventArgs e)
        {
            TabPage page = tabControlMain.TabPages[e.Index];
            Rectangle tabRect = tabControlMain.GetTabRect(e.Index);

            // 選択中のタブは白、非選択のタブはやや暗いグレーで塗り分ける
            bool isSelected = e.Index == tabControlMain.SelectedIndex;
            using (SolidBrush backBrush = new SolidBrush(isSelected ? SystemColors.Window : SystemColors.Control))
                e.Graphics.FillRectangle(backBrush, tabRect);

            // 「×」ボタン分のスペースを空けてタブ名を描画する
            Rectangle closeRect = GetTabCloseButtonRect(tabRect);
            Rectangle textRect = new Rectangle(
                tabRect.Left + 6, tabRect.Top,
                tabRect.Width - TabCloseButtonSize - 12, tabRect.Height);
            TextRenderer.DrawText(e.Graphics, page.Text, tabControlMain.Font, textRect, SystemColors.ControlText,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            // 「×」ボタンを描画する
            TextRenderer.DrawText(e.Graphics, "×", tabControlMain.Font, closeRect, Color.DimGray,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        // タブヘッダー上でのマウスクリックを監視し、「×」ボタン領域がクリックされたらタブを閉じる
        private void TabControlMain_MouseDown(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < tabControlMain.TabPages.Count; i++)
            {
                Rectangle tabRect = tabControlMain.GetTabRect(i);
                Rectangle closeRect = GetTabCloseButtonRect(tabRect);
                if (closeRect.Contains(e.Location))
                {
                    CloseTab(i);
                    return;
                }
            }
        }

        // 指定インデックスのタブを、確認ダイアログを経てから削除する
        private void CloseTab(int index)
        {
            if (index < 0 || index >= tabControlMain.TabPages.Count) return;

            TabPage page = tabControlMain.TabPages[index];
            string folderPath = page.Tag?.ToString();

            // 削除確認（Yesで削除、Noやダイアログを閉じた場合は何もしない）
            DialogResult result = MessageBox.Show(
                $"タブ「{page.Text}」を削除しますか？\nフォルダ内の.mdファイルもまとめてごみ箱に移動されます。",
                "タブの削除確認", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes) return;

            // 削除するタブのファイルを編集中だった場合、削除後のフォルダへ書き戻さないよう参照をクリアする
            if (!string.IsNullOrEmpty(currentFilePath) && folderPath != null &&
                currentFilePath.StartsWith(folderPath, StringComparison.OrdinalIgnoreCase))
            {
                currentFilePath = "";
            }

            // 完全削除ではなくごみ箱へ送ることで、誤って削除した場合にも復元できるようにする
            if (folderPath != null && Directory.Exists(folderPath))
            {
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(
                    folderPath,
                    Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                    Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
            }

            tabControlMain.TabPages.RemoveAt(index);
            hasUnsavedChanges = true;

            // タブが1つも無くなった場合は、サイドバーとエディタも空にしておく
            // （タブが残っていれば SelectedIndexChanged が自動発火し、サイドバーが更新される）
            if (tabControlMain.TabPages.Count == 0)
            {
                lstSidebar.Items.Clear();
                txtEditor.Text = "";
            }
        }

        // タブページ本体（従来使われていなかった領域）に、名称変更用のUIを配置する。
        // 「段落名称」欄＝現在選択中の.mdファイルの表示名、「タブ名称」欄＝タブ（フォルダ）名を、
        // それぞれ書き換えて「名称変更」ボタンを押すとリネームが反映される。
        // 各タブページごとに1組ずつ生成するため、タブ追加のたびに（BtnAddTab_Click／LoadTabsFromFoldersから）呼び出す。
        private void SetupTabPageRenameControls(TabPage page)
        {
            Label lblSidebarName = new Label { Text = "段落名称:", Left = 8, Top = 12, Width = 60, AutoSize = false };
            TextBox txtRenameSidebarName = new TextBox { Name = "txtRenameSidebarName", Left = 72, Top = 8, Width = 140 };
            Label lblTabName = new Label { Text = "タブ名称:", Left = 222, Top = 12, Width = 60, AutoSize = false };
            TextBox txtRenameTabName = new TextBox { Name = "txtRenameTabName", Left = 286, Top = 8, Width = 140, Text = page.Text };
            Button btnRenameApply = new Button { Text = "名称変更", Left = 436, Top = 6, Width = 90 };

            // ボタン押下時に、その時点の各テキストボックスの内容でリネームを実行する
            btnRenameApply.Click += (s, e) => ApplyRename(page, txtRenameSidebarName, txtRenameTabName);

            page.Controls.Add(lblSidebarName);
            page.Controls.Add(txtRenameSidebarName);
            page.Controls.Add(lblTabName);
            page.Controls.Add(txtRenameTabName);
            page.Controls.Add(btnRenameApply);
        }

        // 現在選択中のタブページにあるリネーム欄（段落名称／タブ名称）の表示を、現在の状態に合わせて更新する。
        // タブ切替・段落切替のたびに呼び出すことで、常に最新の名前が表示された状態を保つ。
        private void RefreshRenameBoxes()
        {
            if (tabControlMain.SelectedTab == null) return;

            Control[] tabNameBoxes = tabControlMain.SelectedTab.Controls.Find("txtRenameTabName", false);
            if (tabNameBoxes.Length > 0) ((TextBox)tabNameBoxes[0]).Text = tabControlMain.SelectedTab.Text;

            Control[] sidebarNameBoxes = tabControlMain.SelectedTab.Controls.Find("txtRenameSidebarName", false);
            if (sidebarNameBoxes.Length > 0) ((TextBox)sidebarNameBoxes[0]).Text = lstSidebar.SelectedItem?.ToString() ?? "";
        }

        // 「名称変更」ボタン押下時：段落名称・タブ名称のうち、変更されている方をそれぞれリネームする。
        // 片方だけの変更でもよい。
        private void ApplyRename(TabPage page, TextBox txtSidebarName, TextBox txtTabName)
        {
            // --- タブ名称（＝フォルダ名）の変更 ---
            string newTabName = SanitizeName(txtTabName.Text);
            if (!string.IsNullOrEmpty(newTabName) && newTabName != page.Text)
            {
                string oldFolder = page.Tag?.ToString();
                string newFolder = Path.Combine(currentProjectDir, newTabName);

                if (Directory.Exists(newFolder))
                {
                    MessageBox.Show("同じ名前のタブが既に存在します。");
                }
                else if (!string.IsNullOrEmpty(oldFolder) && Directory.Exists(oldFolder))
                {
                    // 編集中のファイルがこのタブ内にあった場合、リネーム後のパスに追従させる
                    bool wasEditingHere = !string.IsNullOrEmpty(currentFilePath) &&
                        currentFilePath.StartsWith(oldFolder, StringComparison.OrdinalIgnoreCase);
                    string fileNameOnly = wasEditingHere ? Path.GetFileName(currentFilePath) : null;

                    Directory.Move(oldFolder, newFolder);
                    page.Text = newTabName;
                    page.Tag = newFolder;
                    hasUnsavedChanges = true;

                    if (wasEditingHere) currentFilePath = Path.Combine(newFolder, fileNameOnly);
                }
            }

            // --- 段落名称（＝.mdファイル名）の変更 ---
            string newFileDisplayName = SanitizeName(txtSidebarName.Text);
            string currentFolder = page.Tag?.ToString();
            if (!string.IsNullOrEmpty(newFileDisplayName) && lstSidebar.SelectedItem != null &&
                newFileDisplayName != lstSidebar.SelectedItem.ToString() && !string.IsNullOrEmpty(currentFolder))
            {
                string oldFile = Path.Combine(currentFolder, lstSidebar.SelectedItem.ToString() + ".md");
                string newFile = Path.Combine(currentFolder, newFileDisplayName + ".md");

                if (File.Exists(newFile))
                {
                    MessageBox.Show("同じ名前の段落が既に存在します。");
                }
                else if (File.Exists(oldFile))
                {
                    // リネーム前に、エディタの最新内容を確実に保存しておく
                    SaveCurrentFile();

                    File.Move(oldFile, newFile);
                    currentFilePath = newFile;
                    hasUnsavedChanges = true;

                    int selectedIndex = lstSidebar.SelectedIndex;
                    lstSidebar.Items[selectedIndex] = newFileDisplayName;
                }
            }

            // 変更後の状態でリネーム欄の表示を最新化する
            RefreshRenameBoxes();
        }

        // 「+」ボタン押下時：タブ名を入力してもらい、新規タブ（＝WikiProject配下の新規フォルダ）を作成する
        private void BtnAddTab_Click(object sender, EventArgs e)
        {
            using (var dialog = new TextInputDialog("新規タブ", "タブ名を入力してください:", "新規タブ"))
            {
                // キャンセルされた場合は何もしない
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                // フォルダ名として使用できない文字（\ / : * ? " < > | など）を除去する
                string tabName = SanitizeName(dialog.InputText);
                if (string.IsNullOrEmpty(tabName)) tabName = "新規タブ";

                // 同名フォルダが既に存在する場合は "(1)", "(2)"... と連番を付けて重複を回避する
                string uniqueName = tabName;
                int suffix = 1;
                while (Directory.Exists(Path.Combine(currentProjectDir, uniqueName)))
                {
                    uniqueName = $"{tabName}({suffix})";
                    suffix++;
                }

                // タブを追加する前に、現在編集中の内容を保存しておく
                SaveCurrentFile();

                // 新規フォルダを作成し、空のままだと編集できないためサンプルの.mdファイルを1つ生成する
                string newFolder = Path.Combine(currentProjectDir, uniqueName);
                Directory.CreateDirectory(newFolder);
                File.WriteAllText(Path.Combine(newFolder, "1.概要.md"), $"# {uniqueName}\r\nここから仕様を書き始めます。");

                // 名称変更用のUIをタブページ内に配置してから、タブを追加して選択状態にする
                // （選択によりSelectedIndexChangedが発火し、サイドバーが更新される）
                TabPage newPage = new TabPage { Text = uniqueName, Tag = newFolder };
                SetupTabPageRenameControls(newPage);
                tabControlMain.TabPages.Add(newPage);
                tabControlMain.SelectedTab = newPage;
                hasUnsavedChanges = true;
            }
        }

        // 「出力」ボタン押下時：メニューの「HTML出力」と共通の処理を呼び出す
        private void BtnExport_Click(object sender, EventArgs e) => ExportCurrentFileToHtml();

        // 現在編集中の1ファイルをHTMLとして書き出し、既定のブラウザで開く。
        // 右下の「出力」ボタンとメニューの「HTML出力」の両方から共通で呼び出す処理。
        private void ExportCurrentFileToHtml()
        {
            try
            {
                // 出力前に編集中の内容を保存しておく
                SaveCurrentFile();

                if (string.IsNullOrEmpty(currentFilePath) || !File.Exists(currentFilePath))
                {
                    MessageBox.Show("出力対象のファイルが選択されていません。");
                    return;
                }

                // 保存先をダイアログで選んでもらう
                using (var dialog = new SaveFileDialog
                {
                    Filter = "HTMLファイル (*.html)|*.html",
                    FileName = Path.GetFileNameWithoutExtension(currentFilePath) + ".html"
                })
                {
                    if (dialog.ShowDialog(this) != DialogResult.OK) return;

                    string outputFilePath = dialog.FileName;
                    string outputDir = Path.GetDirectoryName(outputFilePath);

                    // 画像がHTML単体でも表示できるよう、assetsフォルダの中身を出力先の隣にまるごとコピーする
                    string outputAssetsDir = Path.Combine(outputDir, "assets");
                    Directory.CreateDirectory(outputAssetsDir);
                    foreach (string srcFile in Directory.GetFiles(assetsDir))
                    {
                        string destFile = Path.Combine(outputAssetsDir, Path.GetFileName(srcFile));
                        File.Copy(srcFile, destFile, true);
                    }

                    // 出力時は画像参照を、出力フォルダから見た相対パス（assets/xxx）に差し替える
                    string html = BuildHtmlDocument(txtEditor.Text, "assets");
                    File.WriteAllText(outputFilePath, html);

                    // 既定のブラウザで開き、その場で表示内容を確認できるようにする
                    Process.Start(new ProcessStartInfo(outputFilePath) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                // 出力に失敗した場合はエラー内容をダイアログで通知する
                MessageBox.Show("出力エラー: " + ex.Message);
            }
        }

        // 指定フォルダ内の既存.mdファイルの先頭番号（例:"1.概要.md"の"1"）の最大値を調べ、その次の番号を返す。
        // 段落追加(BtnAddFile_Click)・外部mdファイルの取り込み(MenuLoadMdFile_Click)で共通利用する。
        private int GetNextParagraphNumber(string folder)
        {
            int nextNumber = 1;
            foreach (string file in Directory.GetFiles(folder, "*.md"))
            {
                string baseName = Path.GetFileNameWithoutExtension(file);
                int dotIndex = baseName.IndexOf('.');
                if (dotIndex > 0 && int.TryParse(baseName.Substring(0, dotIndex), out int num) && num + 1 > nextNumber)
                    nextNumber = num + 1;
            }
            return nextNumber;
        }

        // 「作業内容のセーブ」：現在のWikiProjectフォルダ全体（全タブ・全段落・assets）を
        // 1つの.spcファイル（実体はZIP形式）に固めて保存する。
        // 保存できた場合はtrue、ダイアログをキャンセルした場合や失敗した場合はfalseを返す
        // （「作業内容のロード」で保存してから読み込む場合の判定に使う）。
        private bool SaveWorkToSpc()
        {
            // 保存前に、編集中の内容を確実にファイルへ反映しておく
            SaveCurrentFile();

            using (var dialog = new SaveFileDialog { Filter = "作業内容ファイル (*.spc)|*.spc", FileName = "WikiProject.spc" })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return false;

                try
                {
                    // 同名ファイルが既に存在するとZipFile.CreateFromDirectoryが失敗するため、先に削除する
                    if (File.Exists(dialog.FileName)) File.Delete(dialog.FileName);
                    ZipFile.CreateFromDirectory(currentProjectDir, dialog.FileName);

                    hasUnsavedChanges = false;
                    MessageBox.Show("作業内容を保存しました。");
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("保存エラー: " + ex.Message);
                    return false;
                }
            }
        }

        // 「作業内容のロード」：.spcファイルを読み込み、現在のWikiProjectの内容を完全に置き換える。
        // 未保存の変更がある場合は、先に「保存する／このまま続行／キャンセル」を確認する。
        private void MenuLoadWork_Click(object sender, EventArgs e)
        {
            using (var openDialog = new OpenFileDialog { Filter = "作業内容ファイル (*.spc)|*.spc" })
            {
                if (openDialog.ShowDialog(this) != DialogResult.OK) return;

                if (hasUnsavedChanges)
                {
                    DialogResult confirm = MessageBox.Show(this,
                        "現在の作業内容に未保存の変更があります。読み込むと現在の内容は失われます。\n\n" +
                        "[はい] 保存してから読み込む\n[いいえ] 保存せずに読み込む（変更は破棄されます）\n[キャンセル] 読み込みを中止する",
                        "確認", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

                    if (confirm == DialogResult.Cancel) return;
                    if (confirm == DialogResult.Yes)
                    {
                        // 保存ダイアログをキャンセルした場合等は、読み込み自体も中止する
                        if (!SaveWorkToSpc()) return;
                    }
                    // いいえの場合はそのまま処理を続行し、現在の変更を破棄する
                }

                try
                {
                    // 削除予定のフォルダへ書き戻さないよう、先に参照をクリアしておく
                    currentFilePath = "";

                    // 現在のWikiProjectの中身を完全に削除してから、.spcの内容を展開する
                    if (Directory.Exists(currentProjectDir)) Directory.Delete(currentProjectDir, true);
                    ZipFile.ExtractToDirectory(openDialog.FileName, currentProjectDir);

                    // assetsフォルダが無い場合に備えて再作成しつつ、タブを再読み込みする
                    InitializeProjectFolders();
                    LoadTabsFromFolders();

                    hasUnsavedChanges = false;
                    MessageBox.Show("作業内容を読み込みました。");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("読み込みエラー: " + ex.Message);
                }
            }
        }

        // 「mdファイルのロード」：外部の.mdファイルを、現在選択中のタブに新規段落として取り込む
        private void MenuLoadMdFile_Click(object sender, EventArgs e)
        {
            if (tabControlMain.SelectedTab == null)
            {
                MessageBox.Show("先にタブを選択してください。");
                return;
            }
            string folder = tabControlMain.SelectedTab.Tag?.ToString();
            if (string.IsNullOrEmpty(folder)) return;

            using (var dialog = new OpenFileDialog { Filter = "Markdownファイル (*.md)|*.md" })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                string sourceContent = File.ReadAllText(dialog.FileName);
                string baseName = SanitizeName(Path.GetFileNameWithoutExtension(dialog.FileName));
                if (string.IsNullOrEmpty(baseName)) baseName = "無題";

                // 既存の段落追加と同じ採番ルールで番号を振る
                int nextNumber = GetNextParagraphNumber(folder);
                string displayName = $"{nextNumber}.{baseName}";
                string newFilePath = Path.Combine(folder, displayName + ".md");

                SaveCurrentFile();
                File.WriteAllText(newFilePath, sourceContent);

                lstSidebar.Items.Add(displayName);
                // 選択するとLstSidebar_SelectedIndexChangedが発火し、エディタに読み込まれる
                lstSidebar.SelectedItem = displayName;
                hasUnsavedChanges = true;
            }
        }

        // 終了時：未保存の変更があれば確認ダイアログを表示し、「いいえ」の場合は終了をキャンセルする
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!hasUnsavedChanges) return;

            DialogResult result = MessageBox.Show(this,
                "作業内容が失われますがよろしいですか？", "終了確認", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes) e.Cancel = true;
        }

        // 「+」ボタン押下時：現在のタブ内に新規の段落（.mdファイル）を追加する
        private void BtnAddFile_Click(object sender, EventArgs e)
        {
            if (tabControlMain.SelectedTab == null)
            {
                MessageBox.Show("先にタブを選択してください。");
                return;
            }
            string folder = tabControlMain.SelectedTab.Tag?.ToString();
            if (string.IsNullOrEmpty(folder)) return;

            using (var dialog = new TextInputDialog("新規段落", "段落名を入力してください:", ""))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                string name = SanitizeName(dialog.InputText);
                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("段落名を入力してください。");
                    return;
                }

                // 既存の.mdファイルの先頭番号（例："1.概要.md"の"1"）の最大値を調べ、その次の番号を採番する
                int nextNumber = GetNextParagraphNumber(folder);

                string displayName = $"{nextNumber}.{name}";
                string newFilePath = Path.Combine(folder, displayName + ".md");

                // 段落を追加する前に、現在編集中の内容を保存しておく
                SaveCurrentFile();
                File.WriteAllText(newFilePath, $"# {name}\r\nここから内容を書き始めます。");

                lstSidebar.Items.Add(displayName);
                // 選択するとLstSidebar_SelectedIndexChangedが発火し、エディタに読み込まれる
                lstSidebar.SelectedItem = displayName;
                hasUnsavedChanges = true;
            }
        }

        // 「-」ボタン押下時：サイドバーで選択中の段落（.mdファイル）を削除する
        private void BtnRemoveFile_Click(object sender, EventArgs e)
        {
            if (lstSidebar.SelectedItem == null)
            {
                MessageBox.Show("削除する段落を選択してください。");
                return;
            }
            string folder = tabControlMain.SelectedTab?.Tag?.ToString();
            if (string.IsNullOrEmpty(folder)) return;

            string displayName = lstSidebar.SelectedItem.ToString();
            string filePath = Path.Combine(folder, displayName + ".md");

            // 削除確認（Yesで削除、Noやダイアログを閉じた場合は何もしない）
            DialogResult result = MessageBox.Show(
                $"段落「{displayName}」を削除しますか？\nごみ箱に移動されます。",
                "段落の削除確認", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes) return;

            // 完全削除ではなくごみ箱へ送ることで、誤って削除した場合にも復元できるようにする
            if (File.Exists(filePath))
            {
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
                    filePath,
                    Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                    Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
            }

            // 削除した段落を編集中だった場合、参照をクリアしておく
            if (!string.IsNullOrEmpty(currentFilePath) && currentFilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase))
                currentFilePath = "";

            lstSidebar.Items.RemoveAt(lstSidebar.SelectedIndex);
            hasUnsavedChanges = true;

            // 段落が1つも無くなった場合は、エディタも空にしておく
            // （残っていれば SelectedIndexChanged が自動発火し、次の段落が読み込まれる）
            if (lstSidebar.Items.Count == 0) txtEditor.Text = "";

            RefreshRenameBoxes();
        }
    }
}