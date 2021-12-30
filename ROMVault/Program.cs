using ROMVault.RVServices;
using RomVaultCore;
using System;
using System.Reflection;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ROMVault
{
    internal static class Program
    {
        private static readonly Version Version = Assembly.GetEntryAssembly().GetName().Version;
        private static readonly int VNow = Version.Build;
        public static readonly string StrVersion = Version.ToString(3);

        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            BasicHttpBinding b = new BasicHttpBinding
            {
                SendTimeout = new TimeSpan(0, 0, 10),
                ReceiveTimeout = new TimeSpan(0, 0, 10)
            };
            EndpointAddress e = new EndpointAddress(@"http://services.romvault.com/RVService.svc");
            RVServiceClient s = new RVServiceClient(b, e);

            if (string.IsNullOrEmpty(UISettings.Username) || string.IsNullOrEmpty(UISettings.EMail))
            {
                using (FrmRegistration fReg = new FrmRegistration())
                {
                    fReg.ShowDialog();
                }
            }

            Settings.rvSettings = new Settings();
            Settings.rvSettings = Settings.SetDefaults();

            ReportError.Username = UISettings.Username;
            ReportError.EMail = UISettings.EMail;
            ReportError.OptOut = UISettings.OptOut;
            ReportError.ErrorForm += ShowErrorForm;
            ReportError.Dialog += ShowDialog;

            try
            {
                if (!UISettings.OptOut)
                {
                    s.SendUserAsync(UISettings.Username, UISettings.EMail, VNow).Wait();
                    s.StartUpV2Async(Version.Major, Version.Minor, Version.Build).Wait();
                }

                ReportError.vMajor = Version.Major;
                ReportError.vMinor = Version.Minor;
                ReportError.vBuild = Version.Build;

                Task<bool> taskUpdateCheck = s.UpdateCheckAsync(Version.Major, Version.Minor, Version.Build);
                taskUpdateCheck.Wait();
                bool v = taskUpdateCheck.Result;

                if (v)
                {
                    Task<string> taskGetUpdateLink = s.GetUpdateLinkAsync();
                    taskGetUpdateLink.Wait();
                    string url = taskGetUpdateLink.Result;
                    MessageBox.Show("There is a new release download now from " + url);
                    //System.Diagnostics.Process.Start(url);
                    //s.Close();
                    //return;
                }
            }
            catch
            {
            }

#if !DEBUG
            Application.ThreadException += ReportError.UnhandledExceptionHandler;
#endif

            FrmSplashScreen progress = new FrmSplashScreen();
            progress.ShowDialog();

            progress.Dispose();

            Application.Run(new FrmMain());

            ReportError.Close();
        }

        public static void ShowErrorForm(string message)
        {
            FrmShowError fshow = new FrmShowError();
            fshow.settype(message);
            fshow.ShowDialog();
        }

        public static void ShowDialog(string text, string caption)
        {
            MessageBox.Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }
    }
}