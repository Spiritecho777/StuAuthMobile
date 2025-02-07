using StuAuthMobile.Classe;

namespace StuAuthMobile.Page;

public partial class NewAccount2 : ContentPage
{
    private string otpauth;
    private Main Menu;
    private string fName;

    public NewAccount2(string OtpAuth, Main menu, string folderName)
	{
		InitializeComponent();
        fName = folderName;
        otpauth = OtpAuth;
        Menu = menu;
    }

    private async void SaveNewAccount(object sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(AccountName.Text))
        {
            string[] part = otpauth.Split('/');
            otpauth = part[0] + "/" + part[1] + "/" + part[2] + "/" + AccountName.Text + "/" + part[3];
            string line = fName + "\\" + AccountName.Text + ";" + otpauth;
            AccountManager accountManager = new AccountManager();
            accountManager.AddAccount(line);

            await Navigation.PopToRootAsync();
            Menu.UpdateFolderList();
        }
        else
        {
            await DisplayAlert("","Veuillez entrez un nom de compte","OK");
        }
    }

    private async void Back(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}