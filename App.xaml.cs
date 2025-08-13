using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using StuAuthMobile.Classe;
using StuAuthMobile.Page;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;

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
                var ex = e.ExceptionObject as Exception;
                Debug.WriteLine($"Crash : {ex?.Message}");

                CleanupBeforeExit();
            };
        }

        /*protected override void OnStart()
        {
            base.OnStart();

            string savedLang = Preferences.Get("LangCode","en");

            var loc = (Loc)Application.Current.Resources["Loc"];
            loc.Culture = new CultureInfo(savedLang);
        }*/

        public static void CleanupBeforeExit()
        {
            Debug.WriteLine($"etat du bool: {HttpServer.isStart}");
            if (HttpServer.isStart)
            {
                Debug.WriteLine("l'application ne se ferme pas");
                return;
            }

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
