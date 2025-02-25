using StuAuthMobile.Classe;
using Microsoft.Maui.Controls.PlatformConfiguration;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Maui.Controls.Shapes;
using static System.Net.Mime.MediaTypeNames;
using System.Collections.Generic;
using Microsoft.VisualBasic;
using System.Net.NetworkInformation;
using System.Net;
using System.Text.Json;
using Android.Content;
using Android.Provider;
using Microsoft.Maui.ApplicationModel;

namespace StuAuthMobile.Page;

public partial class Main : ContentPage
{
    #region Variable
    private MainPage pages;
    private List<string> AccountName = new List<string>();
    private List<string> OtpUri = new List<string>();
    private HttpServer server;
    private AccountManager accountManager = new AccountManager();
    public bool isServerRunning = false;
    public string name;
    private ObservableCollection<ListItemModel> AccountButtons = new ObservableCollection<ListItemModel>();
    public bool selected = false;
    private ListItemModel selectedItem;
    private Button lastSelectedButton = null;
    #endregion

    public Main(MainPage page)
	{
		InitializeComponent();
        pages = page;
        server = new HttpServer(this);
        AccountList.ItemsSource = AccountButtons;
        UpdateFolderList();
    }

    #region Liste
    #region Dossier
    private void ListFolder()
    {
        AccountButtons.Clear();
        MainThread.BeginInvokeOnMainThread(() => AccountList.ItemsSource = null);

        var accountByFolder = AccountName
                .Zip(OtpUri, (name, uri) => new { Name = name.TrimEnd('\\'), Uri = uri })
                .GroupBy(c => c.Name);

        HashSet<string> addedFolders = new HashSet<string>();

        foreach (var folder in accountByFolder)
        {
            string fullName = folder.Key;
            string[] parts = fullName.Split('\\');
            string folderName = parts[0];

            if (!addedFolders.Contains(folderName))
            {
                addedFolders.Add(folderName);

                var button = new ListItemModel()
                {
                    Text = folderName,
                    IsFolder = true,
                    OtpUri = null
                };
                AccountButtons.Add(button);              
            }
        }
        MainThread.BeginInvokeOnMainThread(() => AccountList.ItemsSource = AccountButtons);
    }

    private void OpenFolder(object sender, EventArgs e)
    {
        Button clickedButton = (Button)sender;

        if (sender is Button button && button.BindingContext is ListItemModel item && item.IsFolder)
        {
            string buttonName = clickedButton.Text.ToString();
            FolderName.Text = buttonName;
            UpdateAccountList(buttonName);
        }
    }

    public void UpdateFolderList()
    {
        AccountName.Clear();
        OtpUri.Clear();

        if (accountManager.FileExists())
        {
            Dictionary<string, int> occurrences = accountManager.CountFolderOccurrences();

            List<string> validLines = accountManager.GetValidLines(occurrences);

            foreach (string line in validLines)
            {
                string[] parts = line.Split(';');
                if (parts.Length == 2)
                {
                    AccountName.Add(parts[0]);
                    OtpUri.Add(parts[1]);
                }
            }

            accountManager.WriteLines(validLines);
        }

        FolderName.Text = "";
        ListFolder();
    }

    #endregion

    #region Compte
    private void ListAccount()
    {

        AccountButtons.Clear();
        MainThread.BeginInvokeOnMainThread(() => AccountList.ItemsSource = AccountButtons);

        var accountByFolder = AccountName
          .Zip(OtpUri, (name, uri) => new { Name = name, Uri = uri })
          .GroupBy(c => c.Name);

        foreach (var folder in accountByFolder)
        {
            foreach (var account in folder)
            {
                string fullName = account.Name;
                string[] parts = fullName.Split('\\');
                if (parts[1] != "")
                {                   
                    var button = new ListItemModel()
                    {
                        Text = parts[1], // Nom du compte
                        IsFolder = false,
                        OtpUri = account.Uri
                    };

                    AccountButtons.Add(button);
                }
            }
        }
        MainThread.BeginInvokeOnMainThread(() => AccountList.ItemsSource = AccountButtons);
    }

