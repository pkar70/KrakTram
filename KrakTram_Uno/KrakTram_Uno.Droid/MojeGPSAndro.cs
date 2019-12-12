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
using Android.Support.V4.Content;
using Uno.Extensions;



namespace KrakTram
{
    public sealed partial class MyGeolocator : Java.Lang.Object, ILocationListener
    {
        private static LocationManager _locationManager;
        private string _locationProvider;

        private uint _reportInterval = 10; // pkar
        private double _movementThreshold = 0;// pkar
        private uint? _desiredAccuracyInMeters;

        public uint ReportInterval
        { // pkar
            get => _reportInterval;
            set
            {
                _reportInterval = value;
                _locationManager.RemoveUpdates(this);
                _locationManager.RequestLocationUpdates(_locationProvider, _reportInterval, (float)_movementThreshold, this);
            }
        }

        public double MovementThreshold
        { // pkar
            get => _movementThreshold;
            set
            {
                _movementThreshold = value;
                _locationManager.RemoveUpdates(this);
                _locationManager.RequestLocationUpdates(_locationProvider, _reportInterval, (float)_movementThreshold, this);
            }
        }

        public uint? DesiredAccuracyInMeters
        {
            get => _desiredAccuracyInMeters;
            set
            {
                _desiredAccuracyInMeters = value;
                _locationManager.RemoveUpdates(this);
                _locationManager = InitializeLocationProvider();
                _locationManager.RequestLocationUpdates(_locationProvider, _reportInterval, (float)_movementThreshold, this);

            }
        }


        public MyGeolocator()
        {
            _locationManager = InitializeLocationProvider();
            // pkar, tu tez zmiana, bylo 0/0
            _locationManager.RequestLocationUpdates(_locationProvider, _reportInterval, (float)_movementThreshold, this);
            //LocationListener locationListener = new LocationListener()
            // https://developer.android.com/reference/android/location/LocationListener
        }

