using RomVaultCore.RvDB;
using System.Text;
using System.Windows.Forms;

namespace ROMVault
{
    public partial class FrmRomInfo : Form
    {
        public FrmRomInfo()
        {
            InitializeComponent();
        }

        public bool SetRom(RvFile tFile)
        {
            if (tFile.FileGroup == null)
            {
                return false;
            }

            StringBuilder sb = new StringBuilder();

            foreach (RvFile v in tFile.FileGroup.Files)
            {
                sb.AppendLine(v.GotStatus + " | " + v.FullName);
            }

            textBox1.Text = sb.ToString();
            return true;
        }
    }
}
