namespace StuAuthMobile.Page;
using System.Net.NetworkInformation;
using System.Net;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;

public partial class NetworkParameters : ContentPage
{
	public ObservableCollection<string> listIPA {  get; set; }
    private string IPApplication = Preferences.Get("IPApplication", string.Empty);
    public NetworkParameters()
	{
		InitializeComponent();

        listIPA = new ObservableCollection<string>();
        AppIP.ItemsSource = listIPA;
        AppIP.Title = IPApplication;

        string ipAdress = GetLocalIPAddress();
        ServIP.Text = ipAdress;

        Debug.WriteLine($"IP App:{IPApplication}");
    }

    private string GetLocalIPAddress()
    {
        string hostName = Dns.GetHostName();
        IPAddress[] addresses = Dns.GetHostAddresses(hostName);

        return addresses.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?.ToString() ?? "IP introuvable";
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
            DisplayAlert("Erreur", "Veuillez rentrer un réseau correct exemple: 192.168.1","OK");
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
                 
                    MainThread.BeginInvokeOnMainThread(()=>
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

    private void Sync(object sender, EventArgs e)
    {
        
    }

    private void Serveur(object sender, EventArgs e)
    {
        
    }
    #endregion
}