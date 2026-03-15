using CookieVault.Properties;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CookieVault
{
    public partial class MainForm : Form
    {
        #region Var_
        WebView2 webView;
        RoundedTextBox txtUrl;
        IconButton btnNavigate;
        IconButton btnClear;
        IconButton btnExport;
        Label lblCookieCount;
        Panel topBar;
        Panel statusBar;
        Label lblTitle;
        Panel divider;
        bool isLoading = false;
        int cookieCount = 0;
        readonly string userDataFolder;

        // Drop shadow
        [DllImport("dwmapi.dll")]
        static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);
        [StructLayout(LayoutKind.Sequential)]
        struct MARGINS { public int Left, Right, Top, Bottom; }
        #endregion

        public MainForm()
        {
            userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CookieVault", "WebData");

            InitializeComponents();
            InitializeWebView();
        }

        void InitializeComponents()
        {
            // Form settings
            Text = "CookieVault";
            Size = new Size(900, 620);
            MinimumSize = new Size(700, 480);
            BackColor = Color.FromArgb(13, 13, 18);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9f);
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;
            Icon = (Icon)Resources.ResourceManager.GetObject("cookico");

            // Custom title bar
            topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 58,
                BackColor = Color.FromArgb(18, 18, 26),
                Padding = new Padding(12, 0, 12, 0)
            };
            topBar.MouseDown += TopBar_MouseDown;

            // Close / Minimize buttons
            var btnClose = new ChromeButton("✕", Color.FromArgb(220, 60, 80))
            {
                Location = new Point(topBar.Width - 36, 18),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnClose.Click += (s, e) => Application.Exit();
            topBar.Controls.Add(btnClose);

            var btnMin = new ChromeButton("─", Color.FromArgb(50, 50, 70))
            {
                Location = new Point(topBar.Width - 74, 18),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnMin.Click += (s, e) => WindowState = FormWindowState.Minimized;
            topBar.Controls.Add(btnMin);

            // Shield icon + title
            var lblShield = new Label
            {
                Text = "🛡",
                Font = new Font("Segoe UI Emoji", 14f),
                ForeColor = Color.FromArgb(100, 200, 255),
                AutoSize = true,
                Location = new Point(14, 16),
                BackColor = Color.Transparent
            };
            topBar.Controls.Add(lblShield);

            lblTitle = new Label
            {
                Text = "CookieVault",
                Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 240, 255),
                AutoSize = true,
                Location = new Point(42, 19),
                BackColor = Color.Transparent
            };
            topBar.Controls.Add(lblTitle);

            var lblSub = new Label
            {
                Text = "Secure Cookie Browser",
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = Color.FromArgb(80, 80, 110),
                AutoSize = true,
                Location = new Point(44, 37),
                BackColor = Color.Transparent
            };
            topBar.Controls.Add(lblSub);

            // URL Bar panel
            var urlPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Color.FromArgb(13, 13, 18),
                Padding = new Padding(14, 10, 14, 8)
            };

            // URL TextBox
            txtUrl = new RoundedTextBox
            {
                Font = new Font("Segoe Mono", 9.5f),
                ForeColor = Color.FromArgb(200, 210, 240),
                BackColor = Color.FromArgb(22, 22, 32),
                BorderColor = Color.FromArgb(40, 40, 65),
                FocusBorderColor = Color.FromArgb(100, 180, 255),
                Height = 36,
                Text = "https://",
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            txtUrl.KeyDown += TxtUrl_KeyDown;

            btnNavigate = new IconButton("→", Color.FromArgb(100, 200, 255), Color.FromArgb(13, 13, 18))
            {
                Size = new Size(36, 36),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Cursor = Cursors.Hand
            };
            btnNavigate.Click += BtnNavigate_Click;

            btnClear = new IconButton("⟳", Color.FromArgb(70, 70, 100), Color.FromArgb(13, 13, 18))
            {
                Size = new Size(36, 36),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Cursor = Cursors.Hand,
                Visible = false
            };
            btnClear.Click += BtnClear_Click;

            btnExport = new IconButton("E", Color.FromArgb(80, 220, 140), Color.FromArgb(13, 13, 18))
            {
                Size = new Size(36, 36),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Cursor = Cursors.Hand,
                Visible = false
            };
            btnExport.Click += BtnExport_Click;

            urlPanel.Controls.Add(txtUrl);
            urlPanel.Controls.Add(btnNavigate);
            urlPanel.Controls.Add(btnClear);
            urlPanel.Controls.Add(btnExport);
            urlPanel.Resize += (s, e) => LayoutUrlPanel();

            // Divider
            divider = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = Color.FromArgb(30, 30, 50)
            };

            // WebView container
            var webContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(13, 13, 18)
            };

            webView = new WebView2
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(13, 13, 18)
            };
            webContainer.Controls.Add(webView);

            // Status bar
            statusBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                BackColor = Color.FromArgb(18, 18, 26),
                Padding = new Padding(14, 0, 14, 0)
            };

            var lblStatus = new Label
            {
                Text = "● Secure Sandbox  •  JavaScript enabled  •  Isolated cookies",
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = Color.FromArgb(60, 60, 90),
                AutoSize = true,
                Location = new Point(14, 7),
                BackColor = Color.Transparent
            };
            statusBar.Controls.Add(lblStatus);

            lblCookieCount = new Label
            {
                Text = "🍪 0 cookies",
                Font = new Font("Segoe UI Semibold", 7.5f),
                ForeColor = Color.FromArgb(100, 200, 255),
                AutoSize = true,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                BackColor = Color.Transparent
            };
            statusBar.Controls.Add(lblCookieCount);
            statusBar.Resize += (s, e) =>
            {
                lblCookieCount.Location = new Point(
                    statusBar.Width - lblCookieCount.Width - 14, 7);
            };

            // Resize grip
            var grip = new ResizeGrip(this)
            {
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Cursor = Cursors.SizeNWSE
            };
            statusBar.Controls.Add(grip);

            // Assembly
            Controls.Add(webContainer);
            Controls.Add(statusBar);
            Controls.Add(divider);
            Controls.Add(urlPanel);
            Controls.Add(topBar);

            LayoutUrlPanel();
        }

        void LayoutUrlPanel()
        {
            var panel = txtUrl?.Parent;
            if (panel == null) return;
            int pad = 14;
            int btnW = 36;
            int gap = 6;

            int numExtra = (btnClear != null && btnClear.Visible ? 1 : 0)
                         + (btnExport != null && btnExport.Visible ? 1 : 0);
            int totalBtns = btnW + numExtra * (btnW + gap);
            int textW = panel.Width - pad * 2 - totalBtns - gap;

            txtUrl.SetBounds(pad, 10, textW, 36);

            int rightX = pad + textW + gap;
            btnNavigate.Location = new Point(rightX, 10);
            rightX += btnW + gap;

            if (btnClear != null && btnClear.Visible)
            {
                btnClear.Location = new Point(rightX, 10);
                rightX += btnW + gap;
            }
            if (btnExport != null && btnExport.Visible)
            {
                btnExport.Location = new Point(rightX, 10);
            }
        }

        async void InitializeWebView()
        {
            try
            {
                Directory.CreateDirectory(userDataFolder);

                var env = await CoreWebView2Environment.CreateAsync(
                    browserExecutableFolder: null,
                    userDataFolder: userDataFolder,
                    options: new CoreWebView2EnvironmentOptions
                    {
                        AdditionalBrowserArguments =
                            "--disable-background-networking " +
                            "--disable-sync " +
                            "--disable-translate " +
                            "--metrics-recording-only " +
                            "--safebrowsing-disable-auto-update " +
                            "--no-first-run " +
                            "--disable-default-apps " +
                            "--disable-extensions "
                    });

                await webView.EnsureCoreWebView2Async(env);

                // Security settings
                var settings = webView.CoreWebView2.Settings;
                settings.IsScriptEnabled = true;          // JavaScript is required for cookies.
                settings.AreDefaultContextMenusEnabled = false;
                settings.IsStatusBarEnabled = false;
                settings.AreDevToolsEnabled = false;
                settings.IsZoomControlEnabled = false;
                settings.AreBrowserAcceleratorKeysEnabled = false;
                settings.IsBuiltInErrorPageEnabled = true;
                settings.IsPasswordAutosaveEnabled = false;
                settings.IsGeneralAutofillEnabled = false;
                settings.IsSwipeNavigationEnabled = false;

                // Block dangerous navigations
                webView.CoreWebView2.NewWindowRequested += (s, e) => e.Handled = true;
                webView.CoreWebView2.NavigationStarting += WebView_NavigationStarting;
                webView.CoreWebView2.NavigationCompleted += WebView_NavigationCompleted;
                webView.CoreWebView2.PermissionRequested += (s, e) =>
                    e.State = CoreWebView2PermissionState.Deny;

                btnClear.Visible = true;
                btnExport.Visible = true;
                LayoutUrlPanel();

                _ = UpdateCookieCount();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"WebView2 initialization error :\n{ex.Message}\n\nMake sure that WebView2 Runtime is installed.",
                    "CookieVault – Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void WebView_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (e.Uri == "about:blank")
            {
                txtUrl.Text = "https://";
                return;
            }

            if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out Uri uri) ||
                (uri.Scheme != "https" && uri.Scheme != "http"))
            {
                e.Cancel = true;
                return;
            }

            isLoading = true;
            SetLoadingState(true);
            txtUrl.Text = e.Uri;
        }

        async void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            isLoading = false;
            SetLoadingState(false);
            await UpdateCookieCount();
        }

        void SetLoadingState(bool loading)
        {
            if (InvokeRequired) { Invoke(new Action(() => SetLoadingState(loading))); return; }
            btnNavigate.Text = loading ? "■" : "→";
            btnNavigate.Refresh();
        }

        async Task UpdateCookieCount()
        {
            if (webView?.CoreWebView2 == null) return;
            try
            {
                var cookies = await webView.CoreWebView2.CookieManager.GetCookiesAsync("");
                cookieCount = cookies.Count;
                if (InvokeRequired) Invoke(new Action(RefreshCookieLabel));
                else RefreshCookieLabel();
            }
            catch { }
        }

        void RefreshCookieLabel()
        {
            lblCookieCount.Text = $"🍪 {cookieCount} cookie{(cookieCount != 1 ? "s" : "")}";

            lblCookieCount.Location = new Point(
                statusBar.Width - lblCookieCount.Width - 14, 7);
        }

        void BtnNavigate_Click(object sender, EventArgs e)
        {
            if (isLoading) { webView.CoreWebView2?.Stop(); return; }
            Navigate();
        }

        void BtnClear_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                        "Do you want to clear CookieVault data?\nThe application will close and then restart automatically.",
                        "Purge confirmation",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                string batPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "purge.bat");
                string exePath = Application.ExecutablePath;

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = batPath,
                    Arguments = $"\"{exePath}\"",
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Normal
                };

                Process.Start(psi);

                Application.Exit();
            }
        }

        void TxtUrl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                Navigate();
            }
        }

        async void BtnExport_Click(object sender, EventArgs e)
        {
            if (webView?.CoreWebView2 == null) return;

            try
            {
                var cookies = await webView.CoreWebView2.CookieManager.GetCookiesAsync("");

                if (cookies.Count == 0)
                {
                    MessageBox.Show(
                        "No cookies to export. \nLog in to a website first..",
                        "CookieVault – Export",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using (var sfd = new SaveFileDialog
                {
                    Title = "Save cookies",
                    Filter = "Netscape cookie file|cookies.txt|All files|*.*",
                    FileName = "cookies.txt",
                    DefaultExt = "txt"
                })
                {
                    if (sfd.ShowDialog() != DialogResult.OK) return;

                    string content = BuildNetscapeCookieFile(cookies);
                    File.WriteAllText(sfd.FileName, content, new System.Text.UTF8Encoding(false));

                    var original = btnExport.IconColor;
                    btnExport.IconColor = Color.FromArgb(0, 255, 120);
                    btnExport.Refresh();
                    await Task.Delay(600);
                    btnExport.IconColor = original;
                    btnExport.Refresh();

                    MessageBox.Show(
                        $"✔ {cookies.Count} cookie{(cookies.Count > 1 ? "s" : "")} exported{(cookies.Count > 1 ? "s" : "")} successfully.\n\n{sfd.FileName}",
                        "CookieVault – Successful export",
                        MessageBoxButtons.OK, MessageBoxIcon.None);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error during export :\n{ex.Message}",
                    "CookieVault – Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        string BuildNetscapeCookieFile(IReadOnlyList<CoreWebView2Cookie> cookies)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("# Netscape HTTP Cookie File");
            sb.AppendLine("# This file is generated by CookieVault.  Do not edit.");
            sb.AppendLine();

            foreach (var c in cookies)
            {
                // Format Netscape :
                // domain  includeSubdomains  path  secure  expiry  name  value
                string domain = c.Domain;
                // Domains without a starting point do not cover subdomains.
                bool includeSubdomains = domain.StartsWith(".");
                string subDomainFlag = includeSubdomains ? "TRUE" : "FALSE";
                string secureFlag = c.IsSecure ? "TRUE" : "FALSE";

                // Expiry : DateTime.MinValue = session cookie → we set it to 0
                long expiry = 0;
                if (c.Expires != DateTime.MinValue && c.Expires > new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                {
                    // Convert DateTime UTC → Unix seconds
                    expiry = (long)(c.Expires.ToUniversalTime()
                        - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                        .TotalSeconds;
                    if (expiry < 0) expiry = 0;
                }

                sb.AppendLine(string.Join("\t",
                    domain,
                    subDomainFlag,
                    c.Path,
                    secureFlag,
                    expiry.ToString(),
                    c.Name,
                    c.Value));
            }

            return sb.ToString();
        }

        void Navigate()
        {
            if (webView?.CoreWebView2 == null) return;

            string url = txtUrl.Text.Trim();
            if (string.IsNullOrEmpty(url)) return;

            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                url = "https://" + url;

            if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            {
                MessageBox.Show("URL invalide.", "CookieVault", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            txtUrl.Text = url;
            webView.CoreWebView2.Navigate(url);
        }

        // Drag to move
        Point dragOffset;
        void TopBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                dragOffset = e.Location;
                topBar.MouseMove += TopBar_MouseMove;
                topBar.MouseUp += TopBar_MouseUp;
            }
        }
        void TopBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Point screen = topBar.PointToScreen(e.Location);
                Location = new Point(screen.X - dragOffset.X, screen.Y - dragOffset.Y);
            }
        }
        void TopBar_MouseUp(object sender, MouseEventArgs e)
        {
            topBar.MouseMove -= TopBar_MouseMove;
            topBar.MouseUp -= TopBar_MouseUp;
        }

        // Rounded form border
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var pen = new Pen(Color.FromArgb(35, 35, 55), 1))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ClassStyle |= 0x20000; // CS_DROPSHADOW
                return cp;
            }
        }
    }
}