    private async void AccountView(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is ListItemModel item && !item.IsFolder)
        {
            string AC = item.Text;
            string OU = item.OtpUri;

            button.Style = (Style)this.Resources["CustomButton"];
            await Navigation.PushAsync(new SelectAccount(AC,OU));
        }
    }

    public void UpdateAccountList(string folderName)
    {
        AccountName.Clear();
        OtpUri.Clear();

        List<string> accounts = accountManager.GetAccountsByFolder(folderName);

        foreach (var account in accounts)
        {          
            string[] parts = account.Split(';');
            if (parts.Length == 2)
            {
                AccountName.Add(parts[0]);
                OtpUri.Add(parts[1]);
            }
        }

        ListAccount();
    }
    #endregion
    #endregion

    #region Bouton
    private async void Add_Click(object sender, EventArgs e)
    {
        if (pages != null) //Compte
        {
            string Folder = FolderName.Text.ToString();
            if (!string.IsNullOrEmpty(Folder))
            {
                await Navigation.PushAsync(new NewAccount(pages, this, Folder, accountManager));
            }
            else //Dossier
            {
                string FolderN = await DisplayPromptAsync("Ajouter un dossier","Entrez le nom du dossier");

                if (string.IsNullOrWhiteSpace(FolderN))
                {
                    await DisplayAlert("Erreur", "Le nom du dossier ne peut pas �tre vide.", "OK");
                    return;
                }

                accountManager.Add(FolderN);
                UpdateFolderList();
            }
        }
    }

    private void Del_Click(object sender, EventArgs e)
    {
        string folder = FolderName.Text?.ToString();
        if (selectedItem == null)
        {
            DisplayAlert("Erreur", "Aucun compte s�lectionn�.", "OK");
            return;
        }

        string name = selectedItem.Text;

        try
        {
            if (!string.IsNullOrEmpty(folder))
            {
                if (accountManager.DeleteFolderOrAccount(name, isFolder: false))
                {
                    DisplayAlert("Info",$"Le compte '{name}' a �t� supprim� avec succ�s.","OK");
                    selectedItem = null;
                }
            }
            else
            {
                if (accountManager.DeleteFolderOrAccount(name, isFolder: true))
                {
                    DisplayAlert("Info",$"Le dossier '{name}' a �t� supprim� avec succ�s.","OK");
                }
            }

            UpdateFolderList();
        }
        catch (InvalidOperationException ex)
        {
            DisplayAlert("Erreur",ex.Message,"OK");
        }
    }

    private void OnButtonPressed(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is ListItemModel item)
        {
            if (lastSelectedButton != null)
            {
                lastSelectedButton.BackgroundColor = Colors.Transparent;
            }

            button.BackgroundColor = Colors.LightBlue;
            lastSelectedButton = button;

            selectedItem = item;
        }
    }

    private async void Rename_Click(object sender, EventArgs e)
    {
        string folder = FolderName.Text?.ToString();
        if (selectedItem == null)
        {
            DisplayAlert("Erreur", "Aucun compte s�lectionn�.", "OK");
            return;
        }
        string oldName = selectedItem.Text.ToString();
        string newName = await DisplayPromptAsync("Renomm�e", "Entrez le nouveau nom");

        if (!string.IsNullOrEmpty(newName))
        {
            if (!string.IsNullOrEmpty(folder))
            {
                accountManager.RenameFolderOrAccount(oldName, newName, isFolder: false);
            }
            else
            {
                accountManager.RenameFolderOrAccount(oldName, newName, isFolder: true);
            }

            UpdateFolderList();
        }
    }

    private async void Serveur_Connect(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new NetworkParameters(this, server,accountManager));
    }

    private void Help_Click(object sender, EventArgs e)
    {
        string version = AppInfo.VersionString;
        DisplayAlert("Num�ro de version", $"Numero de version actuel : {version}", "OK");
    }

    private async void TimeSynchro(object sender, EventArgs e)
    {
        DateTime localTime = DateTime.Now;  // Heure du t�l�phone
        DateTime networkTime = await GetNetworkTimeAsync();
        TimeSpan difference = networkTime - localTime;

        if (Math.Abs(difference.TotalSeconds) > 10)
        {
            DisplayAlert("Desynchronisation", "L'heure de votre appareil est incorrecte !", "OK");
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                await OpenDateTimeSettings();
            }
            else if (DeviceInfo.Platform == DevicePlatform.iOS)
            {
                await Launcher.OpenAsync("App-Prefs:");
            }
        }
        else
        {
            DisplayAlert("Synchronisation", "L'heure est bien synchroniser", "OK");
        }
    }

    private void Back_Click(object sender, EventArgs e)
    {
        UpdateFolderList();
    }

    #endregion  

    #region M�thode
    public async Task<DateTime> GetNetworkTimeAsync()
    {
        try
        {
            using HttpClient client = new HttpClient();
            string response = await client.GetStringAsync("http://worldtimeapi.org/api/timezone/Etc/UTC");
            var json = JsonSerializer.Deserialize<Dictionary<string, object>>(response);

            if (json != null && json.ContainsKey("utc_datetime"))
            {
                return DateTime.Parse(json["utc_datetime"].ToString());
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erreur de r�cup�ration de l'heure NTP: {ex.Message}");
        }

        return DateTime.UtcNow; // Fallback si erreur
    }

    public async Task OpenDateTimeSettings()
    {
        try
        {
            var intent = new Intent(Settings.ActionDateSettings);
            intent.SetFlags(ActivityFlags.NewTask);
            Platform.CurrentActivity.StartActivity(intent);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erreur lors de l'ouverture des param�tres : {ex.Message}");
        }
    }
    #endregion
}
