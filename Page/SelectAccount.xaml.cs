using Microsoft.Maui.Dispatching;
using OtpNet;

namespace StuAuthMobile.Page;

public partial class SelectAccount : ContentPage
{
    private String accountName;
    private String OtpUri;
    private IDispatcherTimer timer;

    public SelectAccount(String Name, String Otp)
	{
		InitializeComponent();
        accountName = Name;
        OtpUri = Otp;
        AccountName.Text = accountName;
        GenerateOtp();

        timer = Dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromSeconds(1);
        timer.Tick += Timer_Tick;
        timer.Start();
    }

	private async void Back_Click(object sender, EventArgs e)
	{
        await Navigation.PopAsync();
    }

    public void GenerateOtp()
    {
        string otpauthUri = OtpUri;

        var uri = new Uri(otpauthUri);
        var query = uri.Query.TrimStart('?');
        var queryParams = System.Web.HttpUtility.ParseQueryString(query);
        var secretBase32 = queryParams["secret"];

        var secretBytes = Base32Encoding.ToBytes(secretBase32);

        var totp = new OtpNet.Totp(secretBytes, step: 30);

        var otp = totp.ComputeTotp();

        MDP.Text = otp;

        var timeRemaining = 30 - (DateTime.UtcNow.Second % 30);
        TempsRestant.Text = timeRemaining.ToString();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        GenerateOtp();
    }
}