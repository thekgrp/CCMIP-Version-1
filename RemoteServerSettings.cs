using System;
using System.Globalization;
using VideoOS.Platform;

namespace CoreCommandMIP
{
    internal class RemoteServerSettings
    {
        private const string HostKey = "RemoteServerHost";
        private const string PortKey = "RemoteServerPort";
        private const string UseSslKey = "RemoteServerUseSsl";
        private const string ApiKeyKey = "RemoteServerApiKey";
        private const string UsernameKey = "RemoteServerUsername";
        private const string PasswordKey = "RemoteServerPassword";
        private const string LatitudeKey = "RemoteServerLatitude";
        private const string LongitudeKey = "RemoteServerLongitude";
        private const string ZoomKey = "RemoteServerZoom";
        private const string RadiusKey = "RemoteServerRadius";
        private const string PollIntervalKey = "RemoteServerPoll";
        private const string TailLengthKey = "RemoteServerTail";
        private const string MapProviderKey = "MapProvider";
        private const string MapboxAccessTokenKey = "MapboxAccessToken";
        private const string EnableMapCachingKey = "EnableMapCaching";
        private const string Enable3DMapKey = "Enable3DMap";
        private const string Enable3DBuildingsKey = "Enable3DBuildings";
        private const string Enable3DTerrainKey = "Enable3DTerrain";
        private const string DefaultPitchKey = "DefaultPitch";
        private const string DefaultBearingKey = "DefaultBearing";
        private const string SelectedRegionIdsKey = "SelectedRegionIds";
        private const string AssociatedCameraIdsKey = "AssociatedCameraIds";
        private const string PluginInstanceIdKey = "PluginInstanceId";
        private const string LastHealthCheckKey = "LastHealthCheck";
        private const string HealthStatusKey = "HealthStatus";

        internal string Host { get; set; } = string.Empty;
        internal int Port { get; set; } = 443;
        internal bool UseSsl { get; set; } = true;
        internal string ApiKey { get; set; } = string.Empty;
        internal double DefaultLatitude { get; set; } = 0d;
        internal double DefaultLongitude { get; set; } = 0d;
        internal double DefaultZoomLevel { get; set; } = 8d;
        internal string DefaultUsername { get; set; } = string.Empty;
        internal string DefaultPassword { get; set; } = string.Empty;
        internal double SiteRadiusMeters { get; set; } = 0d;
	internal double PollingIntervalSeconds { get; set; } = 1d;
        internal double TailLength { get; set; } = 200d;
        internal MapProvider MapProvider { get; set; } = MapProvider.Leaflet;
        internal string MapboxAccessToken { get; set; } = string.Empty;
        internal bool EnableMapCaching { get; set; } = true;
        internal bool Enable3DMap { get; set; } = false;
        internal bool Enable3DBuildings { get; set; } = true;
        internal bool Enable3DTerrain { get; set; } = true;
        internal double DefaultPitch { get; set; } = 45d; // Degrees from vertical (0-60)
        internal double DefaultBearing { get; set; } = 0d; // Rotation in degrees
        internal string SelectedRegionIds { get; set; } = string.Empty; // Comma-separated list of region IDs
        internal string AssociatedCameraIds { get; set; } = string.Empty; // Comma-separated camera GUIDs
        internal Guid PluginInstanceId { get; set; } = Guid.NewGuid(); // Unique instance identifier
        internal DateTime? LastHealthCheck { get; set; } = null;
        internal HealthStatus HealthStatus { get; set; } = HealthStatus.Unknown;


        internal string Summary
        {
            get
            {
                if (!IsConfigured())
                {
                    return "Remote server not configured";
                }

                return GetBaseUrl();
            }
        }

