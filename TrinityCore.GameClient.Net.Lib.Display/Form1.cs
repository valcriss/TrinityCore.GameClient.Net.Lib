using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Entities;
using TrinityCore.GameClient.Net.Lib.Display.Models;
using TrinityCore.GameClient.Net.Lib.Log;
using TrinityCore.GameClient.Net.Lib.Map;
using TrinityCore.GameClient.Net.Lib.Network.Entities;
using TrinityCore.GameClient.Net.Lib.World.Entities;
using TrinityCore.GameClient.Net.Lib.World.Navigation;

namespace TrinityCore.GameClient.Net.Lib.Display
{
    public partial class Form1 : Form
    {
        private static Client Client { get; set; }
        private static bool Running { get; set; }
        private static bool Connected { get; set; }
        private List<Order> Orders { get; set; }

        private System.Timers.Timer Timer { get; set; }
        public Form1()
        {
            InitializeComponent();
            Running = true;
            Timer = new System.Timers.Timer(5000);
            Timer.Elapsed += Timer_Elapsed;
            Timer.Enabled = true;
            Timer.Start();
            Orders = new List<Order>();
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!Connected) return;
            Player player = Client.GetPlayer();
            if (player == null) return;
            listView1.Items.Clear();
            foreach (var item in Components.Entities.EntitiesComponent.Instance.Collection.Players.Values.OrderBy(c => (c.GetPosition() - player.GetPosition()).Length))
            {
                Position p = item.GetPosition();
                ListViewItem l = new ListViewItem(item.Name);
                l.Tag = item;
                l.SubItems.Add(item.Type.ToString());
                l.SubItems.Add(p.X.ToString());
                l.SubItems.Add(p.Y.ToString());
                l.SubItems.Add(p.Z.ToString());
                listView1.Items.Add(l);
            }
            foreach (var item in Components.Entities.EntitiesComponent.Instance.Collection.Creatures.Values.OrderBy(c => (c.GetPosition() - player.GetPosition()).Length).Take(10))
            {
                Position p = item.GetPosition();
                ListViewItem l = new ListViewItem(item.Name);
                l.Tag = item;
                l.SubItems.Add(item.Type.ToString());
                l.SubItems.Add(p.X.ToString());
                l.SubItems.Add(p.Y.ToString());
                l.SubItems.Add(p.Z.ToString());
                listView1.Items.Add(l);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            toolStripButton1.Enabled = Program.Configuration.IsValid;
            toolStripComboBox1.SelectedIndex = 0;
            Waze.StickToTerrain = false;
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            Forms.Config config = new Forms.Config();
            config.ShowDialog();
            toolStripButton1.Enabled = Program.Configuration.IsValid;
        }

        private void Status(string value)
        {
            toolStripStatusLabel1.Text = value;
            Application.DoEvents();
        }
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            Thread t = new Thread(Start);
            t.Start();
        }

        private void Start()
        {
            toolStripButton1.Enabled = false;
            Connected = false;
            Client = new Client(Program.Configuration.Host, Program.Configuration.Port, Program.Configuration.Login, Program.Configuration.Password, Program.Configuration.DataPath);
            Status("Authenticating...");
            if (!Authenticate())
            {
                MessageBox.Show("Unable to authenticate");
                toolStripButton1.Enabled = true;
                return;
            }
            Status("Requesting worlds...");
            List<WorldServerInfo> worlds = GetWorlds();
            if (worlds == null)
            {
                MessageBox.Show("Unable to get worlds");
                toolStripButton1.Enabled = true;
                return;
            }
            Status("Connecting to world " + worlds[0].Name + "...");
            if (!Connect(worlds[0]))
            {
                MessageBox.Show("Unable to connect to world");
                toolStripButton1.Enabled = true;
                return;
            }
            Status("Requesting characters...");
            List<Character> characters = GetCharacters();
            if (characters == null)
            {
                MessageBox.Show("Unable to get characters");
                toolStripButton1.Enabled = true;
                return;
            }
            Status("Connecting character " + characters[0].Name + "...");
            if (!EnterWorld(characters[0]))
            {
                MessageBox.Show("Unable to enterworld");
                toolStripButton1.Enabled = true;
                return;
            }
            Status("Character is connected");
            Connected = true;
            while (Running)
            {
                Thread.Sleep(150);
                lock(Orders)
                {
                    if(Orders.Count>0)
                    {
                        Order order = Orders[0];
                        if(order.Process())
                        {
                            Orders.RemoveAt(0);
                        }
                    }
                }
            }
        }

        private static bool Authenticate()
        {
            Logger.Log("[bold white]Starting Auth Login[/]", LogLevel.DETAIL);
            Logger.Log("Starting Authentication", LogLevel.DETAIL);
            bool authLogin = Client.Authenticate().Result;
            if (!authLogin)
            {
                Logger.Log("Authentication Failed", LogLevel.ERROR);
                return false;
            }

            Logger.Log("Authentication Success", LogLevel.SUCCESS);
            return true;
        }

        private static List<WorldServerInfo> GetWorlds()
        {
            return Client.GetWorlds().Result;
        }

        private static bool Connect(WorldServerInfo world)
        {
            Logger.Log("Starting World Connect : " + world.Name, LogLevel.DETAIL);
            bool worldLogin = Client.Connect(world).Result;

            if (!worldLogin)
            {
                Logger.Log("World Connect Failed", LogLevel.ERROR);
                return false;
            }

            Logger.Log("World Connect Success", LogLevel.SUCCESS);
            return true;
        }

        private static List<Character> GetCharacters()
        {
            return Client.GetCharacters().Result;
        }

        private static bool EnterWorld(Character character)
        {
            Logger.Log("Starting Character Entering World : " + character.Name, LogLevel.DETAIL);
            bool characterLogin = Client.EnterWorld(character).Result;
            if (!characterLogin)
            {
                Logger.Log("Character Entering Failed", LogLevel.ERROR);
                return false;
            }
            Logger.Log("Character Entering Success", LogLevel.SUCCESS);
            return true;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Running = false;
        }

        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            if(listView1.SelectedItems.Count>0)
            {
                ListViewItem item = listView1.SelectedItems[0];
                Entity entity = ((Entity)item.Tag);
                if (MessageBox.Show("Etes vous sur de vouloir allez vers " + entity.Name + "?", "Confirmation", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    MoveToEntityOrder order = new MoveToEntityOrder(Client, entity.Guid);
                    lock (Orders)
                    {
                        Orders.Add(order);
                    }
                }
            }
        }

        private void toolStripComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(toolStripComboBox1.SelectedIndex == 0)
            {
                Waze.StickToTerrain = false;
            }
            else
            {
                Waze.StickToTerrain = true;
            }
        }
    }
}
