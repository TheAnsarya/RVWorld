using System;
using System.Windows.Forms;
using RVXCore;

namespace RomVaultX {
	public static class Program {

		[STAThread]
		private static void Main() {
			Settings.DBFileName = AppSettings.ReadSetting("DBFileName");
			Settings.DBMemCacheSize = AppSettings.ReadSetting("DBMemCacheSize");
			Settings.DBCheckOnStartUp = AppSettings.ReadSetting("DBCheckOnStartUp");
			Settings.ScanInMemorySize = AppSettings.ReadSetting("ScanInMemorySize");
			Settings.ScanInDir = AppSettings.ReadSetting("ScanInDir");


			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new frmMain());
		}
	}
}