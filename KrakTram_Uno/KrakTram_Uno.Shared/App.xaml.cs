using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace KrakTram
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            ConfigureFilters(Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory);

            this.InitializeComponent();
            //this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
			if (System.Diagnostics.Debugger.IsAttached)
			{
				// this.DebugSettings.EnableFrameRateCounter = true;
			}
#endif
            Frame rootFrame = Windows.UI.Xaml.Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                // PKAR added wedle https://stackoverflow.com/questions/39262926/uwp-hardware-back-press-work-correctly-in-mobile-but-error-with-pc
                rootFrame.Navigated += OnNavigatedAddBackButton;
                Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested += OnBackButtonPressed;

                // not implemented in Uno, a i tak puste
                //if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                //{
                //    //TODO: Load state from previously suspended application
                //}

                // Place the frame in the current Window
                Windows.UI.Xaml.Window.Current.Content = rootFrame;
            }

#if NETFX_CORE
            if (e.PrelaunchActivated == false)
#else
            if(true)
#endif 
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Ensure the current window is active
                Windows.UI.Xaml.Window.Current.Activate();
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// not implemented in Uno, ale i bez kodu tutaj
        //private void OnSuspending(object sender, SuspendingEventArgs e)
        //{
        //    var deferral = e.SuspendingOperation.GetDeferral();
        //    //TODO: Save application state and stop any background activity
        //    deferral.Complete();
        //}


        /// <summary>
        /// Configures global logging
        /// </summary>
        /// <param name="factory"></param>
        static void ConfigureFilters(ILoggerFactory factory)
        {
            factory
                .WithFilter(new FilterLoggerSettings
                    {
                        { "Uno", LogLevel.Warning },
                        { "Windows", LogLevel.Warning },

						// Debug JS interop
						// { "Uno.Foundation.WebAssemblyRuntime", LogLevel.Debug },

						// Generic Xaml events
						// { "Windows.UI.Xaml", LogLevel.Debug },
						// { "Windows.UI.Xaml.VisualStateGroup", LogLevel.Debug },
						// { "Windows.UI.Xaml.StateTriggerBase", LogLevel.Debug },
						// { "Windows.UI.Xaml.UIElement", LogLevel.Debug },

						// Layouter specific messages
						// { "Windows.UI.Xaml.Controls", LogLevel.Debug },
						// { "Windows.UI.Xaml.Controls.Layouter", LogLevel.Debug },
						// { "Windows.UI.Xaml.Controls.Panel", LogLevel.Debug },
						// { "Windows.Storage", LogLevel.Debug },

						// Binding related messages
						// { "Windows.UI.Xaml.Data", LogLevel.Debug },

						// DependencyObject memory references tracking
						// { "ReferenceHolder", LogLevel.Debug },
					}
                )
#if DEBUG
				.AddConsole(LogLevel.Debug);
#else
                .AddConsole(LogLevel.Information);
#endif
        }


        // PKAR added wedle https://stackoverflow.com/questions/39262926/uwp-hardware-back-press-work-correctly-in-mobile-but-error-with-pc
        private void OnNavigatedAddBackButton(object sender, NavigationEventArgs e)
        {
            /* TODO ERROR: Skipped IfDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
            var oFrame = sender as Frame;
            if (oFrame == null)
                return;

            Windows.UI.Core.SystemNavigationManager oNavig = Windows.UI.Core.SystemNavigationManager.GetForCurrentView();

            if (oFrame.CanGoBack)
                oNavig.AppViewBackButtonVisibility = Windows.UI.Core.AppViewBackButtonVisibility.Visible;
            else
                oNavig.AppViewBackButtonVisibility = Windows.UI.Core.AppViewBackButtonVisibility.Collapsed;
        }

        private void OnBackButtonPressed(object sender, Windows.UI.Core.BackRequestedEventArgs e)
        {
            try
            {
                (Window.Current.Content as Frame).GoBack();
                e.Handled = true;
            }
            catch 
            {
            }
        }


        public static Przystanki oStops = new Przystanki();
        public static FavStopList oFavour = new FavStopList();
        public static double mdLat = 100;
        public static double mdLong, mSpeed;
        public static bool mbGoGPS = false;
        public static double mMaxOdl = 20;
        public static string msCat = "tram";
        public static ListaOdjazdow moOdjazdy = new ListaOdjazdow();

        public static async System.Threading.Tasks.Task CheckLoadStopList(bool bForceLoad = false)
        {
            await oStops.LoadOrImport(bForceLoad);
        }

        public static async System.Threading.Tasks.Task LoadFavList()
        {
            await oFavour.LoadOrImport();
        }

        
        public static int GPSdistanceDwa(double dLat0, double dLon0, double dLat, double dLon)
        {
            // https://stackoverflow.com/questions/28569246/how-to-get-distance-between-two-locations-in-windows-phone-8-1

            try
            {
                int iRadix = 6371000;
                double tLat = (dLat - dLat0) * Math.PI / 180;
                double tLon = (dLon - dLon0) * Math.PI / 180;
                double a = Math.Sin(tLat / 2) * Math.Sin(tLat / 2) + Math.Cos(Math.PI / 180 * dLat0) * Math.Cos(Math.PI / 180 * dLat) * Math.Sin(tLon / 2) * Math.Sin(tLon / 2);
                double c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));
                double d = iRadix * c;

                return (int)d;
            }
            catch 
            {
                return 0;
            }// nie powinno sie nigdy zdarzyc, ale na wszelki wypadek...
        }

        public static int GPSdistance(Windows.Devices.Geolocation.Geoposition oPos, double dLat, double dLon)
        {
            return App.GPSdistanceDwa(oPos.Coordinate.Point.Position.Latitude, oPos.Coordinate.Point.Position.Longitude, dLat, dLon);
        }

        public static async System.Threading.Tasks.Task<Point> GetCurrentPoint()
        {
            Point oPoint = new Point(); // = default(Point);

            mSpeed = p.k.GetSettingsInt("walkSpeed", 4);

            oPoint.X = 50.0; // 1985 ' latitude - dane domku, choc mała precyzja
            oPoint.Y = 19.9; // 7872

            Windows.Devices.Geolocation.GeolocationAccessStatus rVal;
            rVal = await Windows.Devices.Geolocation.Geolocator.RequestAccessAsync();
            if (rVal != Windows.Devices.Geolocation.GeolocationAccessStatus.Allowed)
            {
                // If Not GetSettingsBool("noGPSshown") Then
                await p.k.DialogBoxRes("resErrorNoGPSAllowed");
                // SetSettingsBool("noGPSshown", True)
                // End If
                return oPoint;
            }

            Windows.Devices.Geolocation.Geolocator oDevGPS = new Windows.Devices.Geolocation.Geolocator();

            Windows.Devices.Geolocation.Geoposition oPos;
            oDevGPS.DesiredAccuracyInMeters = (uint)p.k.GetSettingsInt("gpsPrec",75); // dla 4 km/h; 100 m = 90 sec, 75 m = 67 sec
            TimeSpan oCacheTime = new TimeSpan(0, 0, 30);  // minuta ≈ 80 m (ale nie autobusem! wtedy 400 m)
            TimeSpan oTimeout = new TimeSpan(0, 0, 5);    // timeout 
            bool bErr = false;

            try
            {
                oPos = await oDevGPS.GetGeopositionAsync(oCacheTime, oTimeout);
                oPoint.X = oPos.Coordinate.Point.Position.Latitude;
                oPoint.Y = oPos.Coordinate.Point.Position.Longitude;

                double dSpeed;
                // 2018.11.13: dodaję: andalso hasvalue
                if (oPos.Coordinate.Speed != null && oPos.Coordinate.Speed.HasValue)
                {
                    if (!double.IsNaN(oPos.Coordinate.Speed.Value ))
                    {
                        if (oPos.Coordinate.Speed != 0)
                        {
                            dSpeed = oPos.Coordinate.Speed.Value / 3.6;    // z m/s na km/h
                            mSpeed = Math.Max(dSpeed, dSpeed - 1);   // co ja tu miałem na myśli??
                            mSpeed = Math.Max(dSpeed, 1);            // nie wiem, więc daję to (na wszelki wypadek - ograniczenie na 1 km/h)
                        }
                    }
                }
            }
            catch 
            {
                bErr = true;
            }
            if (bErr)
            {
                // po tym wyskakuje później z błędem, więc może oPoint jest zepsute?
                // dodaję zarówno ustalenie oPoint i mSpeed na defaulty, jak i Speed.HasValue
                await p.k.DialogBoxRes("resErrorGettingPos");

                oPoint.X = 50.0; // 1985 ' latitude - dane domku, choc mała precyzja
                oPoint.Y = 19.9; // 7872
                mSpeed = p.k.GetSettingsInt("walkSpeed", 4);
            }

            return oPoint;
        }

        public static async System.Threading.Tasks.Task<Windows.Data.Json.JsonObject> WczytajTabliczke(string sCat, string sErrData, int iId)
        {
            string sUrl;
            if ((sCat ?? "") == "bus")
                sUrl = "http://91.223.13.70";
            else
                sUrl = "http://www.ttss.krakow.pl";
            sUrl = sUrl + "/internetservice/services/passageInfo/stopPassages/stop?mode=departure&stop=";
            string sPage = await WebPageAsync(sUrl + iId.ToString(), sErrData, false);
            if (string.IsNullOrEmpty(sPage))
                return null;

            bool bError = false;
            Windows.Data.Json.JsonObject oJson = null;
            try
            {
                oJson = Windows.Data.Json.JsonObject.Parse(sPage);
            }
            catch 
            {
                bError = true;
            }
            if (bError)
            {
                p.k.DialogBox("ERROR: JSON parsing error - tablica in " + sErrData);
                return null;
            }

            return oJson;
        }

        public static async System.Threading.Tasks.Task<string> WebPageAsync(string sUri, string sErrData, bool bNoRedir)
        {
            // string sTmp = "";

            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                p.k.DialogBoxRes("resErrorNoNetwork", sErrData);
                return "";
            }

            System.Net.Http.HttpClient oHttp;
            if (bNoRedir)
            {
            System.Net.Http.HttpClientHandler oHCH = new System.Net.Http.HttpClientHandler();
                oHCH.AllowAutoRedirect = false;
                oHttp = new System.Net.Http.HttpClient(oHCH);
            }
            else
                oHttp = new System.Net.Http.HttpClient();

            string sPage = "";


            bool bError = false;

            oHttp.Timeout = TimeSpan.FromSeconds(8);


            try
            {
                sPage = await oHttp.GetStringAsync(new Uri(sUri));
            }
            catch 
            {
                bError = true;
            }
            if (bError)
            {
                p.k.DialogBoxRes("resErrorGetHttp", sErrData);
                return "";
            }

            return sPage;
        }
    }

}


