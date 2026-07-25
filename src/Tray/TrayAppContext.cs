using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;
using UzTypist.Hooks;

namespace UzTypist.Tray
{
    public sealed class TrayAppContext : ApplicationContext
    {
        private const string AppRegistryName = "UzTypist";
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string SettingsKeyPath = @"Software\UzTypist";
        private const string StartupConfiguredName = "StartupConfigured";

        private readonly NotifyIcon _trayIcon;
        private readonly KeyboardHook _hook;
        private readonly MouseClickWatcher _mouseWatcher;
        private readonly ForegroundWindowWatcher _windowWatcher;
        private readonly ToolStripMenuItem _infoItem;

        public TrayAppContext()
        {
            _hook = new KeyboardHook();
            _hook.Start();

            _mouseWatcher = new MouseClickWatcher(() => _hook.ResetContext());
            _mouseWatcher.Start();

            _windowWatcher = new ForegroundWindowWatcher(() => _hook.ResetContext());
            _windowWatcher.Start();

            InitStartup();

            var menu = new ContextMenuStrip();

            _infoItem = new ToolStripMenuItem("UzTypist faol")
            {
                Enabled = false
            };
            menu.Items.Add(_infoItem);
            menu.Items.Add(new ToolStripSeparator());

            var pauseItem = new ToolStripMenuItem("Pauza")
            {
                CheckOnClick = true,
                Checked = false
            };
            pauseItem.CheckedChanged += (_, _) => SetPaused(pauseItem.Checked);
            menu.Items.Add(pauseItem);

            var startupItem = new ToolStripMenuItem("Avtomatik ishga tushirish")
            {
                CheckOnClick = true,
                Checked = IsStartupEnabled()
            };
            startupItem.CheckedChanged += (_, _) => SetStartup(startupItem.Checked);
            menu.Items.Add(startupItem);

            menu.Items.Add(new ToolStripSeparator());

            var exitItem = new ToolStripMenuItem("Chiqish");
            exitItem.Click += (_, _) => ExitThread();
            menu.Items.Add(exitItem);

            _trayIcon = new NotifyIcon
            {
                Icon = LoadAppIcon(),
                Text = "UzTypist",
                Visible = true,
                ContextMenuStrip = menu
            };

            _trayIcon.BalloonTipTitle = "UzTypist";
            _trayIcon.BalloonTipText = "Dastur ishga tushdi.";
            _trayIcon.ShowBalloonTip(3000);
        }

        private static Icon LoadAppIcon()
        {
            var stream = typeof(TrayAppContext).Assembly
                .GetManifestResourceStream("UzTypist.uz-typist.ico");
            return stream is not null ? new Icon(stream) : SystemIcons.Application;
        }

        private void SetPaused(bool paused)
        {
            _hook.IsPaused = paused;
            _infoItem.Text = paused ? "UzTypist pauzada" : "UzTypist faol";
            _trayIcon.Text = paused ? "UzTypist (pauzada)" : "UzTypist";
        }

        private static string CurrentExecutablePath()
        {
            return Environment.ProcessPath ?? Application.ExecutablePath;
        }

        private static bool IsStartupEnabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
            return key?.GetValue(AppRegistryName) != null;
        }

        private static void InitStartup()
        {
            using var settings = Registry.CurrentUser.CreateSubKey(SettingsKeyPath);
            bool firstRun = settings?.GetValue(StartupConfiguredName) == null;

            if (firstRun)
            {
                SetStartup(true);
                settings?.SetValue(StartupConfiguredName, 1);
            }
            else if (IsStartupEnabled())
            {
                SetStartup(true);
            }
        }

        private static void SetStartup(bool enable)
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true)
                             ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);

            if (key is null)
            {
                return;
            }

            if (enable)
            {
                key.SetValue(AppRegistryName, $"\"{CurrentExecutablePath()}\"");
            }
            else
            {
                key.DeleteValue(AppRegistryName, false);
            }
        }

        protected override void ExitThreadCore()
        {
            _hook.Stop();
            _mouseWatcher.Stop();
            _windowWatcher.Stop();
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            base.ExitThreadCore();
        }
    }
}
