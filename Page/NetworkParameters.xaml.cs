namespace StuAuthMobile.Page;
using StuAuthMobile.Classe;
using System.Net.NetworkInformation;
using System.Net;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.Net.Sockets;
using Xamarin.Essentials;
using System.Text.Json;

public partial class NetworkParameters : ContentPage
{
    private Main main;
    private HttpServer server;
    private AccountManager accountManager;
    public ObservableCollection<string> listIPA { get; set; }
    private string IPApplication = Preferences.Get("IPApplication", string.Empty);
    public NetworkParameters(Main main, HttpServer server, AccountManager accountManager)
    {
        InitializeComponent();
        this.main = main;
        this.server = server;
        this.accountManager = accountManager;
        listIPA = new ObservableCollection<string>();
        AppIP.ItemsSource = listIPA;
        AppIP.Title = IPApplication;

        string ipAdress = GetLocalIPAddress();
        ServIP.Text = ipAdress;

        if (!main.isServerRunning)
        {
            Serv.Background = new SolidColorBrush(Colors.Red);
        }
        else
        {
            Serv.Background = new SolidColorBrush(Colors.Green);
        }
    }

    private string GetLocalIPAddress()
    {
        try
        {
            foreach (var netInterface in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                if (netInterface.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
                {
                    foreach (var addr in netInterface.GetIPProperties().UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                            !addr.Address.ToString().StartsWith("127.") &&  // Ignorer localhost
                            !addr.Address.ToString().StartsWith("169.254.")) // Ignorer APIPA (auto-assignée)
                        {
                            return addr.Address.ToString();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur : {ex.Message}");
        }

        return "IP introuvable";
    }

    private async void AppNetworkChanged(object sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() => AppIP.ItemsSource = null);
        if (!string.IsNullOrEmpty(AppNetwork.Text) && IsValidIPAddress(AppNetwork.Text))
        {
            listIPA.Clear();
            await ScanNetworkAsync(AppNetwork.Text);
            MainThread.BeginInvokeOnMainThread(() => AppIP.ItemsSource = new List<string>(listIPA));
        }
        else
        {
            DisplayAlert("Erreur", "Veuillez rentrer un réseau correct exemple: 192.168.1", "OK");
        }
    }

    private void RegisterIPA(object sender, EventArgs e)
    {
        if (AppIP.SelectedItem != null)
        {
            IPApplication = AppIP.SelectedItem.ToString();
            Preferences.Set("IPApplication", IPApplication);
        }
    }

    #region Scan reseau
    private bool IsValidIPAddress(string ipAddress)
    {
        string pattern = @"^(25[0-5]|2[0-4][0-9]|1?[0-9][0-9]?)\."
                         + @"(25[0-5]|2[0-4][0-9]|1?[0-9][0-9]?)\."
                         + @"(25[0-5]|2[0-4][0-9]|1?[0-9][0-9]?)$";
        return Regex.IsMatch(ipAddress, pattern);
    }

    public async Task ScanNetworkAsync(string subnet)
    {
        List<Task> tasks = new List<Task>();

        for (int i = 1; i < 255; i++)
        {
            string ip = $"{subnet}.{i}";
            tasks.Add(PingHost(ip));
        }

        await Task.WhenAll(tasks);
    }

    private async Task PingHost(string ipAddress)
    {
        try
        {
            using (Ping ping = new Ping())
            {
                PingReply reply = await ping.SendPingAsync(ipAddress, 500);

                if (reply.Status == IPStatus.Success)
                {
                    string hostName = GetHostName(ipAddress);

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        listIPA.Add(ipAddress);
                    });
                }
            }
        }
        catch
        {
            Debug.WriteLine($"Erreur lors du ping de {ipAddress}");
        }
    }

    private static string GetHostName(string ipAddress)
    {
        try
        {
            IPHostEntry hostEntry = Dns.GetHostEntry(ipAddress);
            return hostEntry.HostName;
        }
        catch
        {
            return "Nom d'hôte introuvable";
        }
    }
    #endregion

    #region Bouton
    private async void Back(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void Serveur(object sender, EventArgs e)
    {
        if (!main.isServerRunning)
        {
            main.ServeurConnect.Background = new SolidColorBrush(Colors.Green);
            Serv.Background = new SolidColorBrush(Colors.Green);
            server.Start(ServIP.Text);
            main.isServerRunning = true;
            await Navigation.PopAsync();
        }
        else
        {
            server.Stop();
            server = null;
            main.ServeurConnect.Background = new SolidColorBrush(Colors.Red);
            Serv.Background = new SolidColorBrush(Colors.Red);
            main.isServerRunning = false;
            await Navigation.PopAsync();
        }
    }

    private async void Sync(object sender, EventArgs e)
    {
        string answer = await DisplayActionSheet(
            "Synchronisation",
            "Annuler",
            null,
            "Importer"
        );

        if (answer == "Importer")
        {
            if (!string.IsNullOrWhiteSpace(IPApplication) && IsHostReachable(IPApplication))
            {
                string response = await SendRequestAsync("/", "GET");

                try
                {
                    Retour.IsEnabled = false;
                    Serv.IsEnabled = false;
                    Synchro.IsEnabled = false;
                    LoadingProgressBar.IsVisible = true;
                    LoadingProgressBar.Progress = 0;

                    var data = JsonSerializer.Deserialize<Dictionary<string, string>>(response);

                    if (data == null || !data.ContainsKey("Accounts") || !data.ContainsKey("Folder"))
                    {
                        Console.WriteLine(" Erreur: Données manquantes.");
                        return;
                    }

                    string[] accounts = data["Accounts"].Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    string[] folders = data["Folder"].Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                    string formattedAccounts;
                    int totalAccounts = accounts.Length;

                    for (int i = 0; i < totalAccounts; i++)
                    {
                        string folder = i < folders.Length ? folders[i].Trim() : "Uncategorized";
                        string otpUri = accounts[i].Trim();

                        string label = ExtractLabelFromOTP(otpUri);

                        formattedAccounts = ($"{folder}\\{label};{otpUri}");
                        await Task.Run(() => accountManager.AddAccount(formattedAccounts));

                        LoadingProgressBar.Progress = (double)(i + 1) / totalAccounts;
                    }

                    await Task.Delay(7000);
                    Synchro.IsEnabled = true;
                    Serv.IsEnabled = true;
                    Retour.IsEnabled = true;
                    LoadingProgressBar.IsVisible = false;
                    await Navigation.PopAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Erreur de parsing JSON: " + ex.Message);
                }
            }
            else
            {
                await DisplayAlert("Erreur", "L'adresse IP n'est pas valide ou inaccessible.", "OK");
            }
        }
    }
    #endregion

    static string ExtractLabelFromOTP(string otpUri)
    {
        var match = Regex.Match(otpUri, @"otpauth://totp/([^?]+)");
        return match.Success ? match.Groups[1].Value : "Unknown";
    }

    private bool IsHostReachable(string ip)
    {
        try
        {
            using (var ping = new Ping())
            {
                var reply = ping.Send(ip, 1000);
                return reply.Status == System.Net.NetworkInformation.IPStatus.Success;
            }
        }
        catch
        {
            return false;
        }
    }

    private async Task<string> SendRequestAsync(string endpoint, string method, string jsonData = null)
    {
        using (var client = new HttpClient())
        {
            client.BaseAddress = new Uri($"http://{IPApplication}:19755/");

            HttpResponseMessage response = null;

            if (method == "GET")
            {
                response = await client.GetAsync(endpoint);
            }

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                return null;
            }
        }
    }
}