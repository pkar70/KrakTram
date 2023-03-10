using System;
using System.Linq;

using vb14 = VBlib.pkarlibmodule14;
using static p.Extensions;

namespace KrakTram
{
    public sealed partial class Historia : Windows.UI.Xaml.Controls.Page
    {
        public Historia()
        {
            this.InitializeComponent();
        }
        private const int MIN_ROK = 1882;
        private int _MaxRok = 2024;
        private int _InitRok = 2022;

        private bool mbBlockSlider = true;

        private void UstawGraniceSlider()
        {
            uiSlider.Minimum = MIN_ROK;
            uiSlider.Maximum = _MaxRok;
        }

        private async void Page_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (vb14.GetSettingsBool("pkarmode", p.k.IsThisMoje()))
                uiCommandBar.Visibility = Windows.UI.Xaml.Visibility.Visible;

            await PoszukajLatKrancowychAsync();  // dla Windows nie trzeba ustawiać MIN/INIT

            UstawGraniceSlider();
            mbBlockSlider = false;

            uiSlider.Value = _InitRok;
        }


        private async System.Threading.Tasks.Task PoszukajLatKrancowychAsync()
        {
            // init rok, idziemy od Date.Now w dół
            for(_InitRok = DateTime.Now.Year; _InitRok > MIN_ROK; _InitRok--)
            {
                if (await GetFileFromYearAsync(_InitRok) != null) break;
            }

            // max rok, idziemy od aktualnej w górę (max. 10 lat)
            for (_MaxRok = DateTime.Now.Year+10; _MaxRok > _InitRok; _MaxRok--)
            {
                if (await GetFileFromYearAsync(_MaxRok) != null) break;
            }
        }

#if __ANDROID__
        private async System.Threading.Tasks.Task<Windows.Storage.StorageFile> AndroidGetFileFromApplicationUriAsync(Uri uri)
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
        /// <summary>
        /// zwraca Uri które można wykorzystać jako oBmp.UriSource albo null
        /// </summary>
        /// <param name="iRok"></param>
        /// <returns></returns>
        private async System.Threading.Tasks.Task<Windows.Storage.StorageFile> GetFileFromYearAsync(int iRok)
        {

            if (iRok > _MaxRok)
                return null;
            if (iRok < MIN_ROK)
                return null;

            Uri oPicUri = new Uri("ms-appx:///Assets/" + iRok + ".gif");
            Windows.Storage.StorageFile oFile;
            try
            {
#if NETFX_CORE 
                oFile = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(oPicUri);
#else
                // zakładam że jak nie UWP to Android
                oFile = await AndroidGetFileFromApplicationUriAsync(oPicUri);
#endif
            }
            catch
            {
                return null;
            }

            return oFile;
        }


        private async System.Threading.Tasks.Task<bool> WczytajPicekAsync(int iRok)
        {
            if (iRok > _MaxRok)
                return false;
            if (iRok < MIN_ROK)
                return false;

            //Uri oPicUri = new Uri("ms-appx:///Assets/" + iRok + ".gif");
            Windows.Storage.StorageFile oFile = await GetFileFromYearAsync(iRok);
            if (oFile == null) return false;

            Windows.UI.Xaml.Media.Imaging.BitmapImage oBmp = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
            oBmp.SetSource(await oFile.OpenAsync(Windows.Storage.FileAccessMode.Read));
            uiPic.Source = oBmp;
            return true;
        }

        private void uiSlider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (mbBlockSlider) 
                return;

            PrzestawRokAsync(0);
        }

        private void uiOpoznienia_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.Navigate(typeof(Opoznienia));
        }

        private async System.Threading.Tasks.Task PrzestawRokAsync(int initRokChange)
        {
            int iRok = (int)uiSlider.Value + initRokChange;
            int iRokDelta = initRokChange;
            if (iRokDelta == 0) iRokDelta = 1;

            // w petli probuj ustawic obrazek, az bedzie
            while (iRok <= _MaxRok)
            {
                if (await WczytajPicekAsync(iRok))
                    break;
                iRok += iRokDelta;
            }

            uiRokMinus.IsEnabled = true;
            uiRokPlus.IsEnabled = true;

            if (iRok <= MIN_ROK)
                uiRokMinus.IsEnabled = false;

            if(iRok >= _MaxRok)
                uiRokPlus.IsEnabled = false;

            uiTitle.Text = vb14.GetLangString("resHistoriaTitle") + " " + iRok;

            // czyli wywołanie z guzików, plus/minus - aktualizujemy Slider
            if(initRokChange != 0)
            {
                mbBlockSlider = true;
                uiSlider.Value = iRok;
                mbBlockSlider = false;
            }
        }

        private void uiRokMinus_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            PrzestawRokAsync(-1);
        }
        private void uiRokPlus_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            PrzestawRokAsync(+1);
        }

    }

}
