/******************************************************
 *     ROMVault3 is written by Gordon J.              *
 *     Contact gordon@romvault.com                    *
 *     Copyright 2020                                 *
 ******************************************************/

using System;
using System.Windows.Forms;

namespace ROMVault
{
    public partial class FrmShowError : Form
    {
        public FrmShowError()
        {
            InitializeComponent();
        }

        public void Settype(string s)
        {
            textBox1.Text = s;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
