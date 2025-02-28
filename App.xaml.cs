using System.Threading;
using System.IO;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System.Diagnostics;

namespace StuAuthMobile
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                CleanupBeforeExit();
            };
        }

        public static void CleanupBeforeExit()
        {
            try
            {
                string appDirectory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StuAuthData");
                string filePath = Path.Combine(appDirectory, "Account_decrypted.dat");
                string filePath2 = Path.Combine(appDirectory, "Account.dat");

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                long fileSize = new FileInfo(filePath2).Length;

                if (fileSize == 0 && File.Exists(filePath2))
                {
                    File.Delete(filePath2);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de la suppression des fichiers : {ex.Message}");
            }
        }
    }
}
