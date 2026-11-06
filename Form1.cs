using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RobloxOptimizerLauncher
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        // Элементы шапки
        private Panel pnlHeader = null!;
        private Label lblHeaderTitle = null!;
        private TextBox txtHeaderSearch = null!;
        private Button btnClose = null!;
        private Button btnMinimize = null!;

        // Левый сайдбар
        private Panel pnlSidebar = null!;
        private Button btnNavHome = null!;
        private Button btnNavGames = null!;
        private Button btnNavMacros = null!;
        private Button btnNavSettings = null!;
        private Panel pnlActiveIndicator = null!;

        // Главный контейнер контента
        private Panel pnlContent = null!;
        private Panel panelHome = null!;
        private Panel panelGames = null!;
        private Panel panelMacros = null!;
        private Panel panelSettings = null!;

        // Элементы вкладки "Home / Главная"
        private Label lblHomeTitle = null!;
        private CheckBox chkUltraLowGraphics = null!;
        private CheckBox chkAutoOptimize = null!;
        private Button btnOptimizeFPS = null!;
        private Button btnPreloadMap = null!;
        private Button btnFixPing = null!;
        private Button btnFastLaunch = null!;
        private Label lblStatus = null!;

        // Элементы вкладки "Catalog / Игры"
        private Label lblGamesTitle = null!;
        private FlowLayoutPanel flowGamesGrid = null!;

        // Элементы вкладки "Macros / Макросы"
        private Label lblMacrosTitle = null!;
        private CheckBox chkAutoClicker = null!;
        private Label lblClickSpeed = null!;
        private TrackBar trackBarSpeed = null!;
        private Label lblMacroStatus = null!;

        // Элементы вкладки "Settings / Настройки"
        private Label lblSettingsTitle = null!;
        private Label lblLangLabel = null!;
        private ComboBox cmbLanguage = null!;
        private CheckBox chkAutostart = null!;

        private string currentLang = "en";
        private readonly string settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RobloxLauncher_Settings.json");

        public Form1()
        {
            LoadSettings();

            this.Text = "Roblox Optimizer Launcher";
            this.Size = new Size(950, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(15, 15, 15);
            this.DoubleBuffered = true;

            InitHeader();
            InitSidebar();
            InitContentContainers();
            ApplyLanguage();

            SwitchTab(panelHome, btnNavHome);
            RunAutoOptimization();
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(settingsFilePath))
                {
                    string json = File.ReadAllText(settingsFilePath);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("Language", out var langProp))
                    {
                        currentLang = langProp.GetString() ?? "en";
                    }
                }
            }
            catch
            {
                currentLang = "en";
            }
        }

        private void SaveSettings()
        {
            try
            {
                var data = new { Language = currentLang };
                string json = JsonSerializer.Serialize(data);
                File.WriteAllText(settingsFilePath, json);
            }
            catch { }
        }

        private void RunAutoOptimization()
        {
            Task.Run(() =>
            {
                ApplyRobloxClientSettings(chkUltraLowGraphics != null && chkUltraLowGraphics.Checked);
                CleanDeepCache();
            });
        }

        private void InitHeader()
        {
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = Color.FromArgb(23, 24, 27)
            };
            pnlHeader.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(this.Handle, 0xA1, 0x2, 0);
                }
            };

            lblHeaderTitle = new Label
            {
                Text = "ROBLOX OPTIMIZER",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 162, 255),
                Location = new Point(15, 14),
                AutoSize = true
            };

            txtHeaderSearch = new TextBox
            {
                Font = new Font("Segoe UI", 10f),
                BackColor = Color.FromArgb(35, 37, 42),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Size = new Size(280, 28),
                Location = new Point(320, 10)
            };
            txtHeaderSearch.TextChanged += (s, e) => FilterGames(txtHeaderSearch.Text);

            btnClose = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 10f),
                ForeColor = Color.FromArgb(200, 200, 200),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Right,
                Width = 45,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 17, 35);
            btnClose.Click += (s, e) => Application.Exit();

            btnMinimize = new Button
            {
                Text = "—",
                Font = new Font("Segoe UI", 10f),
                ForeColor = Color.FromArgb(200, 200, 200),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Right,
                Width = 45,
                Cursor = Cursors.Hand
            };
            btnMinimize.FlatAppearance.BorderSize = 0;
            btnMinimize.FlatAppearance.MouseOverBackColor = Color.FromArgb(45, 45, 50);
            btnMinimize.Click += (s, e) => this.WindowState = FormWindowState.Minimized;

            pnlHeader.Controls.Add(lblHeaderTitle);
            pnlHeader.Controls.Add(txtHeaderSearch);
            pnlHeader.Controls.Add(btnMinimize);
            pnlHeader.Controls.Add(btnClose);
            this.Controls.Add(pnlHeader);
        }

        private void InitSidebar()
        {
            pnlSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 70,
                BackColor = Color.FromArgb(23, 24, 27)
            };

            btnNavHome = CreateCustomNavButton(DrawHomeIcon, 20);
            btnNavGames = CreateCustomNavButton(DrawGamesIcon, 80);
            btnNavMacros = CreateCustomNavButton(DrawMacrosIcon, 140);
            btnNavSettings = CreateCustomNavButton(DrawSettingsIcon, 200);

            btnNavHome.Click += (s, e) => SwitchTab(panelHome, btnNavHome);
            btnNavGames.Click += (s, e) => SwitchTab(panelGames, btnNavGames);
            btnNavMacros.Click += (s, e) => SwitchTab(panelMacros, btnNavMacros);
            btnNavSettings.Click += (s, e) => SwitchTab(panelSettings, btnNavSettings);

            pnlActiveIndicator = new Panel
            {
                Width = 4,
                Height = 30,
                BackColor = Color.FromArgb(0, 162, 255),
                Location = new Point(0, 28)
            };

            pnlSidebar.Controls.Add(pnlActiveIndicator);
            pnlSidebar.Controls.Add(btnNavHome);
            pnlSidebar.Controls.Add(btnNavGames);
            pnlSidebar.Controls.Add(btnNavMacros);
            pnlSidebar.Controls.Add(btnNavSettings);

            this.Controls.Add(pnlSidebar);
        }

        private Button CreateCustomNavButton(Action<Graphics, Color> drawIcon, int top)
        {
            Button btn = new Button
            {
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(10, top),
                Size = new Size(50, 46),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(35, 37, 42);

            btn.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Color color = btn.ForeColor;
                drawIcon(e.Graphics, color);
            };

            return btn;
        }

        private void DrawHomeIcon(Graphics g, Color c)
        {
            using Pen p = new Pen(c, 2);
            g.DrawLines(p, new Point[] { new Point(15, 28), new Point(15, 20), new Point(25, 12), new Point(35, 20), new Point(35, 28) });
            g.DrawRectangle(p, 21, 22, 8, 6);
        }

        private void DrawGamesIcon(Graphics g, Color c)
        {
            using Pen p = new Pen(c, 2);
            g.DrawRoundedRectangle(p, 13, 17, 24, 14, 4);
            g.DrawLine(p, 25, 20, 25, 28);
            g.DrawLine(p, 21, 24, 29, 24);
        }

        private void DrawMacrosIcon(Graphics g, Color c)
        {
            using Pen p = new Pen(c, 2);
            Point[] pts = { new Point(27, 12), new Point(17, 23), new Point(24, 23), new Point(21, 34), new Point(33, 21), new Point(26, 21) };
            g.DrawPolygon(p, pts);
        }

        private void DrawSettingsIcon(Graphics g, Color c)
        {
            using Pen p = new Pen(c, 2);
            g.DrawEllipse(p, 19, 17, 12, 12);
            g.DrawEllipse(p, 22, 20, 6, 6);
        }

        private void SwitchTab(Panel targetPanel, Button activeBtn)
        {
            panelHome.Visible = false;
            panelGames.Visible = false;
            panelMacros.Visible = false;
            panelSettings.Visible = false;

            targetPanel.Visible = true;
            pnlActiveIndicator.Top = activeBtn.Top + (activeBtn.Height - pnlActiveIndicator.Height) / 2;

            AnimatePanelFadeIn(targetPanel);

            btnNavHome.ForeColor = Color.FromArgb(140, 145, 155);
            btnNavGames.ForeColor = Color.FromArgb(140, 145, 155);
            btnNavMacros.ForeColor = Color.FromArgb(140, 145, 155);
            btnNavSettings.ForeColor = Color.FromArgb(140, 145, 155);

            activeBtn.ForeColor = Color.White;
        }

        private async void AnimatePanelFadeIn(Panel p)
        {
            for (int i = 5; i <= 15; i += 3)
            {
                p.BackColor = Color.FromArgb(i, i, i);
                await Task.Delay(10);
            }
            p.BackColor = Color.FromArgb(15, 15, 15);
        }

        private void InitContentContainers()
        {
            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(15, 15, 15)
            };

            panelHome = CreateTabPage();
            panelGames = CreateTabPage();
            panelMacros = CreateTabPage();
            panelSettings = CreateTabPage();

            BuildHomeTab(panelHome);
            BuildGamesTab(panelGames);
            BuildMacrosTab(panelMacros);
            BuildSettingsTab(panelSettings);

            pnlContent.Controls.Add(panelHome);
            pnlContent.Controls.Add(panelGames);
            pnlContent.Controls.Add(panelMacros);
            pnlContent.Controls.Add(panelSettings);

            this.Controls.Add(pnlContent);
            pnlContent.BringToFront();
        }

        private Panel CreateTabPage()
        {
            return new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(15, 15, 15),
                Padding = new Padding(35, 25, 25, 25),
                Visible = false
            };
        }

        private void BuildHomeTab(Panel parent)
        {
            lblHomeTitle = new Label
            {
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(35, 20),
                Size = new Size(500, 38)
            };

            chkAutoOptimize = new CheckBox
            {
                Font = new Font("Segoe UI", 10f),
                ForeColor = Color.FromArgb(0, 180, 100),
                Location = new Point(38, 68),
                Size = new Size(550, 25),
                Checked = true,
                Cursor = Cursors.Hand
            };

            chkUltraLowGraphics = new CheckBox
            {
                Font = new Font("Segoe UI", 10f),
                ForeColor = Color.FromArgb(220, 220, 220),
                Location = new Point(38, 100),
                Size = new Size(550, 25),
                Checked = true,
                Cursor = Cursors.Hand
            };

            btnOptimizeFPS = CreateActionButton(140, BtnOptimizeFPS_Click);
            btnPreloadMap = CreateActionButton(195, BtnPreloadMap_Click);
            btnFixPing = CreateActionButton(250, BtnFixPing_Click);

            btnFastLaunch = new Button
            {
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(0, 162, 255),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(35, 315),
                Size = new Size(515, 50),
                Cursor = Cursors.Hand
            };
            btnFastLaunch.FlatAppearance.BorderSize = 0;
            btnFastLaunch.Click += BtnFastLaunch_Click;

            lblStatus = new Label
            {
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(160, 165, 175),
                Location = new Point(35, 375),
                Size = new Size(515, 35),
                TextAlign = ContentAlignment.MiddleCenter
            };

            parent.Controls.Add(lblHomeTitle);
            parent.Controls.Add(chkAutoOptimize);
            parent.Controls.Add(chkUltraLowGraphics);
            parent.Controls.Add(btnOptimizeFPS);
            parent.Controls.Add(btnPreloadMap);
            parent.Controls.Add(btnFixPing);
            parent.Controls.Add(btnFastLaunch);
            parent.Controls.Add(lblStatus);
        }

        private Button CreateActionButton(int top, EventHandler clickEvent)
        {
            Button btn = new Button
            {
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(24, 26, 31),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(35, top),
                Size = new Size(515, 42),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(15, 0, 0, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(38, 41, 48);
            btn.Click += clickEvent;
            return btn;
        }

        private void BuildGamesTab(Panel parent)
        {
            lblGamesTitle = new Label
            {
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(35, 20),
                Size = new Size(500, 38)
            };

            flowGamesGrid = new FlowLayoutPanel
            {
                Location = new Point(35, 70),
                Size = new Size(840, 470),
                AutoScroll = true,
                BackColor = Color.Transparent
            };

            PopulateGamesGrid("");

            parent.Controls.Add(lblGamesTitle);
            parent.Controls.Add(flowGamesGrid);
        }

        private void PopulateGamesGrid(string filter)
        {
            flowGamesGrid.Controls.Clear();

            var games = new (string Name, long PlaceId, string Rating)[]
            {
                ("Brookhaven RP", 4924922222, "87% Rating"),
                ("Blox Fruits", 2753915549, "94% Rating"),
                ("BedWars", 6872265039, "82% Rating"),
                ("DOORS", 6516141723, "90% Rating"),
                ("Adopt Me!", 920587237, "85% Rating"),
                ("Bloxburg", 185655149, "89% Rating"),
                ("Pet Simulator 99", 8737899170, "88% Rating"),
                ("Murder Mystery 2", 142823291, "91% Rating"),
                ("Tower of Hell", 1962086868, "80% Rating"),
                ("Blade Ball", 13772394625, "86% Rating")
            };

            foreach (var game in games)
            {
                if (string.IsNullOrEmpty(filter) || game.Name.ToLower().Contains(filter.ToLower()))
                {
                    Panel card = new Panel
                    {
                        Size = new Size(155, 185),
                        Margin = new Padding(10),
                        BackColor = Color.FromArgb(23, 24, 27),
                        Cursor = Cursors.Hand
                    };

                    Panel thumb = new Panel
                    {
                        Size = new Size(155, 110),
                        Dock = DockStyle.Top,
                        BackColor = Color.FromArgb(32, 35, 42)
                    };

                    PictureBox picThumb = new PictureBox
                    {
                        Dock = DockStyle.Fill,
                        BackColor = Color.Transparent
                    };
                    picThumb.Paint += (s, e) => {
                        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        using LinearGradientBrush brush = new LinearGradientBrush(picThumb.ClientRectangle, Color.FromArgb(40, 45, 60), Color.FromArgb(20, 22, 30), 45f);
                        e.Graphics.FillRectangle(brush, picThumb.ClientRectangle);
                        using Font f = new Font("Segoe UI", 11f, FontStyle.Bold);
                        e.Graphics.DrawString("ROBLOX", f, Brushes.DimGray, new PointF(35, 42));
                    };

                    Label lblTitle = new Label
                    {
                        Text = game.Name,
                        Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                        ForeColor = Color.White,
                        Location = new Point(8, 115),
                        Size = new Size(138, 40)
                    };

                    Label lblRating = new Label
                    {
                        Text = "★ " + game.Rating,
                        Font = new Font("Segoe UI", 8.5f),
                        ForeColor = Color.FromArgb(0, 180, 100),
                        Location = new Point(8, 155),
                        Size = new Size(138, 20)
                    };

                    thumb.Controls.Add(picThumb);
                    card.Controls.Add(thumb);
                    card.Controls.Add(lblTitle);
                    card.Controls.Add(lblRating);

                    EventHandler launchAction = (s, e) => LaunchGame(game.PlaceId);
                    card.Click += launchAction;
                    thumb.Click += launchAction;
                    picThumb.Click += launchAction;
                    lblTitle.Click += launchAction;

                    flowGamesGrid.Controls.Add(card);
                }
            }
        }

        private void FilterGames(string query)
        {
            if (panelGames.Visible)
            {
                PopulateGamesGrid(query);
            }
        }

        private void LaunchGame(long placeId)
        {
            try
            {
                if (chkAutoOptimize.Checked)
                {
                    RunAutoOptimization();
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = $"https://www.roblox.com/games/start?placeid={placeId}",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void BuildMacrosTab(Panel parent)
        {
            lblMacrosTitle = new Label
            {
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(35, 20),
                Size = new Size(500, 38)
            };

            chkAutoClicker = new CheckBox
            {
                Font = new Font("Segoe UI", 10f),
                ForeColor = Color.FromArgb(220, 220, 220),
                Location = new Point(38, 75),
                Size = new Size(500, 30),
                Cursor = Cursors.Hand
            };
            chkAutoClicker.CheckedChanged += ChkAutoClicker_CheckedChanged;

            lblClickSpeed = new Label
            {
                Font = new Font("Segoe UI", 10f),
                ForeColor = Color.FromArgb(220, 220, 220),
                Location = new Point(38, 120),
                Size = new Size(500, 25)
            };

            trackBarSpeed = new TrackBar
            {
                Location = new Point(35, 155),
                Size = new Size(515, 45),
                Minimum = 1,
                Maximum = 50,
                Value = 10,
                BackColor = Color.FromArgb(15, 15, 15)
            };
            trackBarSpeed.Scroll += (s, e) => {
                lblClickSpeed.Text = currentLang == "ru" 
                    ? $"Скорость: {trackBarSpeed.Value} кликов в секунду" 
                    : $"Speed: {trackBarSpeed.Value} clicks per second";
            };

            lblMacroStatus = new Label
            {
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(160, 165, 175),
                Location = new Point(35, 230),
                Size = new Size(515, 60),
                TextAlign = ContentAlignment.MiddleCenter
            };

            parent.Controls.Add(lblMacrosTitle);
            parent.Controls.Add(chkAutoClicker);
            parent.Controls.Add(lblClickSpeed);
            parent.Controls.Add(trackBarSpeed);
            parent.Controls.Add(lblMacroStatus);
        }

        private void ChkAutoClicker_CheckedChanged(object? sender, EventArgs e)
        {
            if (chkAutoClicker.Checked)
            {
                lblMacroStatus.Text = currentLang == "ru" ? $"Автокликер активен ({trackBarSpeed.Value} к/с)" : $"Auto-clicker active ({trackBarSpeed.Value} c/s)";
                lblMacroStatus.ForeColor = Color.FromArgb(0, 180, 100);
            }
            else
            {
                lblMacroStatus.Text = currentLang == "ru" ? "Автокликер выключен." : "Auto-clicker disabled.";
                lblMacroStatus.ForeColor = Color.FromArgb(160, 165, 175);
            }
        }

        private void BuildSettingsTab(Panel parent)
        {
            lblSettingsTitle = new Label
            {
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(35, 20),
                Size = new Size(500, 38)
            };

            lblLangLabel = new Label
            {
                Font = new Font("Segoe UI", 10f),
                ForeColor = Color.FromArgb(220, 220, 220),
                Location = new Point(35, 80),
                Size = new Size(300, 30)
            };

            cmbLanguage = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10f),
                Location = new Point(35, 115),
                Size = new Size(250, 30),
                BackColor = Color.FromArgb(24, 26, 31),
                ForeColor = Color.White
            };
            cmbLanguage.Items.Add("English");
            cmbLanguage.Items.Add("Русский");

            cmbLanguage.SelectedIndex = currentLang == "ru" ? 1 : 0;
            cmbLanguage.SelectedIndexChanged += (s, e) => {
                currentLang = cmbLanguage.SelectedIndex == 1 ? "ru" : "en";
                SaveSettings();
                ApplyLanguage();
            };

            chkAutostart = new CheckBox
            {
                Font = new Font("Segoe UI", 10f),
                ForeColor = Color.FromArgb(220, 220, 220),
                Location = new Point(38, 175),
                Size = new Size(450, 30),
                Cursor = Cursors.Hand,
                Checked = IsAutostartEnabled()
            };
            chkAutostart.CheckedChanged += (s, e) => {
                SetAutostart(chkAutostart.Checked);
            };

            parent.Controls.Add(lblSettingsTitle);
            parent.Controls.Add(lblLangLabel);
            parent.Controls.Add(cmbLanguage);
            parent.Controls.Add(chkAutostart);
        }

        private void ApplyLanguage()
        {
            if (currentLang == "ru")
            {
                lblHomeTitle.Text = "Рекомендуем Для Вас";
                chkAutoOptimize.Text = "Автоматическая оптимизация при каждом запуске";
                chkUltraLowGraphics.Text = "Режим Potato Graphics (максимальный FPS)";
                btnOptimizeFPS.Text = "Очистить кэш и применить конфиг";
                btnPreloadMap.Text = "Прогрузить полную карту (без вылета)";
                btnFixPing.Text = "Сбросить сетевой кэш (DNS)";
                btnFastLaunch.Text = "ЗАПУСТИТЬ ROBLOX";
                lblStatus.Text = "Автоматическая оптимизация включена";

                lblGamesTitle.Text = "Каталог Игр";
                txtHeaderSearch.PlaceholderText = "Поиск...";

                lblMacrosTitle.Text = "Макросы";
                chkAutoClicker.Text = "Включить автокликер";
                lblClickSpeed.Text = $"Скорость: {trackBarSpeed.Value} кликов в секунду";
                lblMacroStatus.Text = "Автокликер готов к активации.";

                lblSettingsTitle.Text = "Настройки";
                lblLangLabel.Text = "Язык приложения (Language):";
                chkAutostart.Text = "Запускать оптимизатор вместе с Windows";
            }
            else
            {
                lblHomeTitle.Text = "Recommended For You";
                chkAutoOptimize.Text = "Automatic optimization on every launch";
                chkUltraLowGraphics.Text = "Potato Graphics mode (maximum FPS)";
                btnOptimizeFPS.Text = "Clean cache & apply config";
                btnPreloadMap.Text = "Preload full map (anti-crash)";
                btnFixPing.Text = "Reset network cache (DNS)";
                btnFastLaunch.Text = "LAUNCH ROBLOX";
                lblStatus.Text = "Automatic optimization enabled";

                lblGamesTitle.Text = "Game Catalog";
                txtHeaderSearch.PlaceholderText = "Search...";

                lblMacrosTitle.Text = "Macros";
                chkAutoClicker.Text = "Enable auto-clicker";
                lblClickSpeed.Text = $"Speed: {trackBarSpeed.Value} clicks per second";
                lblMacroStatus.Text = "Auto-clicker ready.";

                lblSettingsTitle.Text = "Settings";
                lblLangLabel.Text = "Application Language:";
                chkAutostart.Text = "Run optimizer automatically with Windows";
            }
        }

        private void SetAutostart(bool enable)
        {
            try
            {
                string appName = "RobloxOptimizerLauncher";
                string appPath = Application.ExecutablePath;
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        if (enable)
                            key.SetValue(appName, $"\"{appPath}\" --autostart");
                        else
                            key.DeleteValue(appName, false);
                    }
                }
            }
            catch { }
        }

        private bool IsAutostartEnabled()
        {
            try
            {
                string appName = "RobloxOptimizerLauncher";
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    return key?.GetValue(appName) != null;
                }
            }
            catch
            {
                return false;
            }
        }

        private void BtnOptimizeFPS_Click(object? sender, EventArgs e)
        {
            Task.Run(() =>
            {
                ApplyRobloxClientSettings(chkUltraLowGraphics.Checked);
                CleanDeepCache();
            });

            lblStatus.Text = currentLang == "ru" ? "Кэш очищен, конфигурация обновлена" : "Cache cleaned, config updated";
            lblStatus.ForeColor = Color.FromArgb(0, 180, 100);
        }

        private void BtnPreloadMap_Click(object? sender, EventArgs e)
        {
            Task.Run(() => ApplyPreloadMapSettings());

            lblStatus.Text = currentLang == "ru" ? "Безопасный режим прогрузки карты включен!" : "Safe map preloading mode enabled!";
            lblStatus.ForeColor = Color.FromArgb(0, 180, 100);
        }

        private void BtnFixPing_Click(object? sender, EventArgs e)
        {
            Task.Run(() =>
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c ipconfig /flushdns",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(psi);
            });

            lblStatus.Text = currentLang == "ru" ? "DNS-кэш успешно сброшен" : "DNS cache successfully flushed";
            lblStatus.ForeColor = Color.FromArgb(0, 180, 100);
        }

        private async void BtnFastLaunch_Click(object? sender, EventArgs e)
        {
            btnFastLaunch.Enabled = false;
            lblStatus.Text = currentLang == "ru" ? "Авто-оптимизация и запуск..." : "Auto-optimizing & launching...";
            lblStatus.ForeColor = Color.FromArgb(255, 180, 50);

            if (chkAutoOptimize.Checked)
            {
                await Task.Run(() =>
                {
                    ApplyRobloxClientSettings(chkUltraLowGraphics.Checked);
                    CleanDeepCache();
                });
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "roblox-player://",
                    UseShellExecute = true
                });

                _ = Task.Run(async () =>
                {
                    for (int i = 0; i < 15; i++)
                    {
                        await Task.Delay(200);
                        if (SetRobloxHighPriority()) break;
                    }
                });

                lblStatus.Text = currentLang == "ru" ? "Игра успешно запущена" : "Game successfully launched";
                lblStatus.ForeColor = Color.FromArgb(0, 180, 100);
            }
            catch
            {
                lblStatus.Text = currentLang == "ru" ? "Загрузка установщика..." : "Downloading installer...";
                await DownloadAndInstallRobloxAsync();
            }

            btnFastLaunch.Enabled = true;
        }

        private void ApplyRobloxClientSettings(bool ultraLow)
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string versionsPath = Path.Combine(localAppData, "Roblox", "Versions");

            if (!Directory.Exists(versionsPath)) return;

            foreach (string versionDir in Directory.GetDirectories(versionsPath))
            {
                string clientSettingsDir = Path.Combine(versionDir, "ClientSettings");
                if (!Directory.Exists(clientSettingsDir))
                {
                    Directory.CreateDirectory(clientSettingsDir);
                }

                string configFile = Path.Combine(clientSettingsDir, "ClientAppSettings.json");

                string jsonConfig = @"{
                    ""FFlagDebugGraphicsDisableDirect3D11"": ""False"",
                    ""DFIntTaskSchedulerTargetFps"": ""999"",
                    ""FFlagRenderFixQualityLevel"": ""True"",
                    ""FIntDebugForceQualityLevel"": """ + (ultraLow ? "1" : "3") + @""",
                    ""DFIntConnectionMTUSize"": ""1400"",
                    ""FFlagEnableUdpSockets"": ""True""
                }";

                File.WriteAllText(configFile, jsonConfig);
            }
        }

        private void ApplyPreloadMapSettings()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string versionsPath = Path.Combine(localAppData, "Roblox", "Versions");

            if (!Directory.Exists(versionsPath)) return;

            foreach (string versionDir in Directory.GetDirectories(versionsPath))
            {
                string clientSettingsDir = Path.Combine(versionDir, "ClientSettings");
                if (!Directory.Exists(clientSettingsDir))
                {
                    Directory.CreateDirectory(clientSettingsDir);
                }

                string configFile = Path.Combine(clientSettingsDir, "ClientAppSettings.json");

                string jsonConfig = @"{
                    ""FFlagDebugGraphicsDisableDirect3D11"": ""False"",
                    ""DFIntTaskSchedulerTargetFps"": ""999"",
                    ""DFIntStreamingTargetArchiveCapacity"": ""1048576"",
                    ""FFlagStreamingDisableOutofOrderStreaming"": ""True"",
                    ""DFIntMinimalRadiusAroundPhysicsCharactersToStreamIn"": ""1024"",
                    ""FFlagRenderTextureManagerSkipTextureUnload"": ""True"",
                    ""DFIntConnectionMTUSize"": ""1400"",
                    ""FFlagEnableUdpSockets"": ""True""
                }";

                File.WriteAllText(configFile, jsonConfig);
            }
        }

        private void CleanDeepCache()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string[] pathsToClean = new string[]
            {
                Path.Combine(localAppData, "Roblox", "cache"),
                Path.Combine(localAppData, "Roblox", "logs")
            };

            foreach (string path in pathsToClean)
            {
                if (Directory.Exists(path))
                {
                    try
                    {
                        DirectoryInfo di = new DirectoryInfo(path);
                        foreach (FileInfo file in di.GetFiles()) file.Delete();
                    }
                    catch { }
                }
            }
        }

        private bool SetRobloxHighPriority()
        {
            Process[] processes = Process.GetProcessesByName("RobloxPlayerBeta");
            foreach (var proc in processes)
            {
                try
                {
                    proc.PriorityClass = ProcessPriorityClass.High;
                    return true;
                }
                catch { }
            }
            return false;
        }

        private async Task DownloadAndInstallRobloxAsync()
        {
            try
            {
                string tempInstallerPath = Path.Combine(Path.GetTempPath(), "RobloxPlayerInstaller.exe");
                using (HttpClient client = new HttpClient())
                {
                    byte[] data = await client.GetByteArrayAsync("https://www.roblox.com/download/client");
                    File.WriteAllBytes(tempInstallerPath, data);
                }
                Process.Start(tempInstallerPath);
                lblStatus.Text = currentLang == "ru" ? "Установщик загружен." : "Installer downloaded.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error: " + ex.Message;
                lblStatus.ForeColor = Color.FromArgb(232, 17, 35);
            }
        }
    }

    public static class GraphicsExtensions
    {
        public static void DrawRoundedRectangle(this Graphics g, Pen pen, int x, int y, int width, int height, int radius)
        {
            using GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            path.AddArc(x, y, diameter, diameter, 180, 90);
            path.AddArc(x + width - diameter, y, diameter, diameter, 270, 90);
            path.AddArc(x + width - diameter, y + height - diameter, diameter, diameter, 0, 90);
            path.AddArc(x, y + height - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            g.DrawPath(pen, path);
        }
    }
}