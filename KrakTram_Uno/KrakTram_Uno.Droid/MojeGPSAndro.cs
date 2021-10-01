
#if false

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Android.Locations;
using Android.OS;
using Android.Runtime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using System.Threading;

using Android.Content.PM;
using Android.Support.V4.App;
//using Android.Support.V4.Content; // potrzebne dla Uno < 3.0
using Uno.Extensions;

//namespace Windows.Devices.Geolocation
//{
//    public enum PositionSource
//    {
//        Cellular,
//        Satellite,
//        WiFi,
//        IPAddress,
//        Unknown,
//        Default,
//        Obfuscated,
//    }
//}


namespace KrakTram12
{
    public sealed partial class MyGeolocator // : Java.Lang.Object, ILocationListener
    {
        private static LocationManager _locationManager;
        // private string _locationProvider;

        private uint _reportInterval = 1000; // pkar
        private double _movementThreshold = 0;// pkar
        private uint? _desiredAccuracyInMeters;
        private Criteria _locationCriteria = new Android.Locations.Criteria() { HorizontalAccuracy = Accuracy.Medium };

        // wyciągnięte tu, z {set}, by latwiej przełącząc między mojSluchacz a MyGeolocator
        private void RemoveUpdates() => _locationManager?.RemoveUpdates(mojSluchacz);

        private void RequestUpdates()
        {
            // => _locationManager.RequestLocationUpdates(_locationProvider, _reportInterval, (float)_movementThreshold, mojSluchacz, Looper.MainLooper);
            if (_desiredAccuracyInMeters.HasValue)
            {
                if (_desiredAccuracyInMeters.Value < 100)
                {
                    _locationCriteria.HorizontalAccuracy = Accuracy.High;
                }
                else
                    if (_desiredAccuracyInMeters.Value < 500)
                {
                    _locationCriteria.HorizontalAccuracy = Accuracy.Medium;
                }
                else
                {
                    _locationCriteria.HorizontalAccuracy = Accuracy.Low;
                }

            }
            else
            {
                _locationCriteria.HorizontalAccuracy = Accuracy.Medium;
            }

            _locationManager.RequestLocationUpdates(_reportInterval, (float)_movementThreshold, _locationCriteria, mojSluchacz, Looper.MainLooper);
        }

        public uint ReportInterval
        { // pkar
            get => _reportInterval;
            set
            {
                _reportInterval = value;
                RemoveUpdates();
                RequestUpdates();
            }
        }

        public double MovementThreshold
        { // pkar
            get => _movementThreshold;
            set
            {
                _movementThreshold = value;
                RemoveUpdates();
                RequestUpdates();
            }
        }

        public uint? DesiredAccuracyInMeters
        {
            get => _desiredAccuracyInMeters;
            set
            {
                _desiredAccuracyInMeters = value;
                RemoveUpdates();
                // _locationManager = InitializeLocationProvider();
                RequestUpdates();
            }
        }

        #region MyListener

        public class MyLocListen : Java.Lang.Object, ILocationListener
        {
            //IntPtr IJavaObject.Handle => throw new NotImplementedException();

            //void IDisposable.Dispose()
            //{
            //    //throw new NotImplementedException();
            //}

            public void OnLocationChanged(Location location)
            {
                DateTime date = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                date = date.AddMilliseconds(location.Time);
                if (date.AddMinutes(1) > DateTime.UtcNow)
                { // z ostatniej minuty tylko się liczy
                    mbZmiany = true;
                    moLocat = location;
                }
            }

            public void OnProviderDisabled(string provider)
            {
                throw new System.OperationCanceledException();
            }

            public void OnProviderEnabled(string provider)
            {
                //throw new NotImplementedException();
            }

            public void OnStatusChanged(string provider, Availability status, Bundle extras)
            {
                // This method was deprecated in API level 29 (Android 10). This callback will never be invoked.
            }
        }


        MyLocListen mojSluchacz = new MyLocListen();
        #endregion 

