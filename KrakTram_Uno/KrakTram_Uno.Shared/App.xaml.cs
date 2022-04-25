
// using Android.Locations;
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

using vb14 = VBlib.pkarlibmodule14;

namespace KrakTram
{
     partial class App : Application
    {
        private Window _window;

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
#if __ANDROID__
        // ConfigureFilters(Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory);
        InitializeLogging(); // nowsze Uno

#endif
        this.InitializeComponent();
        // this.Suspending += OnSuspending; // komentuje, bo OnSuspending jest i tak puste
    }


    protected Frame OnLaunchFragment(Window win)
    {
        Frame mRootFrame = win.Content as Frame;

        //' Do not repeat app initialization when the Window already has content,
        //' just ensure that the window is active

        if (mRootFrame is null)
        {
            //' Create a Frame to act as the navigation context and navigate to the first page
            mRootFrame = new Frame();

            mRootFrame.NavigationFailed += OnNavigationFailed;

            //' PKAR added wedle https://stackoverflow.com/questions/39262926/uwp-hardware-back-press-work-correctly-in-mobile-but-error-with-pc
            mRootFrame.Navigated += OnNavigatedAddBackButton;
            Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested += OnBackButtonPressed;

            //' Place the frame in the current Window
            Window.Current.Content = mRootFrame;

            p.k.InitLib(null);
        }

        return mRootFrame;
    }

    #region "Back button"

    private void OnNavigatedAddBackButton(object sender, NavigationEventArgs e)
    {
        try
        {
            Frame oFrame = sender as Frame;
            if (oFrame is null) return;

            Windows.UI.Core.SystemNavigationManager oNavig = Windows.UI.Core.SystemNavigationManager.GetForCurrentView();


            if (oFrame.CanGoBack)
                oNavig.AppViewBackButtonVisibility = Windows.UI.Core.AppViewBackButtonVisibility.Visible;
            else
                oNavig.AppViewBackButtonVisibility = Windows.UI.Core.AppViewBackButtonVisibility.Collapsed;

            return;
        }
        catch (Exception ex)
        {
            p.k.CrashMessageExit("@OnNavigatedAddBackButton", ex.Message);
        }
    }

    private void OnBackButtonPressed(object sender, Windows.UI.Core.BackRequestedEventArgs e)
    {
        try
        {
            (Window.Current.Content as Frame)?.GoBack();
            e.Handled = true;
        }
        catch { }
    }

