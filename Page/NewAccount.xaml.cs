namespace StuAuthMobile.Page;
using Microsoft.Maui.Controls;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
using Google.Protobuf;
using Migration;
using System.Text;
using System.Web;
using System.Diagnostics;
using ZXing.QrCode.Internal;
using StuAuthMobile.Classe;

public partial class NewAccount : ContentPage
{
    private MainPage main;
    private AccountManager accountManager;
    private Main menu;
    private List<string> AccountList = new List<string>();
    private string fName;

    public NewAccount(MainPage main, Main menu, string folderName, AccountManager accountManager)
    {
        InitializeComponent();
        fName = folderName;
        this.main = main;
        this.menu = menu;
        this.accountManager = accountManager;
    }

    #region Bouton
    private void Capture(object sender, EventArgs e)
    {
        scannerView.IsVisible = true;
        QRcapture.IsVisible = false;
    }

    private void Confirm_click(object sender, EventArgs e)
    {
        var loc = (Loc)Microsoft.Maui.Controls.Application.Current.Resources["Loc"];
        if (!string.IsNullOrEmpty(SecretKey.Text))
        {
            string normalizeText = SecretKey.Text.Normalize(NormalizationForm.FormD);
            if (SecretKey.Text == normalizeText && SecretKey.Text.All(char.IsLetterOrDigit))
            {
                string otpauth = "otpauth://totp/?secret=" + SecretKey.Text + "&digits=6&period=30";
                main.NewAccount(otpauth, menu, fName, accountManager);
            }
            else
            {
                DisplayAlert(loc["Error"], loc["IntNewAccount2"], "OK");
                SecretKey.Text = string.Empty;
            }
        }
        else
        {
            DisplayAlert(loc["Error"], loc["IntNewAccount"], "OK");
        }
    }

    private async void Back(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
    #endregion

    private void OnBarcodeDetected(object sender, BarcodeDetectionEventArgs e)
    {
        // Empêche le scanner de détecter plusieurs fois le même code
        scannerView.IsDetecting = false;

        // Récupère le contenu du QR Code
        string codeScanne = e.Results[0].Value;

        // Affiche une alerte avec les données scannées
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            DecodeQRCode(codeScanne);       
        });
    }

    #region Méthode
    private string DecodeQRCode(string result)
    {
        var loc = (Loc)Microsoft.Maui.Controls.Application.Current.Resources["Loc"];
        if (result != null)
        {
            if (result.Contains("migration"))
            {
                string otpauth = result;
                int dataIndex = otpauth.IndexOf("data=") + "data=".Length;
                string base64Data = otpauth.Substring(dataIndex);
                base64Data = HttpUtility.UrlDecode(base64Data);

                byte[] bytes = Convert.FromBase64String(base64Data);
                List<ByteString> SecretPayload = DecodeProtobufSecret(bytes);
                List<string> AccountPayload = DecodeProtobufAccount(bytes);
                for (int i = 0; i < SecretPayload.Count; i++)
                {
                    string secret = Base32Encode(SecretPayload[i], false);
                    string Account = AccountPayload[i];
                    AccountList.Add("otpauth://totp/" + Account + "?secret=" + secret + "&digits=6&period=30");
                }

                main.ImportM(AccountList, menu, fName, accountManager);

                return otpauth;
            }
            else
            {
                main.NewAccount(result.ToString(), menu, fName, accountManager);
                return result;
            }
        }
        else
        {
            DisplayAlert(loc["Error"], loc["IntNewAccount1"],"OK");
            return "Pas de QR code trouver";
        }
    }

    public dynamic DecodeProtobufSecret(byte[] payload)
    {
        List<ByteString> secret = new List<ByteString>();

        var parser = new MessageParser<Payload>(() => new Payload());
        var message = parser.ParseFrom(payload);
        if (message.OtpParameters.Count > 0)
        {
            for (int i = 0; i < message.OtpParameters.Count; i++)
            {
                secret.Add(message.OtpParameters[i].Secret);
            }
            return secret;
        }
        else
        {
            return "Rien a migré";
        }
    }

    public dynamic DecodeProtobufAccount(byte[] payload)
    {
        List<string> Account = new List<string>();

        var parser = new MessageParser<Payload>(() => new Payload());
        var message = parser.ParseFrom(payload);
        if (message.OtpParameters.Count > 0)
        {
            for (int i = 0; i < message.OtpParameters.Count; i++)
            {
                Account.Add(message.OtpParameters[i].Name);
            }
            return Account;
        }
        else
        {
            return "Rien a migré";
        }
    }

    public static string Base32Encode(ByteString data, bool padding)
    {
        const string base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        StringBuilder result = new StringBuilder((data.Length * 8 + 4) / 5);

        int buffer = data[0];
        int next = 1;
        int bitsLeft = 8;
        while (bitsLeft > 0 || next < data.Length)
        {
            if (bitsLeft < 5)
            {
                if (next < data.Length)
                {
                    buffer <<= 8;
                    buffer |= data[next++] & 0xFF;
                    bitsLeft += 8;
                }
                else
                {
                    int pad = 5 - bitsLeft;
                    buffer <<= pad;
                    bitsLeft += pad;
                }
            }

            int index = 0x1F & (buffer >> (bitsLeft - 5));
            bitsLeft -= 5;
            result.Append(base32Chars[index]);
        }
        if (padding)
        {
            while (result.Length % 8 != 0)
            {
                result.Append("=");
            }
        }
        return result.ToString();
    }
    #endregion
}