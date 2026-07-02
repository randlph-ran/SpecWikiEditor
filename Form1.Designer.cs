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
            components = new System.ComponentModel.Container();
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            menuSaveWork = new ToolStripMenuItem();
            menuLoadWork = new ToolStripMenuItem();
            menuLoadMdFile = new ToolStripMenuItem();
            menuExportHtml = new ToolStripMenuItem();
            menuExportSite = new ToolStripMenuItem();
            menuSettings = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            menuExit = new ToolStripMenuItem();
            splitContainer1 = new SplitContainer();
            splitContainer2 = new SplitContainer();
            pnlSidebarBottom = new Panel();
            btnAddFile = new Button();
            btnRemoveFile = new Button();
            lstSidebar = new ListBox();
            pnlEditorToolbar = new FlowLayoutPanel();
            btnHeading1 = new Button();
            btnHeading2 = new Button();
            btnBulletList = new Button();
            btnNumberedList = new Button();
            btnCheckList = new Button();
            btnQuote = new Button();
            btnHr = new Button();
            btnBold = new Button();
            btnUnderline = new Button();
            btnStrikethrough = new Button();
            btnInlineCode = new Button();
            btnCodeBlock = new Button();
            btnLink = new Button();
            btnTable = new Button();
            btnTextColor = new Button();
            cmbFontSize = new ComboBox();
            btnFindReplace = new Button();
            txtEditor = new TextBox();
            tableLayoutPanelRight = new TableLayoutPanel();
            pnlTopBar = new Panel();
            btnAddTab = new Button();
            tabControlMain = new TabControl();
            tabPage1 = new TabPage();
            tabPage2 = new TabPage();
            webView2Preview = new Microsoft.Web.WebView2.WinForms.WebView2();
            pnlBottomBar = new Panel();
            btnExport = new Button();
            toolTip1 = new ToolTip(components);
            menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer2).BeginInit();
            splitContainer2.Panel1.SuspendLayout();
            splitContainer2.Panel2.SuspendLayout();
            splitContainer2.SuspendLayout();
            pnlSidebarBottom.SuspendLayout();
            pnlEditorToolbar.SuspendLayout();
            tableLayoutPanelRight.SuspendLayout();
            pnlTopBar.SuspendLayout();
            tabControlMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)webView2Preview).BeginInit();
            pnlBottomBar.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem });
            menuStrip1.Dock = DockStyle.Top;
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(800, 24);
            menuStrip1.TabIndex = 1;
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { menuSaveWork, menuLoadWork, menuLoadMdFile, menuExportHtml, menuExportSite, menuSettings, toolStripSeparator1, menuExit });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(53, 20);
            fileToolStripMenuItem.Text = "ファイル";
            // 
            // menuSaveWork
            // 
            menuSaveWork.Name = "menuSaveWork";
            menuSaveWork.Size = new Size(32, 19);
            menuSaveWork.Text = "作業内容のセーブ";
            // 
            // menuLoadWork
            // 
            menuLoadWork.Name = "menuLoadWork";
            menuLoadWork.Size = new Size(32, 19);
            menuLoadWork.Text = "作業内容のロード";
            // 
            // menuLoadMdFile
            // 
            menuLoadMdFile.Name = "menuLoadMdFile";
            menuLoadMdFile.Size = new Size(32, 19);
            menuLoadMdFile.Text = "mdファイルのロード";
            // 
            // menuExportHtml
            // 
            menuExportHtml.Name = "menuExportHtml";
            menuExportHtml.Size = new Size(32, 19);
            menuExportHtml.Text = "HTML出力";
            //
            // menuExportSite
            // クリックすると Form1.ExportSiteToFolder() が呼ばれる。
            // 「HTML出力」(現在編集中の1ファイルのみ)とは別に、全タブ・全段落をリンクでつないだ
            // 静的HTMLサイト一式(タブごとのフォルダ＋段落ごとのHTML＋index.html)を書き出す機能。
            //
            menuExportSite.Name = "menuExportSite";
            menuExportSite.Size = new Size(32, 19);
            menuExportSite.Text = "サイト出力";
            //
            // menuSettings
            //
            menuSettings.Name = "menuSettings";
            menuSettings.Size = new Size(32, 19);
            menuSettings.Text = "設定";
            //
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(6, 6);
            // 
            // menuExit
            // 
            menuExit.Name = "menuExit";
            menuExit.Size = new Size(32, 19);
            menuExit.Text = "終了";
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
            // 段落リスト:エディタ:右ウィンドウ = 1:5:4 の比率(左側全体は6/10=480px)に戻す
            splitContainer1.SplitterDistance = 480;
            splitContainer1.TabIndex = 0;
            // 
            // splitContainer2
            // 
            splitContainer2.Dock = DockStyle.Fill;
            splitContainer2.Location = new Point(0, 0);
            splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            // Fillコントロールを先に追加し、Bottom等のドッキングコントロールを後に追加しないと
            // 正しく領域が確保されない（後から追加した方が手前に描画され、Fill側と重なって隠れてしまう）
            splitContainer2.Panel1.Controls.Add(lstSidebar);
            splitContainer2.Panel1.Controls.Add(pnlSidebarBottom);
            // 
            // splitContainer2.Panel2
            // 
            // Fillコントロールを先に追加し、Top等のドッキングコントロールを後に追加しないと
            // 正しく領域が確保されない（後から追加した方が手前に描画され、Fill側と重なって隠れてしまう）
            splitContainer2.Panel2.Controls.Add(txtEditor);
            splitContainer2.Panel2.Controls.Add(pnlEditorToolbar);
            splitContainer2.Size = new Size(480, 450);
            // 段落リストの幅は左側全体(480px)の1/6=80px
            splitContainer2.SplitterDistance = 80;
            splitContainer2.TabIndex = 0;
            // 
            // pnlSidebarBottom
            // 
            pnlSidebarBottom.Controls.Add(btnAddFile);
            pnlSidebarBottom.Controls.Add(btnRemoveFile);
            pnlSidebarBottom.Dock = DockStyle.Bottom;
            pnlSidebarBottom.Location = new Point(0, 422);
            pnlSidebarBottom.Margin = new Padding(0);
            pnlSidebarBottom.Name = "pnlSidebarBottom";
            pnlSidebarBottom.Size = new Size(80, 28);
            pnlSidebarBottom.TabIndex = 1;
            //
            // btnAddFile
            //
            btnAddFile.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnAddFile.Location = new Point(46, 3);
            btnAddFile.Name = "btnAddFile";
            btnAddFile.Size = new Size(28, 22);
            btnAddFile.TabIndex = 1;
            btnAddFile.Text = "+";
            btnAddFile.UseVisualStyleBackColor = true;
            //
            // btnRemoveFile
            //
            btnRemoveFile.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnRemoveFile.Location = new Point(16, 3);
            btnRemoveFile.Name = "btnRemoveFile";
            btnRemoveFile.Size = new Size(28, 22);
            btnRemoveFile.TabIndex = 0;
            btnRemoveFile.Text = "-";
            btnRemoveFile.UseVisualStyleBackColor = true;
            // 
            // lstSidebar
            // 
            lstSidebar.Dock = DockStyle.Fill;
            lstSidebar.FormattingEnabled = true;
            lstSidebar.Location = new Point(0, 0);
            lstSidebar.Name = "lstSidebar";
            lstSidebar.Size = new Size(80, 422);
            lstSidebar.TabIndex = 0;
            toolTip1.SetToolTip(lstSidebar, "段落");
            // 
            // pnlEditorToolbar
            // ボタンの折り返し行数はウィンドウ幅によって変わる（何行になってもよい）。
            // 高さはコード側(AdjustEditorToolbarHeight)で、実際に必要な行数分だけ動的に計算・設定する。
            //
            pnlEditorToolbar.WrapContents = true;
            pnlEditorToolbar.Controls.Add(btnHeading1);
            pnlEditorToolbar.Controls.Add(btnHeading2);
            pnlEditorToolbar.Controls.Add(btnBulletList);
            pnlEditorToolbar.Controls.Add(btnNumberedList);
            pnlEditorToolbar.Controls.Add(btnCheckList);
            pnlEditorToolbar.Controls.Add(btnQuote);
            pnlEditorToolbar.Controls.Add(btnHr);
            pnlEditorToolbar.Controls.Add(btnBold);
            pnlEditorToolbar.Controls.Add(btnUnderline);
            pnlEditorToolbar.Controls.Add(btnStrikethrough);
            pnlEditorToolbar.Controls.Add(btnInlineCode);
            pnlEditorToolbar.Controls.Add(btnCodeBlock);
            pnlEditorToolbar.Controls.Add(btnLink);
            pnlEditorToolbar.Controls.Add(btnTable);
            pnlEditorToolbar.Controls.Add(btnTextColor);
            pnlEditorToolbar.Controls.Add(cmbFontSize);
            pnlEditorToolbar.Controls.Add(btnFindReplace);
            pnlEditorToolbar.Dock = DockStyle.Top;
            pnlEditorToolbar.Location = new Point(0, 0);
            pnlEditorToolbar.Name = "pnlEditorToolbar";
            pnlEditorToolbar.Size = new Size(276, 96);
            pnlEditorToolbar.TabIndex = 1;
            // 
            // btnHeading1
            // 
            btnHeading1.AutoSize = true;
            btnHeading1.Location = new Point(2, 2);
            btnHeading1.Margin = new Padding(2);
            btnHeading1.Name = "btnHeading1";
            btnHeading1.Size = new Size(75, 25);
            btnHeading1.TabIndex = 0;
            btnHeading1.Text = "大項目";
            btnHeading1.UseVisualStyleBackColor = true;
            // 
            // btnHeading2
            // 
            btnHeading2.AutoSize = true;
            btnHeading2.Location = new Point(81, 2);
            btnHeading2.Margin = new Padding(2);
            btnHeading2.Name = "btnHeading2";
            btnHeading2.Size = new Size(75, 25);
            btnHeading2.TabIndex = 1;
            btnHeading2.Text = "中項目";
            btnHeading2.UseVisualStyleBackColor = true;
            // 
            // btnBulletList
            // 
            btnBulletList.AutoSize = true;
            btnBulletList.Location = new Point(160, 2);
            btnBulletList.Margin = new Padding(2);
            btnBulletList.Name = "btnBulletList";
            btnBulletList.Size = new Size(75, 25);
            btnBulletList.TabIndex = 2;
            btnBulletList.Text = "・リスト";
            btnBulletList.UseVisualStyleBackColor = true;
            // 
            // btnNumberedList
            // 
            btnNumberedList.AutoSize = true;
            btnNumberedList.Location = new Point(2, 31);
            btnNumberedList.Margin = new Padding(2);
            btnNumberedList.Name = "btnNumberedList";
            btnNumberedList.Size = new Size(75, 25);
            btnNumberedList.TabIndex = 3;
            btnNumberedList.Text = "1.リスト";
            btnNumberedList.UseVisualStyleBackColor = true;
            // 
            // btnCheckList
            // 
            btnCheckList.AutoSize = true;
            btnCheckList.Location = new Point(81, 31);
            btnCheckList.Margin = new Padding(2);
            btnCheckList.Name = "btnCheckList";
            btnCheckList.Size = new Size(75, 25);
            btnCheckList.TabIndex = 4;
            btnCheckList.Text = "チェックリスト";
            btnCheckList.UseVisualStyleBackColor = true;
            // 
            // btnQuote
            // 
            btnQuote.AutoSize = true;
            btnQuote.Location = new Point(160, 31);
            btnQuote.Margin = new Padding(2);
            btnQuote.Name = "btnQuote";
            btnQuote.Size = new Size(75, 25);
            btnQuote.TabIndex = 5;
            btnQuote.Text = "引用";
            btnQuote.UseVisualStyleBackColor = true;
            // 
            // btnHr
            // 
            btnHr.AutoSize = true;
            btnHr.Location = new Point(2, 60);
            btnHr.Margin = new Padding(2);
            btnHr.Name = "btnHr";
            btnHr.Size = new Size(75, 25);
            btnHr.TabIndex = 6;
            btnHr.Text = "水平線";
            btnHr.UseVisualStyleBackColor = true;
            // 
            // btnBold
            // 
            btnBold.AutoSize = true;
            btnBold.Location = new Point(81, 60);
            btnBold.Margin = new Padding(2);
            btnBold.Name = "btnBold";
            btnBold.Size = new Size(75, 25);
            btnBold.TabIndex = 7;
            btnBold.Text = "太字";
            btnBold.UseVisualStyleBackColor = true;
            // 
            // btnUnderline
            // 
            btnUnderline.AutoSize = true;
            btnUnderline.Location = new Point(160, 60);
            btnUnderline.Margin = new Padding(2);
            btnUnderline.Name = "btnUnderline";
            btnUnderline.Size = new Size(75, 25);
            btnUnderline.TabIndex = 8;
            btnUnderline.Text = "下線";
            btnUnderline.UseVisualStyleBackColor = true;
            // 
            // btnStrikethrough
            // 
            btnStrikethrough.AutoSize = true;
            btnStrikethrough.Location = new Point(2, 89);
            btnStrikethrough.Margin = new Padding(2);
            btnStrikethrough.Name = "btnStrikethrough";
            btnStrikethrough.Size = new Size(75, 25);
            btnStrikethrough.TabIndex = 9;
            btnStrikethrough.Text = "取消線";
            btnStrikethrough.UseVisualStyleBackColor = true;
            // 
            // btnInlineCode
            // 
            btnInlineCode.AutoSize = true;
            btnInlineCode.Location = new Point(81, 89);
            btnInlineCode.Margin = new Padding(2);
            btnInlineCode.Name = "btnInlineCode";
            btnInlineCode.Size = new Size(75, 25);
            btnInlineCode.TabIndex = 10;
            btnInlineCode.Text = "コード";
            btnInlineCode.UseVisualStyleBackColor = true;
            // 
            // btnCodeBlock
            // 
            btnCodeBlock.AutoSize = true;
            btnCodeBlock.Location = new Point(160, 89);
            btnCodeBlock.Margin = new Padding(2);
            btnCodeBlock.Name = "btnCodeBlock";
            btnCodeBlock.Size = new Size(75, 25);
            btnCodeBlock.TabIndex = 11;
            btnCodeBlock.Text = "コード塊";
            btnCodeBlock.UseVisualStyleBackColor = true;
            // 
            // btnLink
            // 
            btnLink.AutoSize = true;
            btnLink.Location = new Point(2, 118);
            btnLink.Margin = new Padding(2);
            btnLink.Name = "btnLink";
            btnLink.Size = new Size(75, 25);
            btnLink.TabIndex = 12;
            btnLink.Text = "リンク";
            btnLink.UseVisualStyleBackColor = true;
            // 
            // btnTable
            // 
            btnTable.AutoSize = true;
            btnTable.Location = new Point(81, 118);
            btnTable.Margin = new Padding(2);
            btnTable.Name = "btnTable";
            btnTable.Size = new Size(75, 25);
            btnTable.TabIndex = 13;
            btnTable.Text = "テーブル";
            btnTable.UseVisualStyleBackColor = true;
            // 
            // btnTextColor
            // 
            btnTextColor.AutoSize = true;
            btnTextColor.Location = new Point(160, 118);
            btnTextColor.Margin = new Padding(2);
            btnTextColor.Name = "btnTextColor";
            btnTextColor.Size = new Size(75, 25);
            btnTextColor.TabIndex = 14;
            btnTextColor.Text = "文字色";
            btnTextColor.UseVisualStyleBackColor = true;
            // 
            // cmbFontSize
            // 
            cmbFontSize.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFontSize.Location = new Point(2, 147);
            cmbFontSize.Margin = new Padding(2);
            cmbFontSize.Name = "cmbFontSize";
            cmbFontSize.Size = new Size(90, 23);
            cmbFontSize.TabIndex = 15;
            // 
            // btnFindReplace
            // 
            btnFindReplace.AutoSize = true;
            btnFindReplace.Location = new Point(96, 147);
            btnFindReplace.Margin = new Padding(2);
            btnFindReplace.Name = "btnFindReplace";
            btnFindReplace.Size = new Size(75, 25);
            btnFindReplace.TabIndex = 16;
            btnFindReplace.Text = "検索/置換";
            btnFindReplace.UseVisualStyleBackColor = true;
            // 
            // txtEditor
            // 
            txtEditor.AllowDrop = true;
            txtEditor.Dock = DockStyle.Fill;
            txtEditor.Location = new Point(0, 0);
            txtEditor.Multiline = true;
            txtEditor.Name = "txtEditor";
            txtEditor.ScrollBars = ScrollBars.Vertical;
            txtEditor.Size = new Size(276, 450);
            txtEditor.TabIndex = 0;
            toolTip1.SetToolTip(txtEditor, "Editor");
            // 
            // tableLayoutPanelRight
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
            tableLayoutPanelRight.Size = new Size(477, 450);
            tableLayoutPanelRight.TabIndex = 2;
            // 
            // pnlTopBar
            // 
            pnlTopBar.Controls.Add(btnAddTab);
            pnlTopBar.Dock = DockStyle.Fill;
            pnlTopBar.Location = new Point(0, 0);
            pnlTopBar.Margin = new Padding(0);
            pnlTopBar.Name = "pnlTopBar";
            pnlTopBar.Size = new Size(477, 32);
            pnlTopBar.TabIndex = 0;
            // 
            // btnAddTab
            // 
            btnAddTab.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnAddTab.Location = new Point(276, 4);
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
            tabControlMain.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControlMain.ItemSize = new Size(140, 28);
            tabControlMain.Location = new Point(0, 32);
            tabControlMain.Margin = new Padding(0);
            tabControlMain.Name = "tabControlMain";
            tabControlMain.SelectedIndex = 0;
            tabControlMain.Size = new Size(477, 100);
            tabControlMain.SizeMode = TabSizeMode.Fixed;
            tabControlMain.TabIndex = 1;
            toolTip1.SetToolTip(tabControlMain, "項目タブ");
            // 
            // tabPage1
            // 
            tabPage1.Location = new Point(4, 32);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(469, 64);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "tabPage1";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            tabPage2.Location = new Point(4, 32);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(602, 64);
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
            webView2Preview.Size = new Size(477, 282);
            webView2Preview.TabIndex = 2;
            toolTip1.SetToolTip(webView2Preview, "レビュー");
            webView2Preview.ZoomFactor = 1D;
            // 
            // pnlBottomBar
            // 
            pnlBottomBar.Controls.Add(btnExport);
            pnlBottomBar.Dock = DockStyle.Fill;
            pnlBottomBar.Location = new Point(0, 414);
            pnlBottomBar.Margin = new Padding(0);
            pnlBottomBar.Name = "pnlBottomBar";
            pnlBottomBar.Size = new Size(477, 36);
            pnlBottomBar.TabIndex = 3;
            // 
            // btnExport
            // 
            btnExport.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnExport.Location = new Point(226, 4);
            btnExport.Name = "btnExport";
            btnExport.Size = new Size(86, 28);
            btnExport.TabIndex = 0;
            btnExport.Text = "出力";
            btnExport.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            // Fill以外の(Top等の)ドッキングコントロールは、Fillコントロールより後に追加しないと
            // 正しく領域が確保されない（メニューが本体に重なって表示されてしまう）ため、この順序にしている
            Controls.Add(splitContainer1);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Name = "Form1";
            Text = "Form1";
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            splitContainer2.Panel1.ResumeLayout(false);
            splitContainer2.Panel2.ResumeLayout(false);
            splitContainer2.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer2).EndInit();
            splitContainer2.ResumeLayout(false);
            pnlSidebarBottom.ResumeLayout(false);
            pnlEditorToolbar.ResumeLayout(false);
            pnlEditorToolbar.PerformLayout();
            tableLayoutPanelRight.ResumeLayout(false);
            pnlTopBar.ResumeLayout(false);
            tabControlMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)webView2Preview).EndInit();
            pnlBottomBar.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
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
        private FlowLayoutPanel pnlEditorToolbar;
        private Button btnHeading1;
        private Button btnHeading2;
        private Button btnBulletList;
        private Button btnNumberedList;
        private Button btnCheckList;
        private Button btnQuote;
        private Button btnHr;
        private Button btnBold;
        private Button btnUnderline;
        private Button btnStrikethrough;
        private Button btnInlineCode;
        private Button btnCodeBlock;
        private Button btnLink;
        private Button btnTable;
        private Button btnTextColor;
        private ComboBox cmbFontSize;
        private Button btnFindReplace;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem menuSaveWork;
        private ToolStripMenuItem menuLoadWork;
        private ToolStripMenuItem menuLoadMdFile;
        private ToolStripMenuItem menuExportHtml;
        private ToolStripMenuItem menuExportSite;
        private ToolStripMenuItem menuSettings;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem menuExit;
    }
}