        internal static RemoteServerSettings FromItem(Item item)
        {
            if (item == null)
            {
                return new RemoteServerSettings();
            }

            var settings = new RemoteServerSettings();
            if (item.Properties.TryGetValue(HostKey, out var host))
            {
                settings.Host = host;
            }
            if (item.Properties.TryGetValue(PortKey, out var portString) && int.TryParse(portString, NumberStyles.Integer, CultureInfo.InvariantCulture, out var port))
            {
                settings.Port = port;
            }
            if (item.Properties.TryGetValue(UseSslKey, out var useSslString) && bool.TryParse(useSslString, out var useSsl))
            {
                settings.UseSsl = useSsl;
            }
            if (item.Properties.TryGetValue(ApiKeyKey, out var apiKey))
            {
                settings.ApiKey = apiKey;
            }
            if (item.Properties.TryGetValue(UsernameKey, out var username))
            {
                settings.DefaultUsername = username;
            }
            if (item.Properties.TryGetValue(PasswordKey, out var password))
            {
                settings.DefaultPassword = password;
            }
            if (item.Properties.TryGetValue(LatitudeKey, out var latitudeString) && double.TryParse(latitudeString, NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude))
            {
                settings.DefaultLatitude = latitude;
            }
            if (item.Properties.TryGetValue(LongitudeKey, out var longitudeString) && double.TryParse(longitudeString, NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude))
            {
                settings.DefaultLongitude = longitude;
            }
            if (item.Properties.TryGetValue(ZoomKey, out var zoomString) && double.TryParse(zoomString, NumberStyles.Float, CultureInfo.InvariantCulture, out var zoom))
            {
                settings.DefaultZoomLevel = zoom;
            }
            if (item.Properties.TryGetValue(RadiusKey, out var radiusString) && double.TryParse(radiusString, NumberStyles.Float, CultureInfo.InvariantCulture, out var radius))
            {
                settings.SiteRadiusMeters = radius;
            }
            if (item.Properties.TryGetValue(PollIntervalKey, out var pollString) && double.TryParse(pollString, NumberStyles.Float, CultureInfo.InvariantCulture, out var poll))
            {
                settings.PollingIntervalSeconds = poll;
            }
            if (item.Properties.TryGetValue(TailLengthKey, out var tailString) && double.TryParse(tailString, NumberStyles.Float, CultureInfo.InvariantCulture, out var tail))
            {
                settings.TailLength = tail;
            }
            if (item.Properties.TryGetValue(MapProviderKey, out var mapProviderString) && Enum.TryParse<MapProvider>(mapProviderString, out var mapProvider))
            {
                settings.MapProvider = mapProvider;
            }
            if (item.Properties.TryGetValue(MapboxAccessTokenKey, out var mapboxToken))
            {
                settings.MapboxAccessToken = mapboxToken;
            }
            if (item.Properties.TryGetValue(EnableMapCachingKey, out var cachingString) && bool.TryParse(cachingString, out var enableCaching))
            {
                settings.EnableMapCaching = enableCaching;
            }
            if (item.Properties.TryGetValue(Enable3DMapKey, out var enable3DString) && bool.TryParse(enable3DString, out var enable3D))
            {
                settings.Enable3DMap = enable3D;
            }
            if (item.Properties.TryGetValue(Enable3DBuildingsKey, out var buildings3DString) && bool.TryParse(buildings3DString, out var enable3DBuildings))
            {
                settings.Enable3DBuildings = enable3DBuildings;
            }
            if (item.Properties.TryGetValue(Enable3DTerrainKey, out var terrain3DString) && bool.TryParse(terrain3DString, out var enable3DTerrain))
            {
                settings.Enable3DTerrain = enable3DTerrain;
            }
            if (item.Properties.TryGetValue(DefaultPitchKey, out var pitchString) && double.TryParse(pitchString, NumberStyles.Float, CultureInfo.InvariantCulture, out var pitch))
            {
                settings.DefaultPitch = pitch;
            }
            if (item.Properties.TryGetValue(DefaultBearingKey, out var bearingString) && double.TryParse(bearingString, NumberStyles.Float, CultureInfo.InvariantCulture, out var bearing))
            {
                settings.DefaultBearing = bearing;
            }
            if (item.Properties.TryGetValue(SelectedRegionIdsKey, out var regionIds))
            {
                settings.SelectedRegionIds = regionIds ?? string.Empty;
            }
            if (item.Properties.TryGetValue(AssociatedCameraIdsKey, out var cameraIds))
            {
                settings.AssociatedCameraIds = cameraIds ?? string.Empty;
            }
            if (item.Properties.TryGetValue(PluginInstanceIdKey, out var instanceIdString) && Guid.TryParse(instanceIdString, out var instanceId))
            {
                settings.PluginInstanceId = instanceId;
            }
            if (item.Properties.TryGetValue(LastHealthCheckKey, out var healthCheckString) && DateTime.TryParse(healthCheckString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var healthCheck))
            {
                settings.LastHealthCheck = healthCheck;
            }
            if (item.Properties.TryGetValue(HealthStatusKey, out var healthStatusString) && Enum.TryParse<HealthStatus>(healthStatusString, out var healthStatus))
            {
                settings.HealthStatus = healthStatus;
            }

            return settings;
        }

