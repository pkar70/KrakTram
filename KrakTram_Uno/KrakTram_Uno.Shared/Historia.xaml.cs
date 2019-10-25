using System;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace KrakTram
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
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
            uiTitle.Text = p.k.GetLangString("resHistoriaTitle") + " " + iRok;
        }

        private void Page_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (p.k.IsThisMoje())
                uiCommandBar.Visibility = Windows.UI.Xaml.Visibility.Visible;

            UstawSlider();
            mbBlockSlider = false;

            uiSlider.Value = MAX_ROK;
        }

        private async System.Threading.Tasks.Task<bool> WczytajPicek(int iRok)
        {
            if (iRok > MAX_ROK)
                return false;
            if (iRok < MIN_ROK)
                return false;

            Uri oPicUri = new Uri("ms-appx:///Assets/" + iRok + ".gif");
            Windows.Storage.StorageFile oFile;
            try
            {   // Uno tu protestuje (unimplemented), ale przecież ta strona jest tylko pod UWP
                oFile = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(oPicUri);
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
                if (await WczytajPicek(iRok))
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
