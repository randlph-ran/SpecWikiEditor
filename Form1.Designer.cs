namespace SpecWikiEditor
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            // ToolTip等、コンテナ管理が必要なコンポーネントを保持するためのコンテナを生成する
            components = new System.ComponentModel.Container();
            splitContainer1 = new SplitContainer();
            tableLayoutPanelRight = new TableLayoutPanel();
            pnlTopBar = new Panel();
            btnAddTab = new Button();
            tabControlMain = new TabControl();
            tabPage1 = new TabPage();
            tabPage2 = new TabPage();
            webView2Preview = new Microsoft.Web.WebView2.WinForms.WebView2();
            pnlBottomBar = new Panel();
            btnExport = new Button();
            splitContainer2 = new SplitContainer();
            pnlSidebarBottom = new Panel();
            btnRemoveFile = new Button();
            btnAddFile = new Button();
            lstSidebar = new ListBox();
            txtEditor = new TextBox();
            toolTip1 = new ToolTip(components);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            tableLayoutPanelRight.SuspendLayout();
            pnlTopBar.SuspendLayout();
            tabControlMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)webView2Preview).BeginInit();
            pnlBottomBar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer2).BeginInit();
            splitContainer2.Panel1.SuspendLayout();
            splitContainer2.Panel2.SuspendLayout();
            splitContainer2.SuspendLayout();
            pnlSidebarBottom.SuspendLayout();
            SuspendLayout();
            //
            // splitContainer1
            //
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Name = "splitContainer1";
            //
            // splitContainer1.Panel1
            //
            splitContainer1.Panel1.Controls.Add(splitContainer2);
            //
            // splitContainer1.Panel2
            //
            splitContainer1.Panel2.Controls.Add(tableLayoutPanelRight);
            splitContainer1.Size = new Size(800, 450);
            splitContainer1.SplitterDistance = 186;
            splitContainer1.TabIndex = 0;
            //
            // tableLayoutPanelRight
            // 右側パネル全体を「タブ追加バー」「タブ一覧」「プレビュー」「出力バー」の4段に分割するレイアウト。
            // WebView2はネイティブウィンドウ描画のため、ボタンを重ねて配置せずこのように専用の領域を分けている。
            //
            tableLayoutPanelRight.ColumnCount = 1;
            tableLayoutPanelRight.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanelRight.Controls.Add(pnlTopBar, 0, 0);
            tableLayoutPanelRight.Controls.Add(tabControlMain, 0, 1);
            tableLayoutPanelRight.Controls.Add(webView2Preview, 0, 2);
            tableLayoutPanelRight.Controls.Add(pnlBottomBar, 0, 3);
            tableLayoutPanelRight.Dock = DockStyle.Fill;
            tableLayoutPanelRight.Location = new Point(0, 0);
            tableLayoutPanelRight.Name = "tableLayoutPanelRight";
            tableLayoutPanelRight.RowCount = 4;
            tableLayoutPanelRight.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            tableLayoutPanelRight.RowStyles.Add(new RowStyle(SizeType.Absolute, 100F));
            tableLayoutPanelRight.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanelRight.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            tableLayoutPanelRight.Size = new Size(610, 450);
            tableLayoutPanelRight.TabIndex = 2;
            //
            // pnlTopBar
            // 新規タブ追加(+)ボタンを画面右上に配置するためのバー
            //
            pnlTopBar.Controls.Add(btnAddTab);
            pnlTopBar.Dock = DockStyle.Fill;
            pnlTopBar.Location = new Point(0, 0);
            pnlTopBar.Margin = new Padding(0);
            pnlTopBar.Name = "pnlTopBar";
            pnlTopBar.Size = new Size(610, 32);
            pnlTopBar.TabIndex = 0;
            //
            // btnAddTab
            // 新規タブ(=フォルダ)を追加するボタン。押下すると名前入力ダイアログを表示する。
            //
            btnAddTab.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnAddTab.Location = new Point(570, 4);
            btnAddTab.Name = "btnAddTab";
            btnAddTab.Size = new Size(36, 24);
            btnAddTab.TabIndex = 0;
            btnAddTab.Text = "+";
            btnAddTab.UseVisualStyleBackColor = true;
            //
            // tabControlMain
            //
            tabControlMain.Controls.Add(tabPage1);
            tabControlMain.Controls.Add(tabPage2);
            tabControlMain.Dock = DockStyle.Fill;
            tabControlMain.Location = new Point(0, 32);
            tabControlMain.Margin = new Padding(0);
            tabControlMain.Name = "tabControlMain";
            tabControlMain.SelectedIndex = 0;
            tabControlMain.Size = new Size(610, 100);
            tabControlMain.TabIndex = 1;
            // タブに「×」ボタンを自前描画するため、固定サイズのオーナードローに設定する
            tabControlMain.SizeMode = TabSizeMode.Fixed;
            tabControlMain.ItemSize = new Size(140, 28);
            tabControlMain.DrawMode = TabDrawMode.OwnerDrawFixed;
            toolTip1.SetToolTip(tabControlMain, "項目タブ");
            //
            // tabPage1
            //
            tabPage1.Location = new Point(4, 24);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(602, 72);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "tabPage1";
            tabPage1.UseVisualStyleBackColor = true;
            //
            // tabPage2
            //
            tabPage2.Location = new Point(4, 24);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(192, 72);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "tabPage2";
            tabPage2.UseVisualStyleBackColor = true;
            //
            // webView2Preview
            //
            webView2Preview.AllowExternalDrop = true;
            webView2Preview.CreationProperties = null;
            webView2Preview.DefaultBackgroundColor = Color.White;
            webView2Preview.Dock = DockStyle.Fill;
            webView2Preview.Location = new Point(0, 132);
            webView2Preview.Margin = new Padding(0);
            webView2Preview.Name = "webView2Preview";
            webView2Preview.Size = new Size(610, 282);
            webView2Preview.TabIndex = 2;
            webView2Preview.ZoomFactor = 1D;
            toolTip1.SetToolTip(webView2Preview, "レビュー");
            //
            // pnlBottomBar
            // 出力ボタンを画面右下に配置するためのバー
            //
            pnlBottomBar.Controls.Add(btnExport);
            pnlBottomBar.Dock = DockStyle.Fill;
            pnlBottomBar.Location = new Point(0, 414);
            pnlBottomBar.Margin = new Padding(0);
            pnlBottomBar.Name = "pnlBottomBar";
            pnlBottomBar.Size = new Size(610, 36);
            pnlBottomBar.TabIndex = 3;
            //
            // btnExport
            // 現在編集中のMarkdownをHTMLとして出力し、既定のブラウザで開くボタン。
            //
            btnExport.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnExport.Location = new Point(520, 4);
            btnExport.Name = "btnExport";
            btnExport.Size = new Size(86, 28);
            btnExport.TabIndex = 0;
            btnExport.Text = "出力";
            btnExport.UseVisualStyleBackColor = true;
            //
            // splitContainer2
            //
            splitContainer2.Dock = DockStyle.Fill;
            splitContainer2.Location = new Point(0, 0);
            splitContainer2.Name = "splitContainer2";
            //
            // splitContainer2.Panel1
            // 「-」「+」ボタン用のバーを先にDock.Bottomで追加し、ListBoxをDock.Fillで最後に追加する
            // （Dock配置は追加順に依存するため、外側→中央の順で追加するのが安全）
            //
            splitContainer2.Panel1.Controls.Add(pnlSidebarBottom);
            splitContainer2.Panel1.Controls.Add(lstSidebar);
            //
            // splitContainer2.Panel2
            //
            splitContainer2.Panel2.Controls.Add(txtEditor);
            splitContainer2.Size = new Size(186, 450);
            splitContainer2.SplitterDistance = 106;
            splitContainer2.TabIndex = 0;
            //
            // pnlSidebarBottom
            // 段落(サイドバー項目)の追加(+)・削除(-)ボタンを配置するバー
            //
            pnlSidebarBottom.Controls.Add(btnAddFile);
            pnlSidebarBottom.Controls.Add(btnRemoveFile);
            pnlSidebarBottom.Dock = DockStyle.Bottom;
            pnlSidebarBottom.Location = new Point(0, 422);
            pnlSidebarBottom.Margin = new Padding(0);
            pnlSidebarBottom.Name = "pnlSidebarBottom";
            pnlSidebarBottom.Size = new Size(106, 28);
            pnlSidebarBottom.TabIndex = 1;
            //
            // btnRemoveFile
            // 選択中の段落(.mdファイル)を削除するボタン
            //
            btnRemoveFile.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnRemoveFile.Location = new Point(42, 3);
            btnRemoveFile.Name = "btnRemoveFile";
            btnRemoveFile.Size = new Size(28, 22);
            btnRemoveFile.TabIndex = 0;
            btnRemoveFile.Text = "-";
            btnRemoveFile.UseVisualStyleBackColor = true;
            //
            // btnAddFile
            // 新規段落(.mdファイル)を追加するボタン
            //
            btnAddFile.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnAddFile.Location = new Point(72, 3);
            btnAddFile.Name = "btnAddFile";
            btnAddFile.Size = new Size(28, 22);
            btnAddFile.TabIndex = 1;
            btnAddFile.Text = "+";
            btnAddFile.UseVisualStyleBackColor = true;
            //
            // lstSidebar
            //
            lstSidebar.Dock = DockStyle.Fill;
            lstSidebar.FormattingEnabled = true;
            lstSidebar.Location = new Point(0, 0);
            lstSidebar.Name = "lstSidebar";
            lstSidebar.Size = new Size(106, 422);
            lstSidebar.TabIndex = 0;
            toolTip1.SetToolTip(lstSidebar, "段落");
            //
            // txtEditor
            //
            txtEditor.AllowDrop = true;
            txtEditor.Dock = DockStyle.Fill;
            txtEditor.Location = new Point(0, 0);
            txtEditor.Multiline = true;
            txtEditor.Name = "txtEditor";
            txtEditor.ScrollBars = ScrollBars.Vertical;
            txtEditor.Size = new Size(76, 450);
            txtEditor.TabIndex = 0;
            toolTip1.SetToolTip(txtEditor, "Editor");
            //
            // Form1
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(splitContainer1);
            Name = "Form1";
            Text = "Form1";
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            tableLayoutPanelRight.ResumeLayout(false);
            pnlTopBar.ResumeLayout(false);
            tabControlMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)webView2Preview).EndInit();
            pnlBottomBar.ResumeLayout(false);
            splitContainer2.Panel1.ResumeLayout(false);
            splitContainer2.Panel2.ResumeLayout(false);
            splitContainer2.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer2).EndInit();
            splitContainer2.ResumeLayout(false);
            pnlSidebarBottom.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private SplitContainer splitContainer1;
        private Microsoft.Web.WebView2.WinForms.WebView2 webView2Preview;
        private SplitContainer splitContainer2;
        private ListBox lstSidebar;
        private TextBox txtEditor;
        private TabControl tabControlMain;
        private TabPage tabPage1;
        private TabPage tabPage2;
        private TableLayoutPanel tableLayoutPanelRight;
        private Panel pnlTopBar;
        private Button btnAddTab;
        private Panel pnlBottomBar;
        private Button btnExport;
        private Panel pnlSidebarBottom;
        private Button btnRemoveFile;
        private Button btnAddFile;
        private ToolTip toolTip1;
    }
}