        internal void ApplyToItem(Item item)
        {
            if (item == null)
            {
                return;
            }

            item.Properties[HostKey] = Host ?? string.Empty;
            item.Properties[PortKey] = Port.ToString(CultureInfo.InvariantCulture);
            item.Properties[UseSslKey] = UseSsl.ToString();
            item.Properties[ApiKeyKey] = ApiKey ?? string.Empty;
            item.Properties[UsernameKey] = DefaultUsername ?? string.Empty;
            item.Properties[PasswordKey] = DefaultPassword ?? string.Empty;
            item.Properties[LatitudeKey] = DefaultLatitude.ToString(CultureInfo.InvariantCulture);
            item.Properties[LongitudeKey] = DefaultLongitude.ToString(CultureInfo.InvariantCulture);
            item.Properties[ZoomKey] = DefaultZoomLevel.ToString(CultureInfo.InvariantCulture);
            item.Properties[RadiusKey] = SiteRadiusMeters.ToString(CultureInfo.InvariantCulture);
            item.Properties[PollIntervalKey] = PollingIntervalSeconds.ToString(CultureInfo.InvariantCulture);
            item.Properties[TailLengthKey] = TailLength.ToString(CultureInfo.InvariantCulture);
            item.Properties[MapProviderKey] = MapProvider.ToString();
            item.Properties[MapboxAccessTokenKey] = MapboxAccessToken ?? string.Empty;
            item.Properties[EnableMapCachingKey] = EnableMapCaching.ToString();
            item.Properties[Enable3DMapKey] = Enable3DMap.ToString();
            item.Properties[Enable3DBuildingsKey] = Enable3DBuildings.ToString();
            item.Properties[Enable3DTerrainKey] = Enable3DTerrain.ToString();
            item.Properties[DefaultPitchKey] = DefaultPitch.ToString(CultureInfo.InvariantCulture);
            item.Properties[DefaultBearingKey] = DefaultBearing.ToString(CultureInfo.InvariantCulture);
            item.Properties[SelectedRegionIdsKey] = SelectedRegionIds ?? string.Empty;
            item.Properties[AssociatedCameraIdsKey] = AssociatedCameraIds ?? string.Empty;
            item.Properties[PluginInstanceIdKey] = PluginInstanceId.ToString();
            item.Properties[LastHealthCheckKey] = LastHealthCheck?.ToString("o", CultureInfo.InvariantCulture) ?? string.Empty;
            item.Properties[HealthStatusKey] = HealthStatus.ToString();
        }