        public MyGeolocator()
        {
            // _locationManager = InitializeLocationProvider();
            _locationManager = (LocationManager)Android.App.Application.Context.GetSystemService(Android.Content.Context.LocationService);

            // pkar, tu tez zmiana, bylo 0/0
            _reportInterval = 0;
            _movementThreshold = 0;

            RequestUpdates();
            //LocationListener locationListener = new LocationListener()
            // https://developer.android.com/reference/android/location/LocationListener
        }

        ~MyGeolocator()
        {
            try
            {
                RemoveUpdates();
            }
            catch
            { }

            try
            {
                _locationManager.Dispose();
            }
            catch
            { }
        }

        public void Destruktor()
        {
            try
            {
                RemoveUpdates();
            }
            catch
            { }

            try
            {
                //_locationManager.Dispose();
            }
            catch
            { }

        }

        public Task<Windows.Devices.Geolocation.Geoposition> GetGeopositionAsync()
            => GetGeopositionAsync(TimeSpan.FromHours(2), TimeSpan.FromSeconds(60));


        public async Task<Windows.Devices.Geolocation.Geoposition> GetGeopositionAsync(TimeSpan maximumAge, TimeSpan timeout)
        {
            var providers = _locationManager.GetProviders(_locationCriteria, true);
            int bestAccuracy = 10000;
            Location bestLocation = null;

            // sprawdz najlepsza aktualną
            // - uwzglednij czas
            // - oraz accuracy?

            foreach (string locationProvider in providers)
            {
                var location = _locationManager.GetLastKnownLocation(locationProvider);
                if (location != null)
                {
                    // check how old is this fix
                    DateTime date = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    date = date.AddMilliseconds(location.Time);
                    if (date.AddMilliseconds(maximumAge.TotalMilliseconds) > DateTime.UtcNow)
                    {   // can be used
                        if (location.HasAccuracy)
                        {
                            if (location.Accuracy < bestAccuracy)
                            {
                                bestAccuracy = (int)location.Accuracy;
                                bestLocation = location;
                            }
                        }
                        else
                            bestLocation = location;
                    }
                }
            }

            if (bestLocation != null)
            {
                RemoveUpdates();
                return bestLocation.ToGeoPosition();
            }

            // czekamy na fix
            for (int i = (int)(timeout.TotalMilliseconds / 250.0); i > 0; i--)
            {
                await Task.Delay(250);
                if (mbZmiany)
                {
                    RemoveUpdates();
                    return moLocat.ToGeoPosition();
                }
            }

            //    }
            //    //BroadcastStatus(PositionStatus.Initializing);
            //    var location = _locationManager.GetLastKnownLocation(_locationProvider);
            //if (location != null)
            //{
            //    // check how old is this fix
            //    DateTime date = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            //    date = date.AddMilliseconds(location.Time);
            //    if (date.AddMilliseconds(maximumAge.TotalMilliseconds) > DateTime.UtcNow)
            //    {
            //        // BroadcastStatus(PositionStatus.Ready);
            //        return location.ToGeoPosition();
            //    }
            //}


            //// check if provider is enabled
            //if (!_locationManager.IsProviderEnabled(_locationProvider))
            //    throw new ApplicationException();

            //// jesli nie ma fix, to poczekaj na niego
            //for (int i = (int)(timeout.TotalMilliseconds / 250.0); i > 0; i--)
            //{
            //    await Task.Delay(250);
            //    location = _locationManager.GetLastKnownLocation(_locationProvider);
            //    if (location != null)
            //    {
            //        // check how old is this fix - should not be required?
            //        DateTime date = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            //        date = date.AddMilliseconds(location.Time);
            //        if (date.AddMilliseconds(maximumAge.TotalMilliseconds) > DateTime.UtcNow)
            //        {
            //            //BroadcastStatus(PositionStatus.Ready);
            //            return location.ToGeoPosition();
            //        }
            //    }
            //}

            //BroadcastStatus(PositionStatus.Disabled);
            RemoveUpdates();
            throw new TimeoutException();

        }

        public static async Task<Windows.Devices.Geolocation.GeolocationAccessStatus> RequestAccessAsync()
        {
            // check if location is enabled
            //var locationManager = (LocationManager)global::Android.App.Application.Context.GetSystemService(Android.Content.Context.LocationService);
            //if (!locationManager.IsLocationEnabled)
            //    return Windows.Devices.Geolocation.GeolocationAccessStatus.Denied;
            // **Java.Lang.NoSuchMethodError:** 'no non-static method "Landroid/location/LocationManager;.isLocationEnabled()Z"'

            // below 6.0 (API 23), permission are granted
            if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.M)
            {
                return Windows.Devices.Geolocation.GeolocationAccessStatus.Allowed;
            }

