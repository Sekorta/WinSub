using System;
using System.Net;
using System.Windows.Forms;

namespace SubsonicPlayer
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 |
                    (SecurityProtocolType)768 |
                    SecurityProtocolType.Tls;
            }
            catch
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
            }
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
