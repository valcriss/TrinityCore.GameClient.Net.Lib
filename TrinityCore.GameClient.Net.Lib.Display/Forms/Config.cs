using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace TrinityCore.GameClient.Net.Lib.Display.Forms
{
    public partial class Config : Form
    {
        public Config()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Config_Load(object sender, EventArgs e)
        {
            txtHost.Text = Program.Configuration.Host;
            txtLogin.Text = Program.Configuration.Login;
            txtPassword.Text = Program.Configuration.Password;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Program.Configuration.Host = txtHost.Text;
            Program.Configuration.Login = txtLogin.Text;
            Program.Configuration.Password = txtPassword.Text;
            Program.Configuration.Port = 3724;
            Program.Configuration.DataPath = "none";
            Program.Configuration.LogLevel = "INFO";
            Program.Configuration.Save();
            this.Close();
        }
    }
}