            // do we have declared this permission in Manifest?
            // it could be also Coarse, without GPS
            // https://github.com/xamarin/Essentials/blob/master/Xamarin.Essentials/Permissions/Permissions.android.cs
            Android.Content.Context context = Android.App.Application.Context;
            Android.Content.PM.PackageInfo packageInfo =
                context.PackageManager.GetPackageInfo(context.PackageName, PackageInfoFlags.Permissions);
            var requestedPermissions = packageInfo?.RequestedPermissions;
            if (requestedPermissions is null)
                return Windows.Devices.Geolocation.GeolocationAccessStatus.Denied;

            bool bInManifest = false;
            foreach (string oPerm in requestedPermissions)
            {
                if (oPerm.Equals(Android.Manifest.Permission.AccessFineLocation, StringComparison.OrdinalIgnoreCase))
                    bInManifest = true;

            }

            if (!bInManifest)
                return Windows.Devices.Geolocation.GeolocationAccessStatus.Denied;


            // check if permission is granted
            if (Android.Support.V4.Content.ContextCompat.CheckSelfPermission(Uno.UI.ContextHelper.Current, Android.Manifest.Permission.AccessFineLocation)
                    == Android.Content.PM.Permission.Granted)
            {
                return Windows.Devices.Geolocation.GeolocationAccessStatus.Allowed;
            }

            // zamiast dialogu
            var tcs = new TaskCompletionSource<Uno.UI.BaseActivity.RequestPermissionsResultWithResultsEventArgs>();

            void handler(object sender, Uno.UI.BaseActivity.RequestPermissionsResultWithResultsEventArgs e)
            {

                if (e.RequestCode == 1)
                {
                    tcs.TrySetResult(e);
                }
            }

            var current = Uno.UI.BaseActivity.Current;
            //ActivityCompat.RequestPermissions(current, new[] { Android.Manifest.Permission.AccessFineLocation }, 1);
            try
            {
                current.RequestPermissionsResultWithResults += handler;

                ActivityCompat.RequestPermissions(Uno.UI.BaseActivity.Current, new[] { Android.Manifest.Permission.AccessFineLocation }, 1);

                var result = await tcs.Task;
                if (result.GrantResults.Length < 1)
                    return Windows.Devices.Geolocation.GeolocationAccessStatus.Denied;
                if (result.GrantResults[0] == Permission.Granted)
                    return Windows.Devices.Geolocation.GeolocationAccessStatus.Allowed;

            }
            finally
            {
                current.RequestPermissionsResultWithResults -= handler;
            }


            return Windows.Devices.Geolocation.GeolocationAccessStatus.Denied;
            //Android.App.Activity oAct;
            //ActivityCompat.RequestPermissions(Platform.GetCurrentActivity(true), androidPermissions, requestCode);

        }

        //private LocationManager InitializeLocationProvider()
        //{
        //    var locationManager = (LocationManager)global::Android.App.Application.Context.GetSystemService(Android.Content.Context.LocationService);

        //    var criteriaForLocationService = new Criteria();
        //    {
        //        if (_desiredAccuracyInMeters.HasValue)
        //        {
        //            if (_desiredAccuracyInMeters.Value < 100)
        //            {
        //                criteriaForLocationService.HorizontalAccuracy = Accuracy.High; 
        //            }
        //            else
        //                if (_desiredAccuracyInMeters.Value < 500)
        //                {
        //                    criteriaForLocationService.HorizontalAccuracy = Accuracy.Medium;
        //                }
        //                else
        //                {
        //                    criteriaForLocationService.HorizontalAccuracy = Accuracy.Low;
        //                }

        //        }
        //        else
        //        {
        //            criteriaForLocationService.HorizontalAccuracy = Accuracy.Medium;
        //        }

        //        // <100 m :Accuracy.High
        //        // >500 m: Accuracy.Low
        //        // albo: Accuracy.Medium
        //    };