        ~MyGeolocator()
        {
            try
            {
                _locationManager.RemoveUpdates(this);
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
                _locationManager.RemoveUpdates(this);
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

        public Task<Windows.Devices.Geolocation.Geoposition> GetGeopositionAsync()
            => GetGeopositionAsync(TimeSpan.FromHours(2), TimeSpan.FromSeconds(60));


        public async Task<Windows.Devices.Geolocation.Geoposition> GetGeopositionAsync(TimeSpan maximumAge, TimeSpan timeout)
        {
            //BroadcastStatus(PositionStatus.Initializing);
            var location = _locationManager.GetLastKnownLocation(_locationProvider);
            if (location != null)
            {
                // check how old is this fix
                DateTime date = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                date = date.AddMilliseconds(location.Time);
                if (date.AddMilliseconds(maximumAge.TotalMilliseconds) > DateTime.UtcNow)
                {
                    // BroadcastStatus(PositionStatus.Ready);
                    return location.ToGeoPosition();
                }
            }


            // check if provider is enabled
            if (!_locationManager.IsProviderEnabled(_locationProvider))
                throw new ApplicationException();

            // jesli nie ma fix, to poczekaj na niego
            for (int i = (int)(timeout.TotalMilliseconds / 250.0); i > 0; i--)
            {
                await Task.Delay(250);
                location = _locationManager.GetLastKnownLocation(_locationProvider);
                if (location != null)
                {
                    // check how old is this fix - should not be required?
                    DateTime date = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    date = date.AddMilliseconds(location.Time);
                    if (date.AddMilliseconds(maximumAge.TotalMilliseconds) > DateTime.UtcNow)
                    {
                        //BroadcastStatus(PositionStatus.Ready);
                        return location.ToGeoPosition();
                    }
                }
            }

            //BroadcastStatus(PositionStatus.Disabled);
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
            // https://github.com/xamarin/Essentials/blob/master/Xamarin.Essentials/Permissions/Permissions.android.cs
            Android.Content.Context context = Android.App.Application.Context;
            Android.Content.PM.PackageInfo packageInfo = 
                context.PackageManager.GetPackageInfo(context.PackageName, PackageInfoFlags.Permissions);
            var requestedPermissions = packageInfo?.RequestedPermissions;
            if(requestedPermissions is null)
                return Windows.Devices.Geolocation.GeolocationAccessStatus.Denied;
            
            bool bInManifest = false;
            foreach (string oPerm in requestedPermissions)
            {
                if (oPerm.Equals(Android.Manifest.Permission.AccessFineLocation, StringComparison.OrdinalIgnoreCase))
                    bInManifest = true;
            }

            if(!bInManifest)
                return Windows.Devices.Geolocation.GeolocationAccessStatus.Denied;


            // check if permission is granted
            if (Android.Support.V4.Content.ContextCompat.CheckSelfPermission(Uno.UI.ContextHelper.Current, Android.Manifest.Permission.AccessFineLocation)
                    == Android.Content.PM.Permission.Granted)
            {
                return Windows.Devices.Geolocation.GeolocationAccessStatus.Allowed;
            }

            // zamiast dialogu

            return Windows.Devices.Geolocation.GeolocationAccessStatus.Denied;
            //Android.App.Activity oAct;
            //ActivityCompat.RequestPermissions(Platform.GetCurrentActivity(true), androidPermissions, requestCode);
            
        }

        private LocationManager InitializeLocationProvider()
        {
            var locationManager = (LocationManager)global::Android.App.Application.Context.GetSystemService(Android.Content.Context.LocationService);

            var criteriaForLocationService = new Criteria();
            {
                if (_desiredAccuracyInMeters.HasValue)
                {
                    if (_desiredAccuracyInMeters.Value < 100)
                    {
                        criteriaForLocationService.HorizontalAccuracy = Accuracy.High; 
                    }
                    else
                        if (_desiredAccuracyInMeters.Value < 500)
                        {
                            criteriaForLocationService.HorizontalAccuracy = Accuracy.Medium;
                        }
                        else
                        {
                            criteriaForLocationService.HorizontalAccuracy = Accuracy.Low;
                        }

                }
                else
                {
                    criteriaForLocationService.HorizontalAccuracy = Accuracy.Medium;
                }

                // <100 m :Accuracy.High
                // >500 m: Accuracy.Low
                // albo: Accuracy.Medium
            };

            _locationProvider = locationManager.GetBestProvider(criteriaForLocationService, true);

            return locationManager;
        }

        //partial void StartPositionChanged()
        //{
        //    BroadcastStatus(PositionStatus.Initializing);
        //}

        static bool mbZmiany = false;
        static Location moLocat;

        void ILocationListener.OnLocationChanged(Location location)
        {
            mbZmiany = true;
            moLocat = location;
            //BroadcastStatus(PositionStatus.Ready);
            // this._positionChanged?.Invoke(this, new PositionChangedEventArgs(location.ToGeoPosition()));
        }

        void ILocationListener.OnProviderDisabled(string provider)
        {
        }

        void ILocationListener.OnProviderEnabled(string provider)
        {
        }

        void ILocationListener.OnStatusChanged(string provider, [GeneratedEnum] Availability status, Bundle extras)
        {
        }
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
            Windows.Devices.Geolocation.PositionSource posSource = Windows.Devices.Geolocation.PositionSource.Unknown;
            switch (location.Provider)
            {
                case Android.Locations.LocationManager.NetworkProvider:
                    posSource = Windows.Devices.Geolocation.PositionSource.Cellular;    // cell, wifi
                    break;
                case Android.Locations.LocationManager.PassiveProvider:
                    posSource = Windows.Devices.Geolocation.PositionSource.Unknown;  // inni
                    break;
                case Android.Locations.LocationManager.GpsProvider:
                    posSource = Windows.Devices.Geolocation.PositionSource.Satellite;
                    break;
            }

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
                    heading: geoheading,
                    positionSource: posSource
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
