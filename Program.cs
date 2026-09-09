using System;
using System.Net;
using System.Windows.Forms;

namespace WinSub
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.ThreadException += (s, e) =>
            {
                MessageBox.Show(e.Exception.ToString(), "WinSub Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Exception ex = e.ExceptionObject as Exception;
                MessageBox.Show(ex != null ? ex.ToString() : "Unknown error", "WinSub Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            TryEnableTls12();

            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        private static void TryEnableTls12()
        {
            try
            {
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 |
                    (SecurityProtocolType)768 |
                    SecurityProtocolType.Tls;
            }
            catch
            {
                try { ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls; }
                catch { }
            }
        }


    }
}