        //    _locationProvider = locationManager.GetBestProvider(criteriaForLocationService, true);

        //    return locationManager;
        //}

        //partial void StartPositionChanged()
        //{
        //    BroadcastStatus(PositionStatus.Initializing);
        //}

        static bool mbZmiany = false;
        static Location moLocat;

        //void ILocationListener.OnLocationChanged(Location location)
        //{
        //    mbZmiany = true;
        //    moLocat = location;
        //    //BroadcastStatus(PositionStatus.Ready);
        //    // this._positionChanged?.Invoke(this, new PositionChangedEventArgs(location.ToGeoPosition()));
        //}

        //void ILocationListener.OnProviderDisabled(string provider)
        //{
        //}

        //void ILocationListener.OnProviderEnabled(string provider)
        //{
        //}

        //void ILocationListener.OnStatusChanged(string provider, [GeneratedEnum] Availability status, Bundle extras)
        //{
        //}
    }

    static class Extensions
    {
        private const uint Wgs84SpatialReferenceId = 4326;

        public static Windows.Devices.Geolocation.Geoposition ToGeoPosition(this Location location)
        {
            // pkar
            double? geoheading = null;
            if (location.HasBearing)
            {
                geoheading = location.Bearing;
            }

            // pkar
            //Windows.Devices.Geolocation.PositionSource posSource;
            // zakomentowane, bo 3092 nie ma tych typow, wiec nie umie tego przetworzyc
            //switch (location.Provider)
            //{
            //    case Android.Locations.LocationManager.NetworkProvider:
            //        posSource = Windows.Devices.Geolocation.PositionSource.Cellular;    // cell, wifi
            //        break;
            //    case Android.Locations.LocationManager.PassiveProvider:
            //        posSource = Windows.Devices.Geolocation.PositionSource.Unknown;  // inni
            //        break;
            //    case Android.Locations.LocationManager.GpsProvider:
            //        posSource = Windows.Devices.Geolocation.PositionSource.Satellite;
            //        break;
            //    default:
            //        // np.: "fused" - polaczenie wszystkich, Google Play (na tablecie to jest)
            //        posSource = Windows.Devices.Geolocation.PositionSource.Unknown;
            //        break;
            //}

            // pkar
            // gdy jest new w new etc., to w Point wspolrzedne sa 0,0,0!
            Windows.Devices.Geolocation.BasicGeoposition basicGeoposition; // = new BasicGeoposition();
            basicGeoposition.Altitude = location.Altitude;
            basicGeoposition.Latitude = location.Latitude;
            basicGeoposition.Longitude = location.Longitude;

            Windows.Devices.Geolocation.Geopoint geopoint = new Windows.Devices.Geolocation.Geopoint(basicGeoposition,
                        Windows.Devices.Geolocation.AltitudeReferenceSystem.Ellipsoid,
                        Wgs84SpatialReferenceId
                    );

            double? locVertAccuracy = null;
            // VerticalAccuracy is since API 26
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            {
                // HasVerticalAccuracy and VerticalAccuracyMeters are marked as "to be added"
                if (location.HasVerticalAccuracy)
                {
                    locVertAccuracy = location.VerticalAccuracyMeters;
                }
            }


            Windows.Devices.Geolocation.Geoposition geopos = new Windows.Devices.Geolocation.Geoposition(
                new Windows.Devices.Geolocation.Geocoordinate(
                    latitude: location.Latitude,
                    longitude: location.Longitude,
                    altitude: location.Altitude,
                    timestamp: FromUnixTime(location.Time),
                    speed: location.HasSpeed ? location.Speed : 0,
                    point: geopoint,
                    accuracy: location.HasAccuracy ? location.Accuracy : 0,
                    altitudeAccuracy: locVertAccuracy,
                    heading: geoheading//,
                                       //                    positionSource: posSource
                )
            );



            // nie ustawia:
            // heading (double?) ,
            // position source, [enum]
            // position sourcetimestamp (DateTimeOffset?),
            //satelitedata


            return geopos;
        }


        private static DateTimeOffset FromUnixTime(long time)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddMilliseconds(time);
        }
    }
}
#endif