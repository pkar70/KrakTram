using System;
using System.Linq;

using vb14 = VBlib.pkarlibmodule14;


namespace KrakTram
{
    public sealed partial class Historia : Windows.UI.Xaml.Controls.Page
    {
        public Historia()
        {
            this.InitializeComponent();
        }
        private const int MIN_ROK = 1882;
        private const int MAX_ROK = 2015;

        private bool mbBlockSlider = true;

        private void UstawSlider()
        {
            uiSlider.Minimum = MIN_ROK;
            uiSlider.Maximum = MAX_ROK;
        }

        private void UstawTitle(int iRok)
        {
            uiTitle.Text = vb14.GetLangString("resHistoriaTitle") + " " + iRok;
        }

        private void Page_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (vb14.GetSettingsBool("pkarmode", p.k.IsThisMoje()))
                uiCommandBar.Visibility = Windows.UI.Xaml.Visibility.Visible;

            UstawSlider();
            mbBlockSlider = false;

            uiSlider.Value = MAX_ROK;
        }

#if __ANDROID__
        private async System.Threading.Tasks.Task<Windows.Storage.StorageFile> AndroidGetFileFromApplicationUri(Uri uri)
        {
            // "ms-appx:///Assets/" + iRok + ".gif"
            // "__" + iRok + ".gif" w pkar.KrakTram.apk\res\drawable-nodpi-v4\

            if (uri.Scheme != "ms-appx")
            {
                throw new InvalidOperationException("Uri is not using the ms-appx scheme");
            }

            string sFilename = uri.ToString();

            if (!sFilename.StartsWith("ms-appx:///Assets/"))
            {
                throw new InvalidOperationException("Uri is not ms-appx:///Assets/");
            }

            sFilename = sFilename.Substring("ms-appx:///Assets/".Length);

            var assetMan = Android.App.Application.Context.Assets;
            if (assetMan is null)
            {
                throw new InvalidOperationException("Cannot get AssetManager");
            }

            var outputCachePath = System.IO.Path.Combine(Android.App.Application.Context.CacheDir.AbsolutePath, sFilename);

            if (!System.IO.File.Exists(outputCachePath))
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(outputCachePath));

                using System.IO.Stream fileInApk = assetMan.Open(sFilename);
                using var output = System.IO.File.OpenWrite(outputCachePath);

                await fileInApk.CopyToAsync(output);
            }

            return await Windows.Storage.StorageFile.GetFileFromPathAsync(outputCachePath);
        }

#endif
        private async System.Threading.Tasks.Task<bool> WczytajPicekAsync(int iRok)
        {
            if (iRok > MAX_ROK)
                return false;
            if (iRok < MIN_ROK)
                return false;

            Uri oPicUri = new Uri("ms-appx:///Assets/" + iRok + ".gif");
            Windows.Storage.StorageFile oFile;
            try
            {   // Uno tu protestuje (unimplemented), ale przecież ta strona jest tylko pod UWP - już nie :)
#if NETFX_CORE 
                oFile = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(oPicUri);
#else
                // zakładam że jak nie UWP to Android
                oFile = await AndroidGetFileFromApplicationUri(oPicUri);
#endif
            }
            catch
            {
                return false;
            }

            Windows.UI.Xaml.Media.Imaging.BitmapImage oBmp = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
            oBmp.UriSource = oPicUri;
            uiPic.Source = oBmp;
            return true;
        }

        private async void uiSlider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (mbBlockSlider)
                return;

            int iRok = (int)uiSlider.Value;

            // w petli probuj ustawic obrazek, az bedzie
            while (iRok <= MAX_ROK)
            {
                if (await WczytajPicekAsync(iRok))
                    break;
                iRok = iRok + 1;
            }

            // If iRok <> uiSlider.Value Then
            // mbBlockSlider = True
            // uiSlider.Value = iRok
            // mbBlockSlider = False
            // End If

            UstawTitle(iRok);
        }

        private void uiOpoznienia_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(Opoznienia));
        }
    }

}
