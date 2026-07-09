#nullable disable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Markdig;

// SpecWikiEditor アプリケーションのメインフォーム実装。
// - タブ/段落（フォルダ・Markdownファイル）の管理
// - Markdown エディタとリアルタイムプレビュー(WebView2)
// - 画像アセットの管理(assetsフォルダ) と HTML 出力
namespace SpecWikiEditor
{
    // メインフォームクラス。UIイベントとファイル操作、プレビュー更新を担当する。
    public partial class Form1 : Form
    {
        // プロジェクトルートへのパス。Default.cfg の設定に基づき Form1_Load で決定される。
        private string currentProjectDir;
        // 画像などのアセットを保存するフォルダパス。同じく Form1_Load で決定される。
        private string assetsDir;
        // 現在編集中のMarkdownファイルのフルパス
        private string currentFilePath = "";

        // 現在編集中の段落(ページ)に設定されている、メインページの背景色("#rrggbb")。
        // 未設定の場合は null。.mdファイルの先頭に埋め込む隠しマーカー(下記
        // ParagraphBgColorMarkerPattern)として保存され、LstSidebar_SelectedIndexChangedで
        // 読み込み時に抽出し、SaveCurrentFileで保存時に書き戻す。
        private string currentParagraphBgColor = null;

        // .mdファイルの先頭に埋め込む、メインページ背景色マーカーの正規表現。
        // 例: "<!-- bgcolor:#ffcc00 -->" のような1行。エディタ上には表示させず、
        // ファイルの読み書き時にのみ透過的に付け外しする「隠しマーカー」として扱う。
        private static readonly Regex ParagraphBgColorMarkerPattern =
            new Regex(@"^<!--\s*bgcolor:\s*(#[0-9A-Fa-f]{6})\s*-->\r?\n?");

        // タブ(フォルダ)ごとの設定を保存する小さな設定ファイルの名前。
        // 現状は「サイドメニューの背景色」のみを保持する。"*.md" に一致しないため、
        // 段落一覧の列挙(Directory.GetFiles(folder, "*.md"))には影響しない。
        private const string TabMetaFileName = ".tabmeta.cfg";

        // Default.cfg（保存先パス・フォルダ名・起動時最大化）の内容を保持する
        private AppConfig appConfig;

        // 直近に保存・読み込みした「作業内容(.spc)」ファイルのフルパス。
        // まだ一度も保存・読み込みしていない場合は null（タイトルバーには既定の "Form1" を表示する）。
        private string currentWorkFilePath = null;

        // 「作業内容のセーブ」以降に変更が加えられたかどうか。終了時の確認ダイアログの判定に使う。
        // プロパティ化し、値が変わるたびに自動でタイトルバー(UpdateTitle)を更新する。
        // これにより、更新箇所ごとに個別にUpdateTitle()を呼び忘れる心配がなくなる。
        private bool _hasUnsavedChanges = false;
        private bool hasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set
            {
                _hasUnsavedChanges = value;
                UpdateTitle();
            }
        }

        // タイトルバーの表示を、現在の保存先ファイル名と未保存フラグに合わせて更新する。
        // ・保存/読み込み先がまだ無い場合は既定の "Form1"
        // ・保存/読み込み先がある場合はそのファイル名(拡張子なし)
        // ・未保存の変更がある場合は末尾に " *" を付けて知らせる
        private void UpdateTitle()
        {
            string baseTitle = string.IsNullOrEmpty(currentWorkFilePath)
                ? "Form1"
                : Path.GetFileNameWithoutExtension(currentWorkFilePath);
            this.Text = baseTitle + (_hasUnsavedChanges ? " *" : "");
        }

        // プレビュー・出力HTMLの両方で使う共通CSS。
        // ・画像がプレビュー/出力先の表示幅に収まるよう自動縮小させる（元の画像ファイルには一切手を加えない）
        // ・背景色/文字色を明示指定し、閲覧環境のダークモード設定による自動反転（黒背景に黒文字等）を防ぐ
        // ・テーブルに罫線を付ける（Markdigの出力自体には罫線用のスタイルが含まれておらず、
        //   GitHub上ではGitHub側のCSSで罫線が付いて見えるだけなので、こちら側でも明示的に指定する）
        private const string CommonPreviewCss =
            "html, body { background-color: #ffffff; color: #000000; } " +
            "img { max-width: 100%; height: auto; } " +
            "table { border-collapse: collapse; } " +
            "th, td { border: 1px solid #999; padding: 4px 8px; } " +
            "th { background-color: #f0f0f0; }";

