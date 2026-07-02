#nullable disable
using System;
using System.Drawing;
using System.Windows.Forms;

namespace SpecWikiEditor
{
    // 検索・一括置換用のダイアログ。渡されたTextBox（txtEditor）に対して直接検索・置換を行う。
    // モードレスにはせず、開いている間は「次を検索」「すべて置換」を繰り返し使える簡易ツールとして提供する。
    public class FindReplaceDialog : Form
    {
        private readonly TextBox targetEditor;
        private TextBox txtFind;
        private TextBox txtReplace;
        private Button btnFindNext;
        private Button btnReplaceAll;
        private Button btnClose;

        public FindReplaceDialog(TextBox targetEditor)
        {
            this.targetEditor = targetEditor;

            Text = "検索・置換";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(340, 150);

            Label lblFind = new Label { Text = "検索文字列:", Left = 12, Top = 16, Width = 80 };
            txtFind = new TextBox { Left = 96, Top = 12, Width = 232 };

            Label lblReplace = new Label { Text = "置換文字列:", Left = 12, Top = 46, Width = 80 };
            txtReplace = new TextBox { Left = 96, Top = 42, Width = 232 };

            btnFindNext = new Button { Text = "次を検索", Left = 12, Width = 90, Top = 108 };
            btnReplaceAll = new Button { Text = "すべて置換", Left = 108, Width = 90, Top = 108 };
            btnClose = new Button { Text = "閉じる", Left = 238, Width = 90, Top = 108, DialogResult = DialogResult.Cancel };

            btnFindNext.Click += BtnFindNext_Click;
            btnReplaceAll.Click += BtnReplaceAll_Click;

            Controls.Add(lblFind);
            Controls.Add(txtFind);
            Controls.Add(lblReplace);
            Controls.Add(txtReplace);
            Controls.Add(btnFindNext);
            Controls.Add(btnReplaceAll);
            Controls.Add(btnClose);

            AcceptButton = btnFindNext;
            CancelButton = btnClose;

            Shown += (s, e) => txtFind.Focus();
        }

        // 現在のカーソル位置より後ろから検索文字列を探して選択状態にする。
        // 末尾まで見つからなければ先頭に戻って再検索する（循環検索）。
        private void BtnFindNext_Click(object sender, EventArgs e)
        {
            string keyword = txtFind.Text;
            if (string.IsNullOrEmpty(keyword)) return;

            string text = targetEditor.Text;
            int searchStart = targetEditor.SelectionStart + targetEditor.SelectionLength;

            int foundIndex = text.IndexOf(keyword, searchStart, StringComparison.Ordinal);
            if (foundIndex < 0)
                foundIndex = text.IndexOf(keyword, 0, StringComparison.Ordinal);

            if (foundIndex < 0)
            {
                MessageBox.Show(this, "見つかりませんでした。");
                return;
            }

            targetEditor.Focus();
            targetEditor.Select(foundIndex, keyword.Length);
            targetEditor.ScrollToCaret();
        }

        // 検索文字列に一致する箇所をすべて置換文字列に置き換える
        private void BtnReplaceAll_Click(object sender, EventArgs e)
        {
            string keyword = txtFind.Text;
            if (string.IsNullOrEmpty(keyword)) return;

            string text = targetEditor.Text;
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(keyword, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += keyword.Length;
            }

            if (count == 0)
            {
                MessageBox.Show(this, "見つかりませんでした。");
                return;
            }

            targetEditor.Text = text.Replace(keyword, txtReplace.Text ?? "");
            MessageBox.Show(this, $"{count} 件を置換しました。");
        }
    }
}