    #endregion


    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {

#if DEBUG
        if (System.Diagnostics.Debugger.IsAttached)
        {
            // this.DebugSettings.EnableFrameRateCounter = true;
        }
#endif

#if NET5_0 && WINDOWS
            _window = new Window();
            _window.Activate();
#else
        _window = Windows.UI.Xaml.Window.Current;
#endif

        Frame rootFrame = OnLaunchFragment(_window);

#if !(NET5_0 && WINDOWS)
        if (args.PrelaunchActivated == false)
#endif
        {
            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                rootFrame.Navigate(typeof(MainPage), args.Arguments);
            }
            // Ensure the current window is active
            _window.Activate();
        }
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
    private async System.Threading.Tasks.Task<string> AppServiceLocalCommand(string sCommand)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        return "NO";
    }


    //  RemoteSystems, Timer
    protected override async void OnBackgroundActivated(BackgroundActivatedEventArgs args)
    {
        moTaskDeferal = args.TaskInstance.GetDeferral(); // w pkarmodule.App


        bool bNoComplete = false;
        bool bObsluzone = false;

        //' lista komend danej aplikacji
        string sLocalCmds = "";

        //' zwroci false gdy to nie jest RemoteSystem; gdy true, to zainicjalizowało odbieranie
        if (!bObsluzone) bNoComplete = RemSysInit(args, sLocalCmds);

        if (!bNoComplete) moTaskDeferal.Complete();
    }

    //' CommandLine, Toasts
    protected override async void OnActivated(IActivatedEventArgs args)
    {
        //' to jest m.in. dla Toast i tak dalej?

        //' próba czy to commandline
        if (args.Kind == ActivationKind.CommandLineLaunch)
        {

            CommandLineActivatedEventArgs commandLine = args as CommandLineActivatedEventArgs;
            CommandLineActivationOperation operation = commandLine?.Operation;
            string strArgs = operation?.Arguments;


            p.k.InitLib(strArgs.Split(' ')); // mamy command line, próbujemy zrobić z tego string() (.Net Standard 1.4)

            if (!string.IsNullOrEmpty(strArgs))
            {
                await ObsluzCommandLineAsync(strArgs);
                Window.Current.Close();
            }
            return;
        }

        p.k.InitLib(null);    // nie mamy dostępu do commandline (.Net Standard 1.4)

        //' jesli nie cmdline (a np. toast), albo cmdline bez parametrow, to pokazujemy okno
        Frame rootFrame = OnLaunchFragment(Windows.UI.Xaml.Window.Current);

        if (args.Kind == ActivationKind.ToastNotification)
            rootFrame.Navigate(typeof(MainPage));


        Window.Current.Activate();
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

    // PKAR komentuje, bo i tak nie uzywam a nie ma w UNO
    //private void OnSuspending(object sender, SuspendingEventArgs e)
    //{
    //    var deferral = e.SuspendingOperation.GetDeferral();
    //    //TODO: Save application state and stop any background activity
    //    deferral.Complete();
    //}

    #region "logging"

#if NETFX_CORE
        // previous UNO
        /// <summary>
        /// Configures global logging
        /// </summary>
        static void InitializeLogging()
        {
            // konieczne Microsoft.Extensions.Logging.Filter 1.1.2
            Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory.WithFilter(new FilterLoggerSettings
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
#else

    // nowsze Uno
    /// <summary>
    /// Configures global Uno Platform logging
    /// </summary>
    private static void InitializeLogging()
    {

        var factory = LoggerFactory.Create(builder =>
        {
#if __WASM__
                builder.AddProvider(new global::Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider());
#elif __IOS__
                builder.AddProvider(new global::Uno.Extensions.Logging.OSLogLoggerProvider());
#elif NETFX_CORE
                builder.AddDebug();
#else
            builder.AddConsole();
#endif

            // Exclude logs below this level
            builder.SetMinimumLevel(LogLevel.Information);

            // Default filters for Uno Platform namespaces
            builder.AddFilter("Uno", LogLevel.Warning);
            builder.AddFilter("Windows", LogLevel.Warning);
            builder.AddFilter("Microsoft", LogLevel.Warning);

            // Generic Xaml events
            // builder.AddFilter("Windows.UI.Xaml", LogLevel.Debug );
            // builder.AddFilter("Windows.UI.Xaml.VisualStateGroup", LogLevel.Debug );
            // builder.AddFilter("Windows.UI.Xaml.StateTriggerBase", LogLevel.Debug );
            // builder.AddFilter("Windows.UI.Xaml.UIElement", LogLevel.Debug );
            // builder.AddFilter("Windows.UI.Xaml.FrameworkElement", LogLevel.Trace );

            // Layouter specific messages
            // builder.AddFilter("Windows.UI.Xaml.Controls", LogLevel.Debug );
            // builder.AddFilter("Windows.UI.Xaml.Controls.Layouter", LogLevel.Debug );
            // builder.AddFilter("Windows.UI.Xaml.Controls.Panel", LogLevel.Debug );

            // builder.AddFilter("Windows.Storage", LogLevel.Debug );

            // Binding related messages
            // builder.AddFilter("Windows.UI.Xaml.Data", LogLevel.Debug );
            // builder.AddFilter("Windows.UI.Xaml.Data", LogLevel.Debug );

            // Binder memory references tracking
            // builder.AddFilter("Uno.UI.DataBinding.BinderReferenceHolder", LogLevel.Debug );

            // RemoteControl and HotReload related
            // builder.AddFilter("Uno.UI.RemoteControl", LogLevel.Information);

            // Debug JS interop
            // builder.AddFilter("Uno.Foundation.WebAssemblyRuntime", LogLevel.Debug );
        });

        global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;

        //#if HAS_UNO
        //            global::Uno.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter.Initialize();
        //#endif
    }
#endif

    #endregion
    #region "RemoteSystem/Background"

    private Windows.ApplicationModel.Background.BackgroundTaskDeferral moTaskDeferal = null;
    private Windows.ApplicationModel.AppService.AppServiceConnection moAppConn;
    private string msLocalCmdsHelp = "";

    private void RemSysOnServiceClosed(Windows.ApplicationModel.AppService.AppServiceConnection appCon, Windows.ApplicationModel.AppService.AppServiceClosedEventArgs args)
    {
        if (appCon != null) appCon.Dispose();
        if (moTaskDeferal != null)
        {
            moTaskDeferal.Complete();
            moTaskDeferal = null;
        }
    }

    private void RemSysOnTaskCanceled(Windows.ApplicationModel.Background.IBackgroundTaskInstance sender, Windows.ApplicationModel.Background.BackgroundTaskCancellationReason reason)
    {
        if (moTaskDeferal != null)
        {
            moTaskDeferal.Complete();
            moTaskDeferal = null;
        }
    }

    ///<summary>
    ///do sprawdzania w OnBackgroundActivated
    ///jak zwróci True, to znaczy że nie wolno zwalniać moTaskDeferal !
    ///sLocalCmdsHelp: tekst do odesłania na HELP
    ///</summary>
    public bool RemSysInit(BackgroundActivatedEventArgs args, string sLocalCmdsHelp)
    {
        Windows.ApplicationModel.AppService.AppServiceTriggerDetails oDetails =
         args.TaskInstance.TriggerDetails as Windows.ApplicationModel.AppService.AppServiceTriggerDetails;
        if (oDetails is null) return false;

        msLocalCmdsHelp = sLocalCmdsHelp;

        args.TaskInstance.Canceled += RemSysOnTaskCanceled;
        moAppConn = oDetails.AppServiceConnection;
        moAppConn.RequestReceived += RemSysOnRequestReceived;
        moAppConn.ServiceClosed += RemSysOnServiceClosed;
        return true;
    }

    public async System.Threading.Tasks.Task<string> CmdLineOrRemSysAsync(string sCommand)
    {
        string sResult = p.k.AppServiceStdCmd(sCommand, msLocalCmdsHelp);
        if (string.IsNullOrEmpty(sResult))
            sResult = await AppServiceLocalCommand(sCommand);

        return sResult;
    }

    public async System.Threading.Tasks.Task ObsluzCommandLineAsync(string sCommand)

    {
        Windows.Storage.StorageFolder oFold = Windows.Storage.ApplicationData.Current.TemporaryFolder;
        if (oFold is null) return;

        string sLockFilepathname = System.IO.Path.Combine(oFold.Path, "cmdline.lock");
        string sResultFilepathname = System.IO.Path.Combine(oFold.Path, "stdout.txt");

        try
        {
            System.IO.File.WriteAllText(sLockFilepathname, "lock");
        }
        catch
        {
            return;
        }

        string sResult = await CmdLineOrRemSysAsync(sCommand);
        if (string.IsNullOrEmpty(sResult))
            sResult = "(empty - probably unrecognized command)";

        System.IO.File.WriteAllText(sResultFilepathname, sResult);

        System.IO.File.Delete(sLockFilepathname);
    }

    private async void RemSysOnRequestReceived(Windows.ApplicationModel.AppService.AppServiceConnection sender, Windows.ApplicationModel.AppService.AppServiceRequestReceivedEventArgs args)
    {
        // 'Get a deferral so we can use an awaitable API to respond to the message

        string sStatus;
        string sResult = "";
        Windows.ApplicationModel.AppService.AppServiceDeferral messageDeferral = args.GetDeferral();

        if (vb14.GetSettingsBool("remoteSystemDisabled"))
        {
            sStatus = "No permission";
        }
        else
        {
            Windows.Foundation.Collections.ValueSet oInputMsg = args.Request.Message;

            sStatus = "ERROR while processing command";

            if (oInputMsg.ContainsKey("command"))
            {

                String sCommand = (string)oInputMsg["command"];
                sResult = await CmdLineOrRemSysAsync(sCommand);
            }

            if (sResult != "") sStatus = "OK";
        }

        Windows.Foundation.Collections.ValueSet oResultMsg = new Windows.Foundation.Collections.ValueSet();
        oResultMsg.Add("status", sStatus);
        oResultMsg.Add("result", sResult);

        await args.Request.SendResponseAsync(oResultMsg);

        messageDeferral.Complete();
        moTaskDeferal.Complete();
    }


    #endregion


#pragma warning disable CA2211 // Non-constant fields should not be visible
    public static VBlib.Przystanki oStops = new VBlib.Przystanki(Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path);
        public static VBlib.FavStopList oFavour = new VBlib.FavStopList(Windows.Storage.ApplicationData.Current.LocalFolder.Path);
        //public static double mdLat = 100;
        //public static double mdLong;
        public static Windows.Devices.Geolocation.BasicGeoposition mPoint = p.k.NewBasicGeoposition(100,0);
        public static double mSpeed;
        public static bool mbGoGPS = false;
        public static double mMaxOdl = 20;
        public static string msCat = "tram";
        public static VBlib.ListaOdjazdow moOdjazdy = new VBlib.ListaOdjazdow();
#pragma warning restore CA2211 // Non-constant fields should not be visible

        public static async System.Threading.Tasks.Task CheckLoadStopListAsync(bool bForceLoad = false)
        {
            await oStops.LoadOrImport(bForceLoad, p.k.NetIsIPavailable(false));
        }


        public static async System.Threading.Tasks.Task<Windows.Devices.Geolocation.BasicGeoposition> GetCurrentPointAsync()
        {
            //Point oPoint = new Point(); // = default(Point);

            mSpeed = vb14.GetSettingsInt("walkSpeed", 4);

            //oPoint.X = 50.01; // 1985 ' latitude - dane domku, choc mała precyzja
            //oPoint.Y = 19.97; // 7872   dla Android dodałem drugą cyfrę po kropce, żeby default miał tramwaje

            Windows.Devices.Geolocation.GeolocationAccessStatus rVal;
            rVal = await Windows.Devices.Geolocation.Geolocator.RequestAccessAsync();
            if (rVal != Windows.Devices.Geolocation.GeolocationAccessStatus.Allowed)
            {
                // If Not GetSettingsBool("noGPSshown") Then
                await vb14.DialogBoxResAsync("resErrorNoGPSAllowed");
                // SetSettingsBool("noGPSshown", True)
                // End If
                return p.k.GetDomekGeopos(2); // oPoint;
            }

            // https://stackoverflow.com/questions/33865445/gps-location-provider-requires-access-fine-location-permission-for-android-6-0/33866959'
            Windows.Devices.Geolocation.Geolocator oDevGPS = new Windows.Devices.Geolocation.Geolocator();
            TimeSpan oCacheTime = new TimeSpan(0, 0, 30);  // minuta ≈ 80 m (ale nie autobusem! wtedy 400 m)
            TimeSpan oTimeout = new TimeSpan(0, 0, 7);    // timeout 

            Windows.Devices.Geolocation.Geoposition oPos = null;
            oDevGPS.DesiredAccuracyInMeters = (uint)vb14.GetSettingsInt("gpsPrec", p.k.GetPlatform(75, 100, 75, 75, 75)); // dla 4 km/h; 100 m = 90 sec, 75 m = 67 sec
            string sErr = "";

            try
            {
                oPos = await oDevGPS.GetGeopositionAsync(oCacheTime, oTimeout); // UNO "firmowe" nie protestuje, ale ignoruje te dwa parametry
                //oPos = location.ToGeoposition();
                //oPoint.X = oPos.Coordinate.Point.Position.Latitude;
                //oPoint.Y = oPos.Coordinate.Point.Position.Longitude;

                double dSpeed;
                // 2018.11.13: dodaję: andalso hasvalue
                if (oPos.Coordinate.Speed != null && oPos.Coordinate.Speed.HasValue)
                {
                    if (!double.IsNaN(oPos.Coordinate.Speed.Value))
                    {
                        if (oPos.Coordinate.Speed != 0)
                        {
                            dSpeed = oPos.Coordinate.Speed.Value / 3.6;    // z m/s na km/h
                            mSpeed = Math.Max(dSpeed, dSpeed - 1);   // co ja tu miałem na myśli?? konwersja?
                            mSpeed = Math.Max(dSpeed, 1);            // nie wiem, więc daję to (na wszelki wypadek - ograniczenie na 1 km/h)
                        }
                    }
                }

                return oPos.Coordinate.Point.Position;

            }
            catch (Exception e)
            {
                sErr = e.Message;
            }

#if __ANDROID__ && false
            oDevGPS.Destruktor();
            // oDevGPS.Dispose();
#endif

                // po tym wyskakuje później z błędem, więc może oPoint jest zepsute?
                // dodaję zarówno ustalenie oPoint i mSpeed na defaulty, jak i Speed.HasValue
                await vb14.DialogBoxResAsync("resErrorGettingPos");

                mSpeed = vb14.GetSettingsInt("walkSpeed", 4);

            return p.k.GetDomekGeopos(2);
        }

    }
}


