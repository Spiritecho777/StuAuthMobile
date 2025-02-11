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
                await Navigation.PushAsync(new NewAccount(pages, this, Folder));
            }
            else //Dossier
            {
                string FolderN = await DisplayPromptAsync("Ajouter un dossier","Entrez le nom du dossier");

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
            DisplayAlert("Erreur", "Aucun compte sélectionné.", "OK");
            return;
        }

        string name = selectedItem.Text;

        try
        {
            if (!string.IsNullOrEmpty(folder))
            {
                if (accountManager.DeleteFolderOrAccount(name, isFolder: false))
                {
                    DisplayAlert("Info",$"Le compte '{name}' a été supprimé avec succès.","OK");
                    selectedItem = null;
                }
            }
            else
            {
                if (accountManager.DeleteFolderOrAccount(name, isFolder: true))
                {
                    DisplayAlert("Info",$"Le dossier '{name}' a été supprimé avec succès.","OK");
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
            DisplayAlert("Erreur", "Aucun compte sélectionné.", "OK");
            return;
        }
        string oldName = selectedItem.Text.ToString();
        string newName = await DisplayPromptAsync("Renommée", "Entrez le nouveau nom");

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

    private void Serveur_Connect(object sender, EventArgs e)
    {
        // Logique pour la connexion au serveur
    }

    private void Help_Click(object sender, EventArgs e)
    {
        string version = AppInfo.VersionString;
        DisplayAlert("Numéro de version", $"Numero de version actuel : {version}", "OK");
    }

    private void TimeSynchro(object sender, EventArgs e)
    {
        // Logique de synchronisation
    }

    private void Back_Click(object sender, EventArgs e)
    {
        UpdateFolderList();
    }

    private async void SyncApp_Click(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new NetworkParameters());
    }
    #endregion  

    #region Méthode

    #endregion
}