	internal void ApplySiteConfiguration(double latitude, double longitude, double radiusMeters, string siteName = null, double keepAliveSeconds = 1, double tailLength = 200)
        {
            DefaultLatitude = latitude;
            DefaultLongitude = longitude;
            SiteRadiusMeters = radiusMeters;
            TailLength = tailLength > 0 ? tailLength : TailLength;
            PollingIntervalSeconds = keepAliveSeconds > 0 ? keepAliveSeconds : PollingIntervalSeconds;
            if (radiusMeters > 0)
            {
                DefaultZoomLevel = CalculateZoomFromRadius(radiusMeters, latitude);
            }
            if (!string.IsNullOrWhiteSpace(siteName))
            {
                // Optionally update name when available
            }
        }

        internal static double CalculateZoomFromRadius(double radiusMeters, double latitude)
        {
            if (radiusMeters <= 0)
            {
                return 8d;
            }

            var earthCircumference = 40075016.686;
            var latitudeRadians = latitude * Math.PI / 180d;
            var mapWidthInPixels = 256d;
            var metersPerPixel = radiusMeters * 2 / mapWidthInPixels;
            var cosLat = Math.Cos(latitudeRadians);
            if (cosLat <= 0)
            {
                cosLat = 0.00001;
            }
            var zoom = Math.Log(earthCircumference * cosLat / metersPerPixel, 2);
            zoom = Math.Max(1, Math.Min(19, zoom));
            return zoom;
        }

        internal Uri BuildEndpoint(string relativePath)
        {
            if (!IsConfigured())
            {
                throw new InvalidOperationException("Remote server is not configured");
            }

            var trimmedPath = relativePath?.Trim('/') ?? string.Empty;

            if (Host.Contains("://"))
            {
                var baseUri = new Uri(Host, UriKind.Absolute);
                var combined = new Uri(baseUri, trimmedPath);
                return combined;
            }

            var builder = new UriBuilder
            {
                Scheme = UseSsl ? Uri.UriSchemeHttps : Uri.UriSchemeHttp,
                Host = Host,
                Port = Port,
                Path = trimmedPath
            };
            return builder.Uri;
        }

        internal string GetBaseUrl()
        {
            if (!IsConfigured())
            {
                return string.Empty;
            }

            if (Host.Contains("://"))
            {
                return Host.TrimEnd('/');
            }

            var builder = new UriBuilder
            {
                Scheme = UseSsl ? Uri.UriSchemeHttps : Uri.UriSchemeHttp,
                Host = Host,
                Port = Port,
                Path = string.Empty
            };

            var uri = builder.Uri;
            return uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
        }

        internal bool IsConfigured()
        {
            return !string.IsNullOrWhiteSpace(Host);
        }

        /// <summary>
        /// Clears the WebView2 cache for this plugin instance
        /// </summary>
        internal static void ClearMapCache()
        {
            try
            {
                var cacheFolder = GetWebView2CacheFolder();
                if (System.IO.Directory.Exists(cacheFolder))
                {
                    System.Diagnostics.Debug.WriteLine($"Clearing map cache: {cacheFolder}");
                    System.IO.Directory.Delete(cacheFolder, recursive: true);
                    System.Diagnostics.Debug.WriteLine("Map cache cleared successfully");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing map cache: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets the WebView2 cache folder path
        /// </summary>
        internal static string GetWebView2CacheFolder()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return System.IO.Path.Combine(appDataPath, "CoreCommandMIP", "WebView2Cache");
        }
    }

    /// <summary>
    /// Health status of the C2 connection
    /// </summary>
    internal enum HealthStatus
    {
        Unknown = 0,
        Healthy = 1,
        Degraded = 2,
        Unhealthy = 3,
        Disconnected = 4
    }

    /// <summary>
    /// Map provider options for rendering tracks and regions
    /// </summary>
    internal enum MapProvider
    {
        /// <summary>
        /// Leaflet with OpenStreetMap tiles (free, no API key required)
        /// </summary>
        Leaflet = 0,
        
        /// <summary>
        /// Mapbox GL JS (requires API key, better imagery and performance)
        /// </summary>
        Mapbox = 1
    }
}

