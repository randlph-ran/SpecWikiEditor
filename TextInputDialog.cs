#nullable disable
using System;
using System.Drawing;
using System.Windows.Forms;

namespace SpecWikiEditor
{
    // 1行のテキストを入力してもらうための汎用ダイアログ。
    // タブ名の入力に加え、今後「ファイル名の入力」「タブ名の変更」など
    // 似たような入力操作が必要になった場合にも使い回せるようにしている。
    public class TextInputDialog : Form
    {
        private TextBox txtInput;
        private Button btnOk;
        private Button btnCancel;

        // ユーザーが入力した文字列。呼び出し側は ShowDialog() の戻り値が
        // DialogResult.OK の場合にのみこの値を利用すること。
        public string InputText => txtInput.Text;

        public TextInputDialog(string title, string message, string defaultValue)
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(320, 110);

            Label lbl = new Label { Text = message, Left = 12, Top = 12, Width = 296, AutoSize = false };
            txtInput = new TextBox { Left = 12, Top = 36, Width = 296, Text = defaultValue };
            btnOk = new Button { Text = "OK", Left = 150, Width = 75, Top = 68, DialogResult = DialogResult.OK };
            btnCancel = new Button { Text = "キャンセル", Left = 233, Width = 75, Top = 68, DialogResult = DialogResult.Cancel };

            Controls.Add(lbl);
            Controls.Add(txtInput);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);

            // Enter/Escキーでそれぞれ OK/キャンセルとして扱う
            AcceptButton = btnOk;
            CancelButton = btnCancel;

            // 表示直後にテキストを全選択しておき、そのまま書き換えやすくする
            Shown += (s, e) => { txtInput.SelectAll(); txtInput.Focus(); };
        }
    }
}
