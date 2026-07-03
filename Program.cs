using System.Threading;

namespace SpecWikiEditor
{
    internal static class Program
    {
        // アプリ全体で共有される、多重起動チェック用のMutex名。
        // このアプリはプロジェクトの保存先フォルダが1箇所に固定されており、起動のたびに
        // 内容を空(TOPのみ)にリセットする仕様のため、複数同時起動を許すと片方の起動が
        // もう片方の作業中の内容を消してしまう事故につながる。そのため単独起動に制限する。
        private const string SingleInstanceMutexName = "SpecWikiEditor_SingleInstance_Mutex";

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // 名前付きMutexを取得できるかどうかで、既に起動中のインスタンスがあるか判定する。
            // createdNewがfalseの場合、他のプロセスが既にこのMutexを保持している＝多重起動。
            using (Mutex singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out bool createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show(
                        "SpecWikiEditorは既に起動しています。\n多重起動はできません。",
                        "多重起動の防止", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // To customize application configuration such as set high DPI settings or default font,
                // see https://aka.ms/applicationconfiguration.
                ApplicationConfiguration.Initialize();
                Application.Run(new Form1());

                // Application.Run が終了(アプリ終了)するまでMutexを保持し続けるため、
                // usingブロックはここまで維持する必要がある
            }
        }
    }
}
