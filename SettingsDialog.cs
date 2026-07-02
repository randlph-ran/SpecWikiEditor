#nullable disable
using System.Drawing;
using System.Windows.Forms;

namespace SpecWikiEditor
{
    // 「設定」メニューから開くダイアログ。Default.cfg の内容（保存先パス・フォルダ名・
    // 起動時の最大化）を画面上で編集できるようにする。変更内容は保存後、次回起動時から反映される。
    public class SettingsDialog : Form
    {
        private TextBox txtSavePath;
        private TextBox txtFolderName;
        private CheckBox chkMaximize;
        private Button btnOk;
        private Button btnCancel;

        public string SavePathResult => txtSavePath.Text.Trim();
        public string FolderNameResult => txtFolderName.Text.Trim();
        public bool MaximizeResult => chkMaximize.Checked;

        public SettingsDialog(AppConfig currentConfig)
        {
            Text = "設定";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(430, 168);

            Label lblSavePath = new Label { Text = "保存先パス:", Left = 12, Top = 16, Width = 80 };
            txtSavePath = new TextBox { Left = 96, Top = 12, Width = 236, Text = currentConfig.SavePath };
            Button btnBrowse = new Button { Text = "参照...", Left = 340, Top = 11, Width = 78 };
            btnBrowse.Click += (s, e) =>
            {
                // 現在のパスをそのまま初期選択位置にして、保存先フォルダを選ばせる
                using (var dialog = new FolderBrowserDialog { SelectedPath = txtSavePath.Text })
                {
                    if (dialog.ShowDialog(this) == DialogResult.OK) txtSavePath.Text = dialog.SelectedPath;
                }
            };

            Label lblFolderName = new Label { Text = "フォルダ名:", Left = 12, Top = 46, Width = 80 };
            txtFolderName = new TextBox { Left = 96, Top = 42, Width = 236, Text = currentConfig.FolderName };

            chkMaximize = new CheckBox
            {
                Text = "起動時にウィンドウを最大化する",
                Left = 12,
                Top = 78,
                Width = 320,
                Checked = currentConfig.MaximizeOnStartup
            };

            Label lblNote = new Label
            {
                Text = "※変更内容は、アプリを再起動すると反映されます。",
                Left = 12,
                Top = 108,
                Width = 406,
                ForeColor = SystemColors.GrayText
            };

            btnOk = new Button { Text = "OK", Left = 258, Width = 78, Top = 134, DialogResult = DialogResult.OK };
            btnCancel = new Button { Text = "キャンセル", Left = 340, Width = 78, Top = 134, DialogResult = DialogResult.Cancel };

            Controls.Add(lblSavePath);
            Controls.Add(txtSavePath);
            Controls.Add(btnBrowse);
            Controls.Add(lblFolderName);
            Controls.Add(txtFolderName);
            Controls.Add(chkMaximize);
            Controls.Add(lblNote);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);

            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }
    }
}
