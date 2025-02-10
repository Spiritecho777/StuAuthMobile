using StuAuthMobile.Classe;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace StuAuthMobile.Page;

public partial class Import : ContentPage
{
    Main menu;
    List<string> Account = new List<string>();
    string fName;
    public ObservableCollection<CheckBoxItem> AccountListAdd { get; set; } = new();
    public Import(List<string> AccountList, Main window, string folderName)
	{
		InitializeComponent();
        menu = window;
        fName = folderName;

        if (AccountList.Count != 0)
        {
            Account = AccountList;
            ImportGoogle();
        }
        else
        {
            Initialisation();
        }
    }

    private void Initialisation()
    {
        /*OpenFileDialog ofd = new OpenFileDialog();
        ofd.Filter = "Fichiers texte (*.txt)|*.txt";
        ofd.Title = "Sélectionnez un fichier texte";

        if (ofd.ShowDialog() == DialogResult.OK)
        {
            string filePath = ofd.FileName;
            string[] lignes = File.ReadAllLines(filePath);

            foreach (string line in lignes)
            {
                if (line.StartsWith("otpauth:"))
                {
                    string[] part = line.Split('/');
                    string name = part[3];
                    part = name.Split("?");
                    name = part[0];
                    name = name.Replace("%20", "")
                                .Replace("%40", "@")
                                .Replace("%3A", ":");

                    CheckBox? checkBox = new CheckBox();
                    checkBox.Content = name + ";" + line;
                    ListOtp.Items.Add(checkBox);
                }
            }
        }*/
    }

    private void ImportGoogle()
    {
        MainThread.BeginInvokeOnMainThread(() => ListOtp.ItemsSource = null);
        foreach (string line in Account)
        {
            if (line.StartsWith("otpauth:"))
            {
                string[] part = line.Split('/');
                string name = part[3];
                part = name.Split("?");
                name = part[0];
                if (name.Contains("%20"))
                {
                    name = name.Replace("%20", " ");
                }
                if (name.Contains("%40"))
                {
                    name = name.Replace("%40", "@");
                }

                CheckBox checkBox = new CheckBox
                {
                    IsChecked = false,
                };
                AccountListAdd.Add(new CheckBoxItem { Name = name, OtpUri = line, IsChecked = false });
            }
        }
        MainThread.BeginInvokeOnMainThread(() => ListOtp.ItemsSource = AccountListAdd);
    }

    private async void Confirm_Click(object sender, EventArgs e)
    {
        foreach (var item in AccountListAdd)
        {
            if (item.IsChecked)
            {
                string? line = $"{item.Name};{item.OtpUri}";
                line = $"{fName}\\{line}";

                if (!string.IsNullOrEmpty(line))
                    {
                        string[] part = line.Split(';');
                    if (part.Length == 2)
                    {
                    try
                        {
                            var uri = new Uri(part[1]);
                            AccountManager accountManager = new AccountManager();
                            accountManager.AddAccount(line);
                        }
                        catch
                        {
                            DisplayAlert("Erreur", "Il y a une erreur dans votre fichier d'export veuillez vérifier et recommencer", "OK");
                        }
                    }
                }
            }
        }

        await Navigation.PopToRootAsync();
        menu.UpdateFolderList();
    }

    private async void Back_Click(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
public class CheckBoxItem
{
    public string Name { get; set; }
    public string OtpUri { get; set; }
    public bool IsChecked { get; set; }
}