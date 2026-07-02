#nullable disable
using System;
using System.Drawing;
using System.Windows.Forms;

namespace SpecWikiEditor
{
    // リンク挿入用のダイアログ。「表示文字」と「URL」の2項目を入力してもらい、
    // Markdown形式のリンク記法 [表示文字](URL) を組み立てるために使用する。
    public class LinkInputDialog : Form
    {
        private TextBox txtDisplayText;
        private TextBox txtUrl;
        private Button btnOk;
        private Button btnCancel;

        public string DisplayText => txtDisplayText.Text;
        public string Url => txtUrl.Text;

        public LinkInputDialog(string defaultDisplayText)
        {
            Text = "リンクの挿入";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(340, 150);

            Label lblDisplay = new Label { Text = "表示文字:", Left = 12, Top = 16, Width = 80 };
            txtDisplayText = new TextBox { Left = 96, Top = 12, Width = 232, Text = defaultDisplayText };

            Label lblUrl = new Label { Text = "URL:", Left = 12, Top = 46, Width = 80 };
            txtUrl = new TextBox { Left = 96, Top = 42, Width = 232, Text = "https://" };

            btnOk = new Button { Text = "OK", Left = 170, Width = 75, Top = 108, DialogResult = DialogResult.OK };
            btnCancel = new Button { Text = "キャンセル", Left = 253, Width = 75, Top = 108, DialogResult = DialogResult.Cancel };

            Controls.Add(lblDisplay);
            Controls.Add(txtDisplayText);
            Controls.Add(lblUrl);
            Controls.Add(txtUrl);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            // 表示直後は表示文字欄にフォーカスし、全選択しておく
            Shown += (s, e) => { txtDisplayText.SelectAll(); txtDisplayText.Focus(); };
        }
    }
}