        // 「サイト出力」で生成する各ページ共通のCSS。BuildSitePageHtmlが埋め込むHTML構造
        // （.tabbar / .layout > .sidebar + .content）にそのまま対応させている。
        // タブバー・サイドバー・本文の3ブロックからなる、シンプルで読みやすいレイアウトにしている
        // （配色・デザインは今回は最低限にとどめ、実際に使ってみて必要になったタイミングで見直す想定）。
        private const string SiteCommonCss = @"
body { margin:0; font-family: 'Yu Gothic UI', 'Meiryo', sans-serif; background:#ffffff; color:#000000; }
/* 画面上部の帯。全タブ分のボタン(.tab-button)を横並びで表示する */
.tabbar { background:#2d2d30; padding:8px 8px 0 8px; }
.tab-button { margin-right:4px; padding:8px 14px; border:none; border-radius:4px 4px 0 0; cursor:pointer; background:#555; color:#fff; font-size:14px; }
/* 現在選択中のタブのボタンだけ色を変えて強調する。JSのshowTab()がこのクラスを付け外しする */
.tab-button.active { background:#007acc; }
/* サイドバーと本文を横並びにする2カラムレイアウト */
.layout { display:flex; align-items:flex-start; }
.sidebar { width:220px; flex-shrink:0; border-right:1px solid #ddd; box-sizing:border-box; min-height:100vh; }
/* 各タブ分のリンク一覧(div)。既定では全て非表示にし、.active が付いているものだけを表示する。
   これにより「全タブ分のサイドバーを埋め込みつつ、見た目には1つしか出さない」を実現している。
   paddingとmin-heightをこちら側に持たせることで、タブごとの背景色(インラインスタイル)が
   サイドバー全体を隙間なく塗りつぶすようにしている */
.sidebar-list { display:none; padding:12px; box-sizing:border-box; color:#333; }
.sidebar-list.active { display:block; min-height:100vh; }
/* リンクの文字色は親(.sidebar-list)の色を継承する。タブごとのインラインスタイルで
   color を指定した場合、それがそのままリンクの文字色にも反映されるようにするため */
.sidebar-list a { display:block; padding:6px 4px; color:inherit; text-decoration:none; border-radius:4px; }
.sidebar-list a:hover { background:rgba(128,128,128,0.15); }
/* 「今まさに見ているこのページ」へのリンクを、現在地としてハイライトする */
.sidebar-list a.current { font-weight:bold; color:#007acc; background:#eaf4fc; }
.content { flex:1; padding:24px; min-width:0; }
/* 画像がコンテンツ幅からはみ出さないよう自動縮小する（プレビュー/単体HTML出力と同じ考え方） */
.content img { max-width:100%; height:auto; }
/* テーブルに罫線を付ける（プレビュー/単体HTML出力と同じ考え方） */
.content table { border-collapse: collapse; }
.content th, .content td { border: 1px solid #999; padding: 4px 8px; }
.content th { background-color: #f0f0f0; }
";

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

                // Ctrl+S（上書き保存）をどのコントロールにフォーカスがあっても拾えるようにする
                this.KeyPreview = true;
                this.KeyDown += Form1_KeyDown;

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

                // 「+」ボタンで新規タブ追加、「HTML出力」「サイト出力」ボタンでそれぞれの出力を行う
                btnAddTab.Click += BtnAddTab_Click;
                btnExport.Click += BtnExport_Click;
                btnExportSite.Click += (s, e) => ExportSiteToFolder();

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

                // 編集ツールバーの各ボタンにMarkdown/HTML挿入処理を割り当てる。
                // 行頭に記号を付ける系のボタンは、既に同じ書式が付いている行であれば
                // 逆に取り除く（トグル動作）。第2引数の正規表現は「既にこの書式が付いている」
                // ことを判定するためのもの。
                btnHeading1.Click += (s, e) => InsertPrefixOnSelectedLines(_ => "# ", new Regex(@"^#\s"));
                btnHeading2.Click += (s, e) => InsertPrefixOnSelectedLines(_ => "## ", new Regex(@"^##\s"));
                // 「- 」で始まる箇条書きは、チェックリスト（"- [ ] "）と先頭が同じになるため、
                // 誤ってチェックリスト行を箇条書きと誤認しないよう "- [" の形は除外する
                btnBulletList.Click += (s, e) => InsertPrefixOnSelectedLines(_ => "- ", new Regex(@"^-\s(?!\[)"));
                btnNumberedList.Click += (s, e) => InsertPrefixOnSelectedLines(i => $"{i + 1}. ", new Regex(@"^\d+\.\s"));
                btnCheckList.Click += (s, e) => InsertPrefixOnSelectedLines(_ => "- [ ] ", new Regex(@"^-\s\[.\]\s"));
                btnQuote.Click += (s, e) => InsertPrefixOnSelectedLines(_ => "> ", new Regex(@"^>\s"));
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

                // 「サイドメニューBG色」：左クリックで現在のタブに背景色を設定、右クリックでリセットする
                btnSidebarBgColor.Click += (s, e) => SetTabSidebarBgColorInteractive();
                btnSidebarBgColor.MouseUp += (s, e) => { if (e.Button == MouseButtons.Right) ResetTabSidebarBgColor(); };

                // 「サイドメニュー文字色」：左クリックで現在のタブに文字色を設定、右クリックでリセットする
                btnSidebarTextColor.Click += (s, e) => SetTabSidebarTextColorInteractive();
                btnSidebarTextColor.MouseUp += (s, e) => { if (e.Button == MouseButtons.Right) ResetTabSidebarTextColor(); };

                // 「メインページBG色」：左クリックで現在の段落に背景色を設定、右クリックでリセットする
                btnMainBgColor.Click += (s, e) => SetParagraphMainBgColorInteractive();
                btnMainBgColor.MouseUp += (s, e) => { if (e.Button == MouseButtons.Right) ResetParagraphMainBgColor(); };

                btnFindReplace.Click += (s, e) => new FindReplaceDialog(txtEditor).Show(this);

                // 文字サイズドロップダウンの選択肢を用意し、選択されたら選択範囲をそのサイズで囲む
                foreach (var option in FontSizeOptions) cmbFontSize.Items.Add(option.Label);
                cmbFontSize.SelectedIndexChanged += CmbFontSize_SelectedIndexChanged;

                // サイドバーの「+」で段落追加、「-」で選択中の段落を削除する
                btnAddFile.Click += BtnAddFile_Click;
                btnRemoveFile.Click += BtnRemoveFile_Click;

                // 「ファイル」メニューの各項目にイベントを割り当てる
                menuNewProject.Click += MenuNewProject_Click;
                // 「作業内容のセーブ」：常に保存先をダイアログで選ばせ、保存完了メッセージも表示する
                menuSaveWork.Click += (s, e) => SaveWork(forceDialog: true, showCompletionMessage: true);
                // 「作業内容の上書き保存」：既に保存先が分かっていればそこへ無言で上書き保存する
                // （まだ保存先が無い場合のみダイアログを表示する）。Ctrl+Sと同じ処理。
                menuOverwriteSave.Click += (s, e) => SaveWork(forceDialog: false, showCompletionMessage: false);
                menuLoadWork.Click += MenuLoadWork_Click;
                menuLoadMdFile.Click += MenuLoadMdFile_Click;
                menuExportHtml.Click += (s, e) => ExportCurrentFileToHtml();
                menuExportSite.Click += (s, e) => ExportSiteToFolder();
                menuSettings.Click += MenuSettings_Click;
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
                // Default.cfg を読み込む（存在しなければ既定値で新規作成する）
                appConfig = AppConfig.LoadOrCreate(out bool wasConfigCreated);
                currentProjectDir = appConfig.GetProjectRootPath();
                assetsDir = Path.Combine(currentProjectDir, "assets");

                // 起動時の最大化設定を反映する
                if (appConfig.MaximizeOnStartup) this.WindowState = FormWindowState.Maximized;

                if (wasConfigCreated)
                {
                    // 今回初めてDefault.cfgを作成した場合（アップデート後の初回起動、または
                    // 純粋な初回起動）。旧バージョンで使われていたデスクトップ上のWikiProject
                    // フォルダが残っていれば、新しい保存先へ移行する。
                    // このタイミングだけは「常に空で起動」を適用せず、移行されたデータ（または
                    // 純粋な初回起動用の既定タブ）をそのまま表示する
                    // （直後に空へリセットしてしまうと、移行した意味が無くなるため）。
                    MigrateLegacyProjectFolder();
                    InitializeProjectFolders();
                    LoadTabsFromFolders();
                }
                else
                {
                    // 通常の起動。Excel/PowerPoint等の事務系アプリと同様、
                    // 前回の続きを自動で開くのではなく、常に新規（「TOP」タブのみの空の状態）
                    // から開始する。前回までの作業内容を残しておきたい場合は、終了時の
                    // 保存確認、または「作業内容のセーブ」で明示的に.spcとして保存しておく必要がある。
                    ResetProjectToBlank();
                }

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
            // パネルの幅が0以下、または子コントロールが1つも無い場合は計算せずに終了する
            if (pnlEditorToolbar.Width <= 0 || pnlEditorToolbar.Controls.Count == 0) return;
            // 各ボタンのBottom座標を調べ、最大値を求める
            int maxBottom = 0;
            // Margin.Bottomを加算して、ボタンの下端から余白まで含めた座標を計算する
            foreach (Control control in pnlEditorToolbar.Controls)
            {
                int bottom = control.Bottom + control.Margin.Bottom;
                if (bottom > maxBottom) maxBottom = bottom;
            }
            // パネルのPadding.Bottomも加算して、パネル自身の下端まで含めた高さを計算する
            int neededHeight = maxBottom + pnlEditorToolbar.Padding.Bottom;
            // すでに必要な高さと同じであれば何もしない
            if (pnlEditorToolbar.Height == neededHeight) return;
            // 計算した高さをパネルに反映する
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

        // 旧バージョン（Default.cfg導入前）で使われていた「デスクトップ\WikiProject」フォルダが
        // 残っている場合、新しい保存先（Default.cfgのSavePath\FolderName）へ移動する。
        // Default.cfgを初めて作成したタイミング（＝アップデート後の初回起動）でのみ呼び出す。
        private void MigrateLegacyProjectFolder()
        {
            string legacyProjectDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "WikiProject");

            if (!Directory.Exists(legacyProjectDir) || Directory.Exists(currentProjectDir)) return;

            try
            {
                string parentDir = Path.GetDirectoryName(currentProjectDir);
                if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
                    Directory.CreateDirectory(parentDir);

                Directory.Move(legacyProjectDir, currentProjectDir);
                MessageBox.Show(
                    $"デスクトップの既存データ(WikiProject)を新しい保存先に移行しました。\n{currentProjectDir}",
                    "データ移行");
            }
            catch (Exception ex)
            {
                MessageBox.Show("既存データの移行に失敗しました: " + ex.Message);
            }
        }

        // 「設定」メニュー押下時：Default.cfgの内容を編集するダイアログを表示する
        private void MenuSettings_Click(object sender, EventArgs e)
        {
            using (var dialog = new SettingsDialog(appConfig))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                string newSavePath = dialog.SavePathResult;
                string newFolderName = SanitizeName(dialog.FolderNameResult);
                if (string.IsNullOrEmpty(newSavePath) || string.IsNullOrEmpty(newFolderName))
                {
                    MessageBox.Show("保存先パスとフォルダ名は必須です。");
                    return;
                }

                appConfig.SavePath = newSavePath;
                appConfig.FolderName = newFolderName;
                appConfig.MaximizeOnStartup = dialog.MaximizeResult;
                appConfig.Save();

                MessageBox.Show("設定を保存しました。変更を反映するにはアプリを再起動してください。");
            }
        }

        private void InitializeProjectFolders()
        {
            // プロジェクトルートとアセットフォルダがなければ作成
            if (!Directory.Exists(currentProjectDir)) Directory.CreateDirectory(currentProjectDir);
            if (!Directory.Exists(assetsDir)) Directory.CreateDirectory(assetsDir);

            // プロジェクトフォルダ内にタブ用フォルダが無ければ、初期フォルダとサンプルファイルを作成する。
            // 「サイト出力」機能が「TOP」タブを前提とするため、既定タブ名もTOPで統一している。
            if (Directory.GetDirectories(currentProjectDir).Length == 0)
            {
                string tab1 = Path.Combine(currentProjectDir, "TOP");
                Directory.CreateDirectory(tab1);
                File.WriteAllText(Path.Combine(tab1, "1.概要.md"), "# TOP\r\nここから仕様を書き始めます。");
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

            // 「TOP」タブ（サイト出力時の入口ページ）が存在する場合、常に一番左に来るよう並べ替える
            EnsureTopTabIsFirst();

            // 最初のタブ（TOPタブがあればTOP）を選択してサイドバーを読み込む
            if (tabControlMain.TabPages.Count > 0) TabControlMain_SelectedIndexChanged(null, null);
        }

        // 「TOP」という名前のタブ（サイト出力機能で、サイトの入口ページとして扱う予約タブ名）が
        // 存在する場合、タブ一覧の先頭（一番左）に来るよう並び替える。
        // 「TOP」タブが無い場合や、既に先頭にある場合は何もしない。
        // タブ一覧の再構築時(LoadTabsFromFolders)・新規タブ追加時(BtnAddTab_Click)・
        // タブ名称変更時(ApplyRename、他のタブをTOPにリネームした場合)のいずれからも呼び出す。
        private void EnsureTopTabIsFirst()
        {
            for (int i = 0; i < tabControlMain.TabPages.Count; i++)
            {
                if (tabControlMain.TabPages[i].Text != "TOP") continue;
                if (i == 0) return; // 既に先頭にあるので並び替え不要

                // TabPages.Insert/RemoveAtの間、選択中のタブが変わったように見えてしまわないよう、
                // 現在選択中のタブを覚えておき、並び替え後に選択し直す
                TabPage previouslySelected = tabControlMain.SelectedTab;

                TabPage topPage = tabControlMain.TabPages[i];
                tabControlMain.TabPages.RemoveAt(i);
                tabControlMain.TabPages.Insert(0, topPage);

                if (previouslySelected != null) tabControlMain.SelectedTab = previouslySelected;
                return;
            }
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
            // ListBoxは内部のスクロール位置(TopIndex)が、新規タブ追加直後などの連続したレイアウト
            // 変更の影響でリセットされず、1件目が表示範囲外に隠れてしまうことがあるため明示的に戻す
            lstSidebar.TopIndex = 0;

            // タブ切替に伴い、タブページ内のリネーム欄の表示も最新の状態に更新する
            RefreshRenameBoxes();

            // タブ切替に伴い、サイドバー(lstSidebar)の背景色をこのタブの設定色に更新する
            ApplyCurrentTabSidebarColor();

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
            if (File.Exists(currentFilePath))
            {
                // 先頭の背景色マーカーを抽出し、エディタには残りの本文だけを表示する
                string rawContent = File.ReadAllText(currentFilePath);
                currentParagraphBgColor = ExtractParagraphBgColor(rawContent, out string remainingContent);
                txtEditor.Text = remainingContent;
            }
            else
            {
                currentParagraphBgColor = null;
            }

            // 段落の選択に伴い、リネーム欄の「段落名称」表示も最新の状態に更新する
            RefreshRenameBoxes();

            // 段落切替に伴い、プレビューの背景色もこの段落の設定色に更新する
            UpdatePreview();

            // 段落切替時も、確実にエディタへフォーカスを戻しておく
            txtEditor.Focus();
        }

        private void SaveCurrentFile()
        {
            // currentFilePath が設定されていればテキストボックスの内容をファイルに書き込む。
            // メインページ背景色が設定されている場合は、先頭に隠しマーカーを付けて書き込む。
            if (!string.IsNullOrEmpty(currentFilePath))
                File.WriteAllText(currentFilePath, BuildParagraphContentWithBgMarker(currentParagraphBgColor, txtEditor.Text));
        }

        // .mdファイルの生の内容から、先頭の背景色マーカー(<!-- bgcolor:#rrggbb -->)を検出して取り除く。
        // マーカーが見つかった場合はその色("#rrggbb")を返し、remainingContentにはマーカーを除いた
        // 本文を格納する。見つからなければ null を返し、remainingContentは元の内容のままにする。
        private string ExtractParagraphBgColor(string rawContent, out string remainingContent)
        {
            Match match = ParagraphBgColorMarkerPattern.Match(rawContent ?? "");
            if (match.Success)
            {
                remainingContent = rawContent.Substring(match.Length);
                return match.Groups[1].Value;
            }
            remainingContent = rawContent;
            return null;
        }

        // 本文の先頭に、背景色マーカーを付け加える（bgColorがnullの場合は本文をそのまま返す）。
        // ExtractParagraphBgColorの逆の処理で、ファイル保存時に対で使う。
        private string BuildParagraphContentWithBgMarker(string bgColor, string content)
        {
            return string.IsNullOrEmpty(bgColor) ? content : $"<!-- bgcolor:{bgColor} -->\r\n{content}";
        }

        // タブフォルダの .tabmeta.cfg から、指定したキーの値を読み取る（無ければnull）。
        // SidebarBgColor・SidebarTextColor など、複数の設定項目を同じファイル形式(Key=Value)で
        // 扱うための汎用処理。
        private string GetTabMetaValue(string tabFolder, string key)
        {
            if (string.IsNullOrEmpty(tabFolder)) return null;
            string metaPath = Path.Combine(tabFolder, TabMetaFileName);
            if (!File.Exists(metaPath)) return null;

            foreach (string line in File.ReadAllLines(metaPath))
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
                    return trimmed.Substring(key.Length + 1).Trim();
            }
            return null;
        }

        // タブフォルダの .tabmeta.cfg に、指定したキーの値を書き込む（valueがnull/空ならそのキーだけ削除する）。
        // 複数キーを1つのファイルにまとめて保持するため、既存の内容を読み込んでから該当キーだけ
        // 更新し、全体を書き直す。全キーが無くなった場合は設定ファイル自体を削除する。
        private void SetTabMetaValue(string tabFolder, string key, string value)
        {
            if (string.IsNullOrEmpty(tabFolder)) return;
            string metaPath = Path.Combine(tabFolder, TabMetaFileName);

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (File.Exists(metaPath))
            {
                foreach (string line in File.ReadAllLines(metaPath))
                {
                    string trimmed = line.Trim();
                    int sep = trimmed.IndexOf('=');
                    if (sep <= 0) continue;
                    values[trimmed.Substring(0, sep)] = trimmed.Substring(sep + 1);
                }
            }

            if (string.IsNullOrEmpty(value)) values.Remove(key);
            else values[key] = value;

            if (values.Count == 0)
            {
                if (File.Exists(metaPath)) File.Delete(metaPath);
                return;
            }

            StringBuilder sb = new StringBuilder();
            foreach (var pair in values) sb.Append($"{pair.Key}={pair.Value}\r\n");
            File.WriteAllText(metaPath, sb.ToString());
        }

        // 指定したタブフォルダに設定されている、サイドメニュー背景色を取得する（未設定ならnull）
        private string GetTabSidebarBgColor(string tabFolder) => GetTabMetaValue(tabFolder, "SidebarBgColor");

        // 指定したタブフォルダに、サイドメニュー背景色を設定する。
        // colorHexがnull/空の場合は設定を削除し、「未設定」の状態に戻す。
        private void SetTabSidebarBgColor(string tabFolder, string colorHex) => SetTabMetaValue(tabFolder, "SidebarBgColor", colorHex);

        // 指定したタブフォルダに設定されている、サイドメニュー文字色を取得する（未設定ならnull）
        private string GetTabSidebarTextColor(string tabFolder) => GetTabMetaValue(tabFolder, "SidebarTextColor");

        // 指定したタブフォルダに、サイドメニュー文字色を設定する。
        // colorHexがnull/空の場合は設定を削除し、「未設定」の状態に戻す。
        private void SetTabSidebarTextColor(string tabFolder, string colorHex) => SetTabMetaValue(tabFolder, "SidebarTextColor", colorHex);

        // 現在選択中のタブに設定されているサイドメニュー背景色・文字色を、実際にlstSidebarへ反映する。
        // 未設定の場合はそれぞれ既定の色(SystemColors.Window / SystemColors.WindowText)に戻す。
        // 背景色によっては文字が見えなくなることがあるため、文字色も個別に設定できるようにしている。
        private void ApplyCurrentTabSidebarColor()
        {
            string tabFolder = tabControlMain.SelectedTab?.Tag?.ToString();

            string bgColorHex = GetTabSidebarBgColor(tabFolder);
            try
            {
                lstSidebar.BackColor = string.IsNullOrEmpty(bgColorHex) ? SystemColors.Window : ColorTranslator.FromHtml(bgColorHex);
            }
            catch
            {
                // 設定ファイルの内容が壊れていた場合は既定色にフォールバックする
                lstSidebar.BackColor = SystemColors.Window;
            }

            string textColorHex = GetTabSidebarTextColor(tabFolder);
            try
            {
                lstSidebar.ForeColor = string.IsNullOrEmpty(textColorHex) ? SystemColors.WindowText : ColorTranslator.FromHtml(textColorHex);
            }
            catch
            {
                lstSidebar.ForeColor = SystemColors.WindowText;
            }
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

            // プレビューでは画像を仮想ホスト名（wiki-assets）経由で参照させ、
            // 背景色は現在の段落に設定されているメインページ背景色を反映する
            string fullHtml = BuildHtmlDocument(txtEditor.Text, "https://wiki-assets", currentParagraphBgColor);
            webView2Preview.CoreWebView2.NavigateToString(fullHtml);
        }

        // Markdown文字列を、プレビュー表示と出力(出力ボタン)の両方で共有するHTMLドキュメントに変換する。
        // imageBaseUrl : 画像参照の解決先。プレビュー時は仮想ホストURL("https://wiki-assets")、
        //                出力時は出力先フォルダからの相対パス("assets")などを渡す。
        // bgColor : このページの背景色("#rrggbb")。null/空の場合はCommonPreviewCssの既定色(白)のまま。
        // 今後CSSやHTMLテンプレートに手を加えたくなった場合も、この1箇所を変更するだけで
        // プレビュー・出力の両方に反映される（拡張性を考慮した共通化）。
        private string BuildHtmlDocument(string markdownText, string imageBaseUrl, string bgColor = null)
        {
            string htmlBody = Markdown.ToHtml(markdownText, markdownPipeline);
            // assetsDir のパス区切りをURL向けに変換してから、いったん仮想ホスト表記に統一する
            // （ローカルの絶対パスで画像が参照されているケースへの後方互換）
            htmlBody = htmlBody.Replace(assetsDir.Replace("\\", "/"), "https://wiki-assets");
            // 呼び出し元が指定した参照先（プレビュー用/出力用）に置き換える
            if (imageBaseUrl != "https://wiki-assets")
                htmlBody = htmlBody.Replace("https://wiki-assets", imageBaseUrl);

            // ページ個別の背景色が設定されていれば、bodyタグへのインラインスタイルとして適用する
            // （CommonPreviewCss側の既定の背景色指定より、インラインスタイルの方が優先される）
            string bodyStyle = string.IsNullOrEmpty(bgColor) ? "" : $" style=\"background-color:{bgColor};\"";

            // 画像がウィンドウ幅に収まるよう自動縮小するCSSを共通で適用する
            return $@"<html><head><meta charset=""utf-8""><style>{CommonPreviewCss}</style></head><body{bodyStyle}>{htmlBody}</body></html>";
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

        // 現在の選択範囲を含む行（複数行選択時はすべての行）の先頭に、prefixGenerator が返す文字列を
        // 追加する。ただし、その行の先頭が既に existingPattern にマッチする場合（＝既に同じ書式が
        // 付いている場合）は、追加する代わりにマッチした部分を取り除く（トグル動作）。
        // 見出し・箇条書き・番号付きリスト・チェックリスト・引用など、行頭に記号を付ける系のボタンで共通利用する。
        // prefixGenerator : 「選択範囲内での行番号(0始まり)」を受け取り、追加すべき先頭文字列を返す
        //   （番号付きリストの連番などに使う）。
        // existingPattern : その行の先頭が「既にこの書式である」と判定するための正規表現。
        private void InsertPrefixOnSelectedLines(Func<int, string> prefixGenerator, Regex existingPattern)
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
                string line = lines[i].TrimEnd('\r');
                Match existingMatch = existingPattern.Match(line);

                if (existingMatch.Success && existingMatch.Index == 0)
                {
                    // 既に同じ書式が行頭に付いているので、その部分を取り除く（トグルオフ）
                    sb.Append(line.Substring(existingMatch.Length));
                }
                else
                {
                    // まだ付いていないので追加する（トグルオン）
                    sb.Append(prefixGenerator(i));
                    sb.Append(line);
                }
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
        //
        // トグル動作: 選択範囲が既にこの書式で囲まれている場合は、追加する代わりに囲みを取り除く。
        // 「囲まれている」は次の2パターンを判定する。
        //   (1) 選択範囲そのものが prefix...suffix の形になっている（マーカーごと選択した場合）
        //   (2) 選択範囲の直前・直後にちょうど prefix・suffix が隣接している（中身だけ選択した場合）
        private void WrapSelection(string prefix, string suffix, string placeholder)
        {
            int start = txtEditor.SelectionStart;
            int length = txtEditor.SelectionLength;

            if (length > 0)
            {
                string selected = txtEditor.SelectedText;

                // パターン(1): 選択範囲そのものが "prefix...suffix" になっている
                if (selected.Length >= prefix.Length + suffix.Length &&
                    selected.StartsWith(prefix, StringComparison.Ordinal) &&
                    selected.EndsWith(suffix, StringComparison.Ordinal))
                {
                    string unwrapped = selected.Substring(prefix.Length, selected.Length - prefix.Length - suffix.Length);
                    txtEditor.SelectedText = unwrapped;
                    txtEditor.SelectionStart = start;
                    txtEditor.SelectionLength = unwrapped.Length;
                    txtEditor.Focus();
                    return;
                }

                // パターン(2): 選択範囲の直前・直後にちょうど prefix・suffix が隣接している
                string fullText = txtEditor.Text;
                int prefixStart = start - prefix.Length;
                int suffixStart = start + length;
                if (prefixStart >= 0 && suffixStart + suffix.Length <= fullText.Length &&
                    fullText.Substring(prefixStart, prefix.Length) == prefix &&
                    fullText.Substring(suffixStart, suffix.Length) == suffix)
                {
                    txtEditor.Text = fullText.Substring(0, prefixStart) + selected + fullText.Substring(suffixStart + suffix.Length);
                    txtEditor.SelectionStart = prefixStart;
                    txtEditor.SelectionLength = selected.Length;
                    txtEditor.Focus();
                    return;
                }
            }

            // どちらのパターンにも当てはまらない場合は、通常どおり囲む（トグルオン）
            string toWrap = length > 0 ? txtEditor.SelectedText : placeholder;
            txtEditor.SelectedText = prefix + toWrap + suffix;

            txtEditor.SelectionStart = start + prefix.Length;
            txtEditor.SelectionLength = toWrap.Length;
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

        // 「サイドメニューBG色」ボタン左クリック時：カラーピッカーで色を選び、現在選択中のタブの
        // サイドメニュー背景色として設定する（タブフォルダ内の .tabmeta.cfg に保存される）。
        private void SetTabSidebarBgColorInteractive()
        {
            if (tabControlMain.SelectedTab == null) return;
            string tabFolder = tabControlMain.SelectedTab.Tag?.ToString();
            if (string.IsNullOrEmpty(tabFolder)) return;

            using (var dialog = new ColorDialog())
            {
                // 既に設定済みの色があれば、カラーピッカーの初期選択色にしておく
                string currentColor = GetTabSidebarBgColor(tabFolder);
                if (!string.IsNullOrEmpty(currentColor))
                {
                    try { dialog.Color = ColorTranslator.FromHtml(currentColor); } catch { /* 壊れた値は無視 */ }
                }

                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                string colorHex = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
                SetTabSidebarBgColor(tabFolder, colorHex);
                hasUnsavedChanges = true;
                ApplyCurrentTabSidebarColor();
            }
        }

        // 「サイドメニューBG色」ボタン右クリック時：現在選択中のタブの背景色設定を削除し、既定色に戻す
        private void ResetTabSidebarBgColor()
        {
            if (tabControlMain.SelectedTab == null) return;
            string tabFolder = tabControlMain.SelectedTab.Tag?.ToString();
            if (string.IsNullOrEmpty(tabFolder)) return;

            SetTabSidebarBgColor(tabFolder, null);
            hasUnsavedChanges = true;
            ApplyCurrentTabSidebarColor();
        }

        // 「サイドメニュー文字色」ボタン左クリック時：カラーピッカーで色を選び、現在選択中のタブの
        // サイドメニュー文字色として設定する。背景色によっては文字が読めなくなることがあるため、
        // 背景色とは別に文字色も調整できるようにしている。
        private void SetTabSidebarTextColorInteractive()
        {
            if (tabControlMain.SelectedTab == null) return;
            string tabFolder = tabControlMain.SelectedTab.Tag?.ToString();
            if (string.IsNullOrEmpty(tabFolder)) return;

            using (var dialog = new ColorDialog())
            {
                string currentColor = GetTabSidebarTextColor(tabFolder);
                if (!string.IsNullOrEmpty(currentColor))
                {
                    try { dialog.Color = ColorTranslator.FromHtml(currentColor); } catch { /* 壊れた値は無視 */ }
                }

                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                string colorHex = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
                SetTabSidebarTextColor(tabFolder, colorHex);
                hasUnsavedChanges = true;
                ApplyCurrentTabSidebarColor();
            }
        }

        // 「サイドメニュー文字色」ボタン右クリック時：現在選択中のタブの文字色設定を削除し、既定色に戻す
        private void ResetTabSidebarTextColor()
        {
            if (tabControlMain.SelectedTab == null) return;
            string tabFolder = tabControlMain.SelectedTab.Tag?.ToString();
            if (string.IsNullOrEmpty(tabFolder)) return;

            SetTabSidebarTextColor(tabFolder, null);
            hasUnsavedChanges = true;
            ApplyCurrentTabSidebarColor();
        }

        // 「メインページBG色」ボタン左クリック時：カラーピッカーで色を選び、現在編集中の段落の
        // メインページ背景色として設定する（.mdファイル先頭の隠しマーカーとして保存される）。
        private void SetParagraphMainBgColorInteractive()
        {
            if (string.IsNullOrEmpty(currentFilePath)) return;

            using (var dialog = new ColorDialog())
            {
                // 既に設定済みの色があれば、カラーピッカーの初期選択色にしておく
                if (!string.IsNullOrEmpty(currentParagraphBgColor))
                {
                    try { dialog.Color = ColorTranslator.FromHtml(currentParagraphBgColor); } catch { /* 壊れた値は無視 */ }
                }

                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                currentParagraphBgColor = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
                hasUnsavedChanges = true;
                // マーカー行を含めてすぐにファイルへ反映しておく
                SaveCurrentFile();
                UpdatePreview();
            }
        }

        // 「メインページBG色」ボタン右クリック時：現在編集中の段落の背景色設定を削除し、既定色に戻す
        private void ResetParagraphMainBgColor()
        {
            if (string.IsNullOrEmpty(currentFilePath)) return;

            currentParagraphBgColor = null;
            hasUnsavedChanges = true;
            SaveCurrentFile();
            UpdatePreview();
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
            // タブの追加・削除・並び替え（EnsureTopTabIsFirstのRemoveAt+Insertなど）を行った直後は、
            // 変更前のタブ数を前提にした描画メッセージが遅れて届くことがある。
            // その場合 e.Index が現在のタブ数に対して範囲外になり得るため、範囲チェックして無視する
            // （チェックせずにアクセスすると ArgumentOutOfRangeException で落ちる）。
            if (e.Index < 0 || e.Index >= tabControlMain.TabPages.Count) return;

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

        // 大文字小文字だけが異なる名前への変更かどうかを判定する。
        // Windowsのファイルシステムは既定で大文字小文字を区別しないため、例えば "top" → "TOP" の
        // ような変更は Directory.Exists/File.Exists が「自分自身」を「別の既存項目」と誤認してしまい、
        // 「同じ名前が既に存在する」と誤って弾かれてしまう。この誤判定を避けるための判定に使う。
        private bool IsCaseOnlyNameChange(string oldName, string newName)
        {
            return !string.IsNullOrEmpty(oldName) && oldName != newName &&
                string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase);
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

                // 大文字小文字だけの変更(例: "top"→"TOP")の場合、newFolderは実際には
                // oldFolderと同一のフォルダを指しているため、Directory.Existsによる重複チェックは
                // 行わず、大文字小文字の変更として素通しする
                bool isCaseOnlyRename = IsCaseOnlyNameChange(page.Text, newTabName);

                if (!isCaseOnlyRename && Directory.Exists(newFolder))
                {
                    MessageBox.Show("同じ名前のタブが既に存在します。");
                }
                else if (!string.IsNullOrEmpty(oldFolder) && Directory.Exists(oldFolder))
                {
                    // 編集中のファイルがこのタブ内にあった場合、リネーム後のパスに追従させる
                    bool wasEditingHere = !string.IsNullOrEmpty(currentFilePath) &&
                        currentFilePath.StartsWith(oldFolder, StringComparison.OrdinalIgnoreCase);
                    string fileNameOnly = wasEditingHere ? Path.GetFileName(currentFilePath) : null;

                    if (isCaseOnlyRename)
                    {
                        // Directory.Move(oldFolder, newFolder) を直接呼ぶと、大文字小文字違いだけの
                        // パスは「同一パスへの移動」とみなされて失敗する場合があるため、
                        // 一時的な別名を経由して2段階でリネームする
                        string tempFolder = oldFolder + "_rename_" + Guid.NewGuid().ToString("N");
                        Directory.Move(oldFolder, tempFolder);
                        Directory.Move(tempFolder, newFolder);
                    }
                    else
                    {
                        Directory.Move(oldFolder, newFolder);
                    }

                    page.Text = newTabName;
                    page.Tag = newFolder;
                    hasUnsavedChanges = true;

                    if (wasEditingHere) currentFilePath = Path.Combine(newFolder, fileNameOnly);

                    // 他のタブを「TOP」にリネームした場合に備え、先頭配置のルールを再適用する
                    EnsureTopTabIsFirst();
                }
            }

            // --- 段落名称（＝.mdファイル名）の変更 ---
            string newFileDisplayName = SanitizeName(txtSidebarName.Text);
            string currentFolder = page.Tag?.ToString();
            if (!string.IsNullOrEmpty(newFileDisplayName) && lstSidebar.SelectedItem != null &&
                newFileDisplayName != lstSidebar.SelectedItem.ToString() && !string.IsNullOrEmpty(currentFolder))
            {
                string oldDisplayName = lstSidebar.SelectedItem.ToString();
                string oldFile = Path.Combine(currentFolder, oldDisplayName + ".md");
                string newFile = Path.Combine(currentFolder, newFileDisplayName + ".md");

                // タブ名と同様、大文字小文字だけの変更は「自分自身」なので重複扱いにしない
                bool isCaseOnlyRename = IsCaseOnlyNameChange(oldDisplayName, newFileDisplayName);

                if (!isCaseOnlyRename && File.Exists(newFile))
                {
                    MessageBox.Show("同じ名前の段落が既に存在します。");
                }
                else if (File.Exists(oldFile))
                {
                    // リネーム前に、エディタの最新内容を確実に保存しておく
                    SaveCurrentFile();

                    if (isCaseOnlyRename)
                    {
                        // File.Move(oldFile, newFile) を直接呼ぶと、大文字小文字違いだけの
                        // パスは「同一パスへの移動」とみなされて失敗する場合があるため、
                        // 一時的な別名を経由して2段階でリネームする
                        string tempFile = oldFile + "_rename_" + Guid.NewGuid().ToString("N");
                        File.Move(oldFile, tempFile);
                        File.Move(tempFile, newFile);
                    }
                    else
                    {
                        File.Move(oldFile, newFile);
                    }

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

                // 新規タブに「TOP」という名前を付けた場合に備え、先頭配置のルールを適用する
                EnsureTopTabIsFirst();
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

                    // 出力時は画像参照を、出力フォルダから見た相対パス（assets/xxx）に差し替える。
                    // 背景色も現在の段落に設定されているメインページ背景色を反映する。
                    string html = BuildHtmlDocument(txtEditor.Text, "assets", currentParagraphBgColor);
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

        // 「サイト出力」：全タブ・全段落をリンクでつないだ、PukiWiki風の静的HTMLサイトとして書き出す。
        //
        // 【全体の考え方】
        // ・段落(＝アプリ内の1つの.mdファイル)ごとに、個別のHTMLファイルを1つ生成する。
        //   サイドバーの段落リンクをクリックすると、このファイル単位で実際にページ遷移する
        //   （PukiWikiの各ページが個別URLを持つのと同じイメージ）。
        // ・一方、タブの切り替えはページ遷移を伴わない。生成する全ページに「全タブ分のタブボタン」と
        //   「全タブ分のサイドバー(段落リンク一覧)」をあらかじめ埋め込んでおき、タブボタンを押すと
        //   JavaScriptで「どのタブのサイドバーを表示するか」を切り替えるだけにする（詳細は
        //   BuildSitePageHtml のコメントを参照）。
        // ・「TOP」は特別な予約タブ名として扱う。TOPタブの最初の段落を、サイト全体の入口となる
        //   ルート直下の index.html として複製する（ブラウザで出力先フォルダを開いたときに
        //   最初に表示されるページになる）。
        private void ExportSiteToFolder()
        {
            try
            {
                // 出力対象の内容が最新であることを保証するため、出力前に編集中の内容を保存しておく
                SaveCurrentFile();

                // --- 事前チェック：サイトの入口となる「TOP」タブが存在し、中身が空でないこと ---
                // 「TOP」は名前で判定する予約タブ名。存在しない・空の場合はここで打ち切り、
                // 中途半端な（index.htmlが無い）サイトが生成されるのを防ぐ。
                TabPage topTab = null;
                foreach (TabPage page in tabControlMain.TabPages)
                {
                    if (page.Text == "TOP") { topTab = page; break; }
                }
                if (topTab == null)
                {
                    MessageBox.Show("サイト出力には「TOP」という名前のタブが必要です。\n先にTOPタブを作成し、段落を1つ以上追加してください。");
                    return;
                }
                string topFolder = topTab.Tag?.ToString();
                string[] topFiles = string.IsNullOrEmpty(topFolder) ? Array.Empty<string>() : Directory.GetFiles(topFolder, "*.md");
                if (topFiles.Length == 0)
                {
                    MessageBox.Show("「TOP」タブに段落が1つもありません。サイト出力には最低1つの段落が必要です。");
                    return;
                }

                // 複数ファイル・複数フォルダを書き出すため、単一ファイル用の SaveFileDialog ではなく
                // フォルダを選ばせる FolderBrowserDialog を使う
                using (var dialog = new FolderBrowserDialog { Description = "サイトの出力先フォルダを選択してください。" })
                {
                    if (dialog.ShowDialog(this) != DialogResult.OK) return;
                    string outputRoot = dialog.SelectedPath;

                    // --- 事前準備：全タブ・全段落の情報を1回だけ集めておく ---
                    // BuildSitePageHtml では「タブバー」と「全タブ分のサイドバー」を毎ページ生成するために
                    // 全タブ・全段落の一覧が必要になる。ページ生成のたびにファイルシステムを毎回
                    // 読み直すと非効率かつタイミングによって内容がずれる可能性があるため、
                    // このタイミングで一度だけ収集し、以降は同じ一覧(allTabs)を使い回す。
                    var allTabs = new List<(string TabName, string FolderPath, List<string> Paragraphs)>();
                    foreach (TabPage page in tabControlMain.TabPages)
                    {
                        string folder = page.Tag?.ToString();
                        if (string.IsNullOrEmpty(folder)) continue;

                        var paragraphNames = new List<string>();
                        foreach (string file in Directory.GetFiles(folder, "*.md"))
                            paragraphNames.Add(Path.GetFileNameWithoutExtension(file));

                        allTabs.Add((page.Text, folder, paragraphNames));
                    }

                    // --- 全タブ・全段落分のページを生成する ---
                    // 出力先は "<出力先フォルダ>\<タブ名>\<段落名>.html"。
                    // これらのファイルはサイトのルート(出力先フォルダ直下)から見て1段深い場所に
                    // あるため、リンク・画像パスの相対参照には "../" を付ける必要がある
                    // （BuildSitePageHtml の relativePrefix 引数として渡す）。
                    foreach (var tab in allTabs)
                    {
                        string tabOutputDir = Path.Combine(outputRoot, tab.TabName);
                        Directory.CreateDirectory(tabOutputDir);

                        foreach (string paragraphName in tab.Paragraphs)
                        {
                            string mdPath = Path.Combine(tab.FolderPath, paragraphName + ".md");
                            // 通常はファイルが存在するはずだが、念のため存在チェックしておく
                            string markdownText = File.Exists(mdPath) ? File.ReadAllText(mdPath) : "";
                            string html = BuildSitePageHtml(tab.TabName, paragraphName, markdownText, "../", allTabs);
                            File.WriteAllText(Path.Combine(tabOutputDir, paragraphName + ".html"), html);
                        }
                    }

                    // --- アセット(画像)のコピー ---
                    // タブ名フォルダの内部からもルート直下からも参照できるよう、assetsは
                    // 出力先フォルダの直下に1箇所だけ配置し、各ページからは相対パスで参照させる
                    // （BuildSitePageHtml内で "<relativePrefix>assets/xxx" という形に置換している）。
                    string outputAssetsDir = Path.Combine(outputRoot, "assets");
                    Directory.CreateDirectory(outputAssetsDir);
                    foreach (string srcFile in Directory.GetFiles(assetsDir))
                        File.Copy(srcFile, Path.Combine(outputAssetsDir, Path.GetFileName(srcFile)), true);

                    // --- サイトの入口(index.html)を生成する ---
                    // 「TOP」タブの最初の段落（Directory.GetFilesの列挙順で先頭に来たもの。通常は
                    // "1.〇〇.md" が採番ルール上先頭になる）を、ルート直下の index.html として
                    // もう一度生成する。中身は "TOP\<同じ段落名>.html" と実質同じだが、
                    // 出力位置がルート直下(1段浅い)になるため、relativePrefixを "" にして
                    // 別途生成し直している（単純なファイルコピーだと相対パスがずれてしまうため）。
                    string topFirstParagraph = Path.GetFileNameWithoutExtension(topFiles[0]);
                    string topMarkdown = File.ReadAllText(topFiles[0]);
                    string indexHtml = BuildSitePageHtml("TOP", topFirstParagraph, topMarkdown, "", allTabs);
                    string indexFilePath = Path.Combine(outputRoot, "index.html");
                    File.WriteAllText(indexFilePath, indexHtml);

                    MessageBox.Show("サイトを出力しました。");
                    // 既定のブラウザでindex.htmlを開き、その場でタブ切替・ページ遷移の
                    // つながりを確認できるようにする
                    Process.Start(new ProcessStartInfo(indexFilePath) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                // フォルダアクセス権限が無い・ディスク容量不足など、書き出し中の失敗をまとめて捕捉する
                MessageBox.Show("サイト出力エラー: " + ex.Message);
            }
        }

        // サイト出力用の1ページ分のHTMLを組み立てる。1つの段落(.mdファイル)につき、この関数を1回呼び出す。
        //
        // 引数の意味:
        // currentTabName/currentParagraphName : このページ自身が属するタブ名・段落名。
        //   このページを開いた瞬間にどのタブのサイドバーを最初から表示しておくか（isCurrentTab）、
        //   サイドバー中のどのリンクを「現在地」として太字ハイライトするか（isCurrentParagraph）の
        //   判定に使う。JSを使わずサーバー（ここではC#側）でレンダリング時点で決めてしまうことで、
        //   ページを開いた瞬間に正しい状態で表示され、ちらつき（一瞬違うタブが見える等）が起きない。
        // markdownText : このページの本文(Markdown)。既存のプレビュー・単体HTML出力と同じ
        //   Markdigパイプライン(markdownPipeline)でHTML化する。
        // relativePrefix : サイトのルート(出力先フォルダ)から見た、このページの深さを表す接頭辞。
        //   ルート直下のindex.html用は""（そのまま "assets/xxx" 等で参照できる）、
        //   1段深い "<タブ名>\<段落名>.html" 用は"../"（"../assets/xxx"のように1段上がる必要がある）。
        //   サイドバー内のリンクにも、画像パスにも同じ接頭辞を使う。
        // allTabs : ExportSiteToFolder で事前に収集した、全タブ・全段落の一覧。
        //   このページの内容には関係ない「他のタブ・他の段落」の情報も含まれているが、これは
        //   全ページ共通で「タブバー(全タブ分のボタン)」と「サイドバー(全タブ分のリンク一覧)」を
        //   丸ごと埋め込む設計にしているため（タブボタンを押した瞬間にJSだけで切り替えるには、
        //   切替先のサイドバーの中身も最初からページ内に存在している必要があるため）。
        private string BuildSitePageHtml(string currentTabName, string currentParagraphName, string markdownText,
            string relativePrefix, List<(string TabName, string FolderPath, List<string> Paragraphs)> allTabs)
        {
            // 本文の先頭に埋め込まれている「メインページ背景色」の隠しマーカーを取り除き、
            // このページ自身の.content背景色として使う（マーカー自体は本文として表示させない）
            string mainBgColor = ExtractParagraphBgColor(markdownText, out string cleanedMarkdown);

            // 本文をHTML化する。既存のBuildHtmlDocument(プレビュー/単体HTML出力用)と同様に、
            // 一旦 "https://wiki-assets" という仮想ホスト表記に統一してから、このページの深さに
            // 応じた実際の相対パス(relativePrefix + "assets")に置き換える2段階の変換を行っている。
            string bodyHtml = Markdown.ToHtml(cleanedMarkdown, markdownPipeline);
            bodyHtml = bodyHtml.Replace(assetsDir.Replace("\\", "/"), "https://wiki-assets");
            bodyHtml = bodyHtml.Replace("https://wiki-assets", relativePrefix + "assets");

            string contentStyle = string.IsNullOrEmpty(mainBgColor) ? "" : $" style=\"background-color:{mainBgColor};\"";

            // --- タブバー(全タブ分のボタン)とサイドバー(全タブ分のリンク一覧)をまとめて組み立てる ---
            // どちらも allTabs を1回だけループして同時に構築する。
            StringBuilder tabBarHtml = new StringBuilder();
            StringBuilder sidebarHtml = new StringBuilder();
            foreach (var tab in allTabs)
            {
                // タブ名・段落名にはユーザーが自由な文字列を入力できるため、HTML特殊文字
                // （< > & " など）が含まれていてもタグ構造が壊れないよう必ずHtmlEncodeする
                string tabNameEncoded = WebUtility.HtmlEncode(tab.TabName);
                bool isCurrentTab = tab.TabName == currentTabName;

                // タブボタン: data-tab属性にタブ名を持たせておき、JS側(showTab)でこの値を見て
                // 対応するサイドバー(下記sidebar-list)を表示/非表示切り替える。
                // 自分自身が属するタブのボタンには、最初から"active"クラス（見た目の強調）を付けておく。
                //
                // data-first-page には、このタブの最初の段落ページへの相対リンクを持たせておく。
                // タブボタンをクリックした際、サイドバーの表示切り替えだけでなく、右側の本文も
                // そのタブの最初の段落ページに実際に遷移させる（サイドバーだけ切り替わって本文が
                // 変わらない、というちぐはぐな動きを防ぐため）。段落が1つも無いタブの場合は
                // 遷移先が無いため、data-first-pageは空にしておく（JS側でサイドバー切替のみ行う）。
                string firstPageHref = tab.Paragraphs.Count > 0
                    ? $"{relativePrefix}{Uri.EscapeDataString(tab.TabName)}/{Uri.EscapeDataString(tab.Paragraphs[0])}.html"
                    : "";
                tabBarHtml.Append(
                    $"<button type=\"button\" data-tab=\"{tabNameEncoded}\" data-first-page=\"{firstPageHref}\" class=\"tab-button{(isCurrentTab ? " active" : "")}\">{tabNameEncoded}</button>");

                // サイドバー: タブごとに1つの<div>(data-tab属性で紐付け)を作り、CSS側で
                // ".sidebar-list"は非表示、".sidebar-list.active"だけ表示、というルールにしている。
                // 自タブの<div>にだけ最初から"active"を付けることで、ページを開いた時点で
                // 正しいサイドバーが（JS実行を待たずに）表示された状態になる。
                // タブごとに設定された「サイドメニュー背景色・文字色」があれば、その<div>自体に
                // インラインスタイルで適用する（表示中のタブに応じて自然に色が変わる）。
                // リンク(<a>)側のCSSは color:inherit にしてあるため、この文字色指定がそのまま
                // リンクの見た目にも反映される（背景色によって文字が見えなくなるのを防ぐため）。
                string tabSidebarBgColor = GetTabSidebarBgColor(tab.FolderPath);
                string tabSidebarTextColor = GetTabSidebarTextColor(tab.FolderPath);
                string sidebarListStyle =
                    (string.IsNullOrEmpty(tabSidebarBgColor) ? "" : $"background-color:{tabSidebarBgColor};") +
                    (string.IsNullOrEmpty(tabSidebarTextColor) ? "" : $"color:{tabSidebarTextColor};");
                if (!string.IsNullOrEmpty(sidebarListStyle)) sidebarListStyle = $" style=\"{sidebarListStyle}\"";
                sidebarHtml.Append($"<div class=\"sidebar-list{(isCurrentTab ? " active" : "")}\" data-tab=\"{tabNameEncoded}\"{sidebarListStyle}>");
                foreach (string paragraphName in tab.Paragraphs)
                {
                    string paragraphEncoded = WebUtility.HtmlEncode(paragraphName);
                    // 「今まさに表示しているこのページ自身」へのリンクだけ、現在地として太字表示する
                    bool isCurrentParagraph = isCurrentTab && paragraphName == currentParagraphName;
                    // タブ名・段落名には日本語や記号が含まれ得るため、URLとして安全な形に
                    // パーセントエンコードする（実ファイル名自体は元の文字列のままで、
                    // ブラウザがリンク解決時に自動でデコードして正しいファイルを開く）
                    string href = $"{relativePrefix}{Uri.EscapeDataString(tab.TabName)}/{Uri.EscapeDataString(paragraphName)}.html";
                    sidebarHtml.Append(
                        $"<a href=\"{href}\" class=\"{(isCurrentParagraph ? "current" : "")}\">{paragraphEncoded}</a>");
                }
                sidebarHtml.Append("</div>");
            }

            // 生成するHTMLドキュメント本体。
            // ・タブバー(tabBarHtml)とサイドバー(sidebarHtml)は上のループで既に組み立て済みで、
            //   「今どのタブを表示すべきか」もサーバー側(C#)で決めて active クラスを付与済みのため、
            //   ページを開いた瞬間に正しい見た目になる。
            // ・タブボタンをクリックした際は、そのタブの最初の段落ページへ実際にページ遷移する。
            //   これにより、サイドバーの表示切り替えだけでなく、右側の本文も正しく切り替わる
            //   （段落が1つも無いタブの場合のみ、遷移先が無いためサイドバー表示の切替だけを行う）。
            return $@"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8"">
<title>{WebUtility.HtmlEncode(currentParagraphName)}</title>
<style>{SiteCommonCss}</style>
</head>
<body>
<div class=""tabbar"">{tabBarHtml}</div>
<div class=""layout"">
<div class=""sidebar"">{sidebarHtml}</div>
<div class=""content""{contentStyle}>{bodyHtml}</div>
</div>
<script>
// 指定したタブ名に対応するサイドバー・タブボタンだけを active にし、他は非表示/非アクティブにする
// （段落が1つも無いタブをクリックした場合の、サイドバー表示切り替えのみのフォールバック用）
function showTab(name) {{
  document.querySelectorAll('.sidebar-list').forEach(function (el) {{ el.classList.toggle('active', el.getAttribute('data-tab') === name); }});
  document.querySelectorAll('.tab-button').forEach(function (el) {{ el.classList.toggle('active', el.getAttribute('data-tab') === name); }});
}}
// 各タブボタンのクリックで、そのタブの最初の段落ページへ実際に遷移する。
// これにより、サイドバーの表示だけでなく右側の本文も正しく切り替わる。
document.querySelectorAll('.tab-button').forEach(function (btn) {{
  btn.addEventListener('click', function () {{
    var firstPage = btn.getAttribute('data-first-page');
    if (firstPage) {{
      window.location.href = firstPage;
    }} else {{
      showTab(btn.getAttribute('data-tab'));
    }}
  }});
}});
</script>
</body>
</html>";
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

        // 現在のWikiProjectフォルダ全体（全タブ・全段落・assets）を1つの.spcファイル
        // （実体はZIP形式）に固めて保存する。「作業内容のセーブ」「作業内容の上書き保存」
        // 「Ctrl+S」の3箇所すべてから、引数を変えて共通で呼び出す。
        //
        // forceDialog : true の場合、既に保存先が分かっていても必ず保存先ダイアログを表示する
        //   （「作業内容のセーブ」用。常に保存先を選び直せるようにするため）。
        //   false の場合、currentWorkFilePath が既に分かっていればそこへそのまま上書きし、
        //   まだ無ければ（一度も保存・読み込みしていなければ）ダイアログを表示する
        //   （「作業内容の上書き保存」「Ctrl+S」用。既存の保存先へ無言で上書きする動作にするため）。
        // showCompletionMessage : 保存成功時に「作業内容を保存しました。」を表示するかどうか。
        //   Ctrl+S・上書き保存は毎回ポップアップされると煩わしいため、通常は false にする。
        //
        // 戻り値: 保存できた場合はtrue、ダイアログをキャンセルした場合や失敗した場合はfalse
        //   （「作業内容のロード」で、読み込み前に保存してから進める場合の判定に使う）。
        private bool SaveWork(bool forceDialog, bool showCompletionMessage)
        {
            // 保存前に、編集中の内容を確実にファイルへ反映しておく
            SaveCurrentFile();

            string targetPath = currentWorkFilePath;

            // 保存先ダイアログが必要なケース：毎回選ばせたい場合、または保存先がまだ分かっていない場合
            if (forceDialog || string.IsNullOrEmpty(targetPath))
            {
                using (var dialog = new SaveFileDialog { Filter = "作業内容ファイル (*.spc)|*.spc", FileName = "WikiProject.spc" })
                {
                    if (dialog.ShowDialog(this) != DialogResult.OK) return false;
                    targetPath = dialog.FileName;
                }
            }

            try
            {
                // 同名ファイルが既に存在するとZipFile.CreateFromDirectoryが失敗するため、先に削除する
                if (File.Exists(targetPath)) File.Delete(targetPath);
                ZipFile.CreateFromDirectory(currentProjectDir, targetPath);

                // 保存先を記憶しておくことで、次回以降の「上書き保存」やCtrl+Sで
                // 再度ダイアログを出さずに済むようにする。あわせてタイトルバーにも反映される
                // （hasUnsavedChangesのプロパティセッターがUpdateTitle()を自動で呼び出す）。
                currentWorkFilePath = targetPath;
                hasUnsavedChanges = false;

                if (showCompletionMessage) MessageBox.Show("作業内容を保存しました。");
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存エラー: " + ex.Message);
                return false;
            }
        }

        // Ctrl+S押下時：常に「上書き保存」として扱う（保存先が無ければダイアログ、あれば無言で上書き）
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.S)
            {
                // OSの警告音や、フォーカス中のコントロールへの'S'入力を防ぐ
                e.Handled = true;
                e.SuppressKeyPress = true;
                SaveWork(forceDialog: false, showCompletionMessage: false);
            }
        }

        // 未保存の変更がある場合に、これから行おうとしている操作の前に確認する共通処理。
        // 「作業内容のロード」「新規作成」など、今の作業内容を失う可能性のある操作の直前に呼び出す。
        // actionDescription には確認メッセージに埋め込む動作の説明を渡す（例:"別ファイルをロード"）。
        //
        // ダイアログの選択肢:
        //   はい     → そのまま操作を続行してよい（未保存の変更は破棄される）→ true を返す
        //   いいえ   → 保存先を選んで保存してから操作を続行する（保存に失敗・キャンセルした場合は
        //              操作自体も中止する）→ 保存できればtrue、できなければfalseを返す
        //   キャンセル → 何もせず操作を中止する → false を返す
        //
        // 戻り値がtrueの場合のみ、呼び出し元は本来行いたかった操作（読み込み・新規作成など）を続行する。
        private bool ConfirmDiscardUnsavedChanges(string actionDescription)
        {
            if (!hasUnsavedChanges) return true;

            DialogResult confirm = MessageBox.Show(this,
                $"現在の作業内容に未保存の内容があります。このまま{actionDescription}すると作業内容が失われますがよろしいですか？",
                "確認", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

            if (confirm == DialogResult.Cancel) return false;
            if (confirm == DialogResult.No)
            {
                // 保存先を選んで保存し、成功した場合のみ操作を続行してよいとする
                return SaveWork(forceDialog: true, showCompletionMessage: true);
            }
            // 「はい」の場合はそのまま続行してよい（現在の変更は破棄される）
            return true;
        }

        // 「作業内容のロード」：.spcファイルを読み込み、現在のWikiProjectの内容を完全に置き換える。
        // 未保存の変更がある場合は、先にファイル選択より前に確認する
        // （ファイルを選んだ後で確認すると、確認の意味が薄れてしまうため）。
        private void MenuLoadWork_Click(object sender, EventArgs e)
        {
            // 未保存の変更があれば、ファイルを選ぶ前にまず確認する
            if (!ConfirmDiscardUnsavedChanges("別ファイルをロード")) return;

            using (var openDialog = new OpenFileDialog { Filter = "作業内容ファイル (*.spc)|*.spc" })
            {
                if (openDialog.ShowDialog(this) != DialogResult.OK) return;

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

                    // 読み込んだファイルを「現在の作業内容ファイル」として記憶する
                    // （以後のCtrl+S・上書き保存はこのファイルへ上書きされ、タイトルバーにも反映される）
                    currentWorkFilePath = openDialog.FileName;
                    hasUnsavedChanges = false;
                    MessageBox.Show("作業内容を読み込みました。");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("読み込みエラー: " + ex.Message);
                }
            }
        }

        // プロジェクトの中身を「TOP」タブのみが存在する空の状態に作り直す共通処理。
        // ・「新規作成」メニュー押下時
        // ・通常のアプリ起動時（Excel/PowerPoint等の事務系アプリと同様、常に新規状態で
        //   開く仕様にしたため。Default.cfgを初めて作成した回＝移行直後の回を除く）
        // の両方から呼び出す。呼び出し元で例外処理・未保存確認を行う想定のため、ここでは行わない。
        private void ResetProjectToBlank()
        {
            // 削除予定のフォルダへ書き戻さないよう、先に参照をクリアしておく
            currentFilePath = "";
            // 新規状態なので、以前の保存先とは切り離す
            // （hasUnsavedChangesを更新する前にセットし、タイトルバーが正しく"Form1"に戻るようにする）
            currentWorkFilePath = null;

            // 現在のプロジェクトフォルダの中身を完全に削除してから、空のプロジェクトを作り直す
            if (Directory.Exists(currentProjectDir)) Directory.Delete(currentProjectDir, true);
            Directory.CreateDirectory(currentProjectDir);
            Directory.CreateDirectory(assetsDir);

            // 空のままだと編集できないタブになってしまうため、既存のタブ追加と同じルールで
            // 「TOP」タブとサンプル段落を1つ自動生成しておく
            string topFolder = Path.Combine(currentProjectDir, "TOP");
            Directory.CreateDirectory(topFolder);
            File.WriteAllText(Path.Combine(topFolder, "1.概要.md"), "# TOP\r\nここから仕様を書き始めます。");

            LoadTabsFromFolders();
        }

        // 「新規作成」：プロジェクトの中身を空っぽ（「TOP」タブのみ存在する状態）にリセットする。
        // 未保存の変更がある場合は、先に確認する。
        private void MenuNewProject_Click(object sender, EventArgs e)
        {
            // 未保存の変更があれば、リセットする前にまず確認する
            if (!ConfirmDiscardUnsavedChanges("新規作成")) return;

            try
            {
                ResetProjectToBlank();
                hasUnsavedChanges = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("新規作成エラー: " + ex.Message);
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
                lstSidebar.TopIndex = 0;
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
                lstSidebar.TopIndex = 0;
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