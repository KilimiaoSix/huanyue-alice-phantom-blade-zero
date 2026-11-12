using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

[assembly: System.Reflection.AssemblyTitle("Huanyue Alice · Phantom Blade Zero")]
[assembly: System.Reflection.AssemblyVersion("1.0.0.0")]
[assembly: System.Reflection.AssemblyFileVersion("1.0.0.0")]

namespace HuanyueAliceInstaller
{
    internal sealed class InstallerWindow : Form
    {
        private readonly ComboBox path = new ComboBox();
        private readonly Label status = new Label();
        private readonly Button install = new Button();
        private readonly Button uninstall = new Button();

        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new InstallerWindow());
        }

        private InstallerWindow()
        {
            Text = "幻月爱丽丝 · 影之刃零 v1.0.0";
            ClientSize = new Size(580, 201);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            Font = new Font("Microsoft YaHei UI", 9);

            var label = new Label { Text = "Steam 游戏目录", AutoSize = true, Location = new Point(15, 17) };
            path.Location = new Point(16, 43);
            path.Width = 388;
            path.TextChanged += delegate { RefreshStatus(); };
            var browse = new Button { Text = "浏览", Location = new Point(412, 42), Size = new Size(71, 27) };
            browse.Click += delegate { Browse(); };
            var detect = new Button { Text = "重新检测", Location = new Point(490, 42), Size = new Size(74, 27) };
            detect.Click += delegate { Detect(); };

            status.Location = new Point(15, 83);
            status.Size = new Size(550, 49);
            var dependency = new LinkLabel { Text = "获取已实测的 UE4SS", AutoSize = true, Location = new Point(16, 160) };
            dependency.LinkClicked += delegate
            {
                try
                {
                    Process.Start("https://github.com/KintaroModding/ue4ss/releases/tag/v3.0.1-1195-g1ceefac8");
                }
                catch (Exception error) { ShowError(error); }
            };

            uninstall.Text = "卸载 v1.0.0";
            uninstall.Location = new Point(335, 151);
            uninstall.Size = new Size(104, 34);
            uninstall.Click += delegate { Run(true); };
            install.Text = "安装 v1.0.0";
            install.Location = new Point(448, 151);
            install.Size = new Size(116, 34);
            install.Click += delegate { Run(false); };

            Controls.AddRange(new Control[] { label, path, browse, detect, status, dependency, uninstall, install });
            AcceptButton = install;
            Shown += delegate { Detect(); };
        }

        private void Detect()
        {
            try
            {
                IList<SteamGame> games = SteamDiscovery.FindGames();
                path.Items.Clear();
                path.Items.AddRange(games.Select(game => game.Root).Cast<object>().ToArray());
                SteamGame gameMatch = games.FirstOrDefault(game => game.Build == SteamDiscovery.CompatibleBuild);
                if (gameMatch == null && games.Count != 0) gameMatch = games[0];
                if (gameMatch != null) path.Text = gameMatch.Root;
                RefreshStatus();
            }
            catch (Exception error) { ShowError(error); }
        }

        private void Browse()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "选择包含 ProjectPBZ 和 platform 的 Steam 游戏目录。";
                dialog.ShowNewFolderButton = false;
                dialog.SelectedPath = path.Text;
                if (dialog.ShowDialog(this) == DialogResult.OK) path.Text = dialog.SelectedPath;
            }
        }

        private void RefreshStatus()
        {
            install.Enabled = false;
            uninstall.Enabled = false;
            if (string.IsNullOrWhiteSpace(path.Text))
            {
                status.Text = "未找到游戏时，可以手动选择 Steam 游戏目录。";
                return;
            }
            try
            {
                SteamGame game = SteamDiscovery.Inspect(path.Text.Trim());
                uninstall.Enabled = true;
                if (game.Build != SteamDiscovery.CompatibleBuild)
                    status.Text = "已找到 Steam Build " + game.Build + "。此版支持 Build 26367565。";
                else if (!ModInstaller.HasLoader(game.Root))
                    status.Text = "Steam Build 26367565。请先安装 UE4SS v3.0.1-1195-g1ceefac8。";
                else
                {
                    status.Text = "Steam Build 26367565，已检测到 UE4SS 文件。请退出游戏后安装。";
                    install.Enabled = true;
                }
            }
            catch (Exception error) { status.Text = error.Message; }
        }

        private void Run(bool remove)
        {
            if (remove && MessageBox.Show(this, "移除本版的 Mod 文件？", Text,
                MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;
            try
            {
                UseWaitCursor = true;
                string result = ModInstaller.Apply(path.Text.Trim(), remove);
                MessageBox.Show(this, result, Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception error) { ShowError(error); }
            finally { UseWaitCursor = false; RefreshStatus(); }
        }

        private void ShowError(Exception error)
        {
            MessageBox.Show(this, error.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
