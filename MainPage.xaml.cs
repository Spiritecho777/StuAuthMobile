using StuAuthMobile.Page;
using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using StuAuthMobile.Classe;

namespace StuAuthMobile
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await Navigation.PushAsync(new Main(this));
        }

        public async void NewAccount(string OtpAuth, Main menu, string folderName, AccountManager accountManager)
        {
            await Navigation.PushAsync(new NewAccount2(OtpAuth, menu, folderName, accountManager));
        }

        public async void ImportM(List<string> Account, Main menu, string folderName, AccountManager accountManager)
        {
            await Navigation.PushAsync(new Import(Account, menu, folderName, accountManager));
        }
    }
}
