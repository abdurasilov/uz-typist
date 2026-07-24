using System;
using System.Windows.Forms;
using UzTypist.Tray;

namespace UzTypist
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            using var singleInstanceGuard = new System.Threading.Mutex(
                true, "UzTypist_SingleInstance", out bool isNew);

            if (!isNew)
            {
                MessageBox.Show(
                    "Dastur allaqachon faol.",
                    "UzTypist",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                using var context = new TrayAppContext();
                Application.Run(context);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "UzTypist",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
