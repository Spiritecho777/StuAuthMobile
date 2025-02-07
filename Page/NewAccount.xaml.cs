namespace StuAuthMobile.Page;
using Microsoft.Maui.Controls;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
using Google.Protobuf;
using Migration;
//using ZXing.Net.Maui;
//using ZXing.Net.Maui.Controls;
using System.Drawing;
using System.Text;
using System.Web;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;
using Color = System.Drawing.Color;
using System.Diagnostics;
using ZXing.QrCode.Internal;

//using Cursors = System.Windows.Forms.Cursors;

public partial class NewAccount : ContentPage
{
    private Point startPoint;
    private MainPage main;
    private Main menu;
    private List<string> AccountList = new List<string>();
    private string fName;

    public NewAccount(MainPage main, Main menu, string folderName)
    {
        InitializeComponent();
        fName = folderName;
        this.main = main;
        this.menu = menu;
    }

    /*private string DecodeQRCode(Bitmap bitmap)
    {
        BarcodeReader reader = new BarcodeReader();

        Result result = reader.Decode(bitmap);

        if (result != null)
        {
            if (result.Text.Contains("migration"))
            {
                string otpauth = result.Text;
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

                main.Visibility = Visibility.Visible;
                main.ImportM(AccountList, menu, fName);

                return otpauth;
            }
            else
            {
                main.Visibility = Visibility.Visible;
                main.NewAccount(result.Text.ToString(), menu, fName);
                return result.Text;
            }
        }
        else
        {
            main.Visibility = Visibility.Visible;
            System.Windows.MessageBox.Show("Pas de QR code trouver");
            return "Pas de QR code trouver";
        }
    }

    #region M�thode
    private void StartSelection(Screen screen)
    {
        main.Hide();

        Rectangle screenBounds = screen.Bounds;
        using (Bitmap screenBitmap = new Bitmap(screenBounds.Width, screenBounds.Height))
        {

            using (Graphics g = Graphics.FromImage(screenBitmap))
            {
                g.CopyFromScreen(screenBounds.X, screenBounds.Y, 0, 0, screenBounds.Size);
            }

            using (Form selectionForm = new Form())
            {
                selectionForm.StartPosition = FormStartPosition.Manual;
                selectionForm.Location = screen.Bounds.Location;

                selectionForm.FormBorderStyle = FormBorderStyle.None;
                selectionForm.WindowState = FormWindowState.Maximized;
                selectionForm.Cursor = Cursors.Cross;

                PictureBox pictureBox = new PictureBox();
                Rectangle selectedRegion = new Rectangle();
                pictureBox.Dock = DockStyle.Fill;
                pictureBox.Image = screenBitmap;
                pictureBox.MouseDown += (sender, e) =>
                {
                    startPoint = e.Location;
                };
                pictureBox.MouseMove += (sender, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        ControlPaint.DrawReversibleFrame(selectedRegion, Color.Black, FrameStyle.Dashed);
                        selectedRegion = new Rectangle(Math.Min(startPoint.X, e.X), Math.Min(startPoint.Y, e.Y), Math.Abs(startPoint.X - e.X), Math.Abs(startPoint.Y - e.Y));
                        ControlPaint.DrawReversibleFrame(selectedRegion, Color.Black, FrameStyle.Dashed);
                    }
                };
                pictureBox.MouseUp += (sender, e) =>
                {
                    selectionForm.Close();
                    Bitmap selectedBitmap = new Bitmap(selectedRegion.Width, selectedRegion.Height);
                    Graphics selectedGraphics = Graphics.FromImage(selectedBitmap);
                    selectedGraphics.DrawImage(screenBitmap, 0, 0, selectedRegion, GraphicsUnit.Pixel);
                    string qrCodeData = DecodeQRCode(selectedBitmap);
                };

                selectionForm.Controls.Add(pictureBox);
                selectionForm.ShowDialog();
            }
        }
    }*/

    #region Bouton
    private void Capture(object sender, EventArgs e)
    {
        scannerView.IsVisible = true;
        QRcapture.IsVisible = false;
        Import.IsVisible = false;
    }

    private void Import_Click(object sender, EventArgs e)
    {
        //List<string> vide = new List<string>();
        //main.ImportM(vide, menu, fName);
    }

    /*private void Confirm(object sender, EventArgs e)
    {
        string normalizeText = SecretKey.Text.Normalize(NormalizationForm.FormD);
        if (SecretKey.Text == normalizeText && SecretKey.Text.All(char.IsLetterOrDigit))
        {
            string otpauth = "otpauth://totp/?secret=" + SecretKey.Text + "&digits=6&period=30";
            main.NewAccount(otpauth, menu, fName);
        }
        else
        {
            System.Windows.MessageBox.Show("La cl� que vous avez rentrez n'est pas correct");
            SecretKey.Clear();
        }
    }*/

    private async void Back(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
    #endregion

    private void OnBarcodeDetected(object sender, BarcodeDetectionEventArgs e)
    {
        // Emp�che le scanner de d�tecter plusieurs fois le m�me code
        scannerView.IsDetecting = false;

        // R�cup�re le contenu du QR Code
        string codeScanne = e.Results[0].Value;

        // Affiche une alerte avec les donn�es scann�es
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            DecodeQRCode(codeScanne);       
        });
    }

    #region M�thode
    private string DecodeQRCode(string result)
    {
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

                //main.Visibility = Visibility.Visible;
                main.ImportM(AccountList, menu, fName);

                return otpauth;
            }
            else
            {
                //main.Visibility = Visibility.Visible;
                main.NewAccount(result.ToString(), menu, fName);
                return result;
            }
        }
        else
        {
            //main.Visibility = Visibility.Visible;
            DisplayAlert("Erreur","Pas de QR code trouver","OK");
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
            return "Rien a migr�";
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
            return "Rien a migr�";
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