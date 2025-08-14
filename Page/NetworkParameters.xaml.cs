namespace StuAuthMobile.Page;
using StuAuthMobile.Classe;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xamarin.Essentials;

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
        var loc = (Loc)Microsoft.Maui.Controls.Application.Current.Resources["Loc"];
        this.main = main;
        this.server = server;
        this.accountManager = accountManager;
        listIPA = new ObservableCollection<string>();
        AppIP.ItemsSource = listIPA;
        AppIP.Title = IPApplication;

        LoadNetworkInterfaces();
        int savedIndex = int.Parse(Preferences.Get("InterfaceSelect", "0"));
        ListInterface.SelectedIndex = savedIndex;

        if (ListInterface.SelectedItem is NetworkInterfaceInfo selectedInterface)
        {
            string ipAdress = GetLocalIPAddress(selectedInterface.Name);
            ServIP.Text = ipAdress;
        }
        else
        {
            ServIP.Text = loc["IntNP7"];
        }

        if (!main.isServerRunning)
        {
            Serv.Background = new SolidColorBrush(Colors.Red);
        }
        else
        {
            Serv.Background = new SolidColorBrush(Colors.Green);
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

    #region Reseau
    private string GetLocalIPAddress(string interfaceName)
    {
        var loc = (Loc)Microsoft.Maui.Controls.Application.Current.Resources["Loc"];
        try
        {
            foreach (var netInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (netInterface.Name != interfaceName)
                    continue;

                if (netInterface.OperationalStatus != OperationalStatus.Up)
                    continue;

                foreach (var addr in netInterface.GetIPProperties().UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                        !addr.Address.ToString().StartsWith("127.") &&
                        !addr.Address.ToString().StartsWith("169.254."))
                    {
                        return addr.Address.ToString();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur : {ex.Message}");
        }

        return loc["IntNP6"];
    }

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

        List<string> arpDevices = GetAllConnectedDevices(subnet);
        foreach (string ip in arpDevices)
        {
            if (!listIPA.Contains(ip))
            {
                listIPA.Add(ip);
            }
        }
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

    private List<string> GetAllConnectedDevices(string subnet)
    {
        List<string> devices = new List<string>();

        try
        {
            Process p = new Process();
            p.StartInfo.FileName = "arp";
            p.StartInfo.Arguments = "-a";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.CreateNoWindow = true;
            p.Start();

            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            string pattern = @"(\d+\.\d+\.\d+\.\d+)\s+([a-fA-F0-9:-]+)";
            foreach (Match match in Regex.Matches(output, pattern))
            {
                string ip = match.Groups[1].Value;

                if (ip.StartsWith(subnet + "."))
                {
                    devices.Add(ip);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Erreur lors de la récupération des appareils via ARP : " + ex.Message);
        }

        return devices;
    }

    static string ExtractLabelFromOTP(string otpUri)
    {
        var match = Regex.Match(otpUri, @"otpauth://totp/([^?]+)");
        return match.Success ? match.Groups[1].Value.Replace("/", "") : "Unknown";
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
        var loc = (Loc)Microsoft.Maui.Controls.Application.Current.Resources["Loc"];
        using (var client = new HttpClient())
        {
            try
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
            catch (HttpRequestException ex)
            {
                await DisplayAlert(loc["Error"], loc["IntNP4"], "OK");
                return null;
            }
            catch (TaskCanceledException ex)
            {
                Debug.WriteLine("Erreur", $"Requête expirée : {ex.Message}", "OK");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Erreur", $"Erreur inattendue : {ex.Message}", "OK");
                return null;
            }
        }
    }

    private void LoadNetworkInterfaces()
    {
        var interfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
            .Select(ni => new NetworkInterfaceInfo
            {
                Name = ni.Name,
                Description = ni.Description
            })
            .ToList();

        ListInterface.ItemsSource = interfaces;
    }
    #endregion

    #region Control
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
        var loc = (Loc)Microsoft.Maui.Controls.Application.Current.Resources["Loc"];
        string answer = await DisplayActionSheet(
        loc["Synchronization"],
        loc["Cancel"],
        null,
        loc["IntNP5"]
        );

        if (answer == loc["IntNP5"])
        {
            if (!string.IsNullOrWhiteSpace(IPApplication) && IsHostReachable(IPApplication))
            {
                string response = await SendRequestAsync("/", "GET");
                if (response != null)
                {
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

                        List<string> comptesExistants = accountManager.GetAllOtpUri();

                        for (int i = 0; i < totalAccounts; i++)
                        {
                            string folder = i < folders.Length ? folders[i].Trim() : "Uncategorized";
                            string otpUri = accounts[i].Trim();

                            bool alreadyExists = comptesExistants.Any(ligne => otpUri.Contains(ligne));

                            if (!alreadyExists)
                            {
                                string label = ExtractLabelFromOTP(otpUri);

                                formattedAccounts = ($"{folder}\\{label};{otpUri}");
                                await Task.Run(() => accountManager.AddAccount(formattedAccounts));

                                LoadingProgressBar.Progress = (double)(i + 1) / totalAccounts;
                            }
                        }

                        await Task.Delay(1000);
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
            }
            else
            {
                await DisplayAlert(loc["Error"], loc["IntNP2"], "OK");
            }
        }      
    }

    private async void AppNetworkChanged(object sender, EventArgs e)
    {
        var loc = (Loc)Microsoft.Maui.Controls.Application.Current.Resources["Loc"];
        MainThread.BeginInvokeOnMainThread(() => AppIP.ItemsSource = null);
        if (!string.IsNullOrEmpty(AppNetwork.Text) && IsValidIPAddress(AppNetwork.Text))
        {
            listIPA.Clear();
            await ScanNetworkAsync(AppNetwork.Text);
            MainThread.BeginInvokeOnMainThread(() => AppIP.ItemsSource = new List<string>(listIPA));
        }
        else
        {
            DisplayAlert(loc["Error"], loc["IntNP3"], "OK");
        }
    }

    private void ChangedInterface(object sender, EventArgs e)
    {
        if (ListInterface.SelectedItem is NetworkInterfaceInfo selectedInterface)
        {
            string ipAddress = GetLocalIPAddress(selectedInterface.Name);
            ServIP.Text = ipAddress;

            Preferences.Set("InterfaceSelect", ListInterface.SelectedIndex.ToString());
        }
    }
    #endregion
}

public class NetworkInterfaceInfo
{
    public string Name { get; set; }
    public string Description { get; set; }

    public override string ToString() => $"{Name} - {Description}";
}