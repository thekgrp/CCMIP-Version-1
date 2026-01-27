using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace CoreCommandMIP.Client
{
    internal sealed class MetadataStore
    {
        private readonly Guid _configurationId;
        private readonly string _metadataFilePath;
        private string _lastPayload;

        internal MetadataStore(Guid configurationId)
        {
            _configurationId = configurationId;
            var baseFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CoreCommandMIP", "Metadata");
            Directory.CreateDirectory(baseFolder);
            _metadataFilePath = Path.Combine(baseFolder, string.Format(CultureInfo.InvariantCulture, "{0:D}.json", configurationId));
        }

        internal bool TryPersist(SmartMapLocation location, RemoteServerSettings settings, string siteName)
        {
            if (location == null)
            {
                return false;
            }

            var payload = BuildPayload(location, settings, siteName);
            if (string.Equals(payload, _lastPayload, StringComparison.Ordinal))
            {
                return false;
            }

            _lastPayload = payload;
            File.WriteAllText(_metadataFilePath, payload, Encoding.ASCII);
            return true;
        }

        private string BuildPayload(SmartMapLocation location, RemoteServerSettings settings, string siteName)
        {
            var resolvedSettings = settings ?? new RemoteServerSettings();
            var resolvedName = string.IsNullOrWhiteSpace(siteName) ? "Unnamed site" : siteName;
            var tailLength = Math.Max(1, Math.Round(resolvedSettings.TailLength));
            var radius = Math.Max(0, Math.Round(resolvedSettings.SiteRadiusMeters));
            const int keepAlive = 5;
            var builder = new StringBuilder(256);

            builder.Append("{\"Error\":false,\"Results\":{");
            builder.AppendFormat(CultureInfo.InvariantCulture,
                "\"GpsLocation\":{{\"Latitude\":{0},\"Longitude\":{1},\"Altitude\":{2}}}",
                FormatCoordinate(location.Latitude),
                FormatCoordinate(location.Longitude),
                FormatCoordinate(location.Altitude, "0.##"));
            builder.AppendFormat(CultureInfo.InvariantCulture, ",\"SiteRadius\":{0}", radius.ToString("0", CultureInfo.InvariantCulture));
            builder.AppendFormat(CultureInfo.InvariantCulture, ",\"KeepAliveDuration\":{0}", keepAlive);
            builder.Append(",\"PinMaximumDuration\":600");
            builder.Append(",\"SessionTimeoutMinutes\":0");
            builder.Append(",\"ShouldMerge\":false");
            builder.Append(",\"MaximumMergeDistance\":10");
            builder.Append(",\"MaximumMergeVelocityDifference\":100");
            builder.AppendFormat(CultureInfo.InvariantCulture, ",\"TailLength\":{0}", tailLength.ToString("0", CultureInfo.InvariantCulture));
            builder.Append(",\"NoiseFilterEnable\":false");
            builder.Append(",\"NoiseCaptureTime\":30");
            builder.Append(",\"NoiseCellSize\":1");
            builder.Append(",\"NoiseThreshold\":10");
            builder.Append(",\"AnonymousRTSP\":true");
            builder.Append(",\"HeartbeatIntervalSec\":0");
            builder.Append(",\"HeartbeatAction\":\"\"");
            builder.Append(",\"ShowFailedLogins\":true");
            builder.Append(",\"RtspFrameRate\":1");
            builder.Append(",\"RtspResolution\":1");
            builder.Append(",\"State\":1");
            builder.AppendFormat(CultureInfo.InvariantCulture, ",\"Guid\":\"{0}\"", _configurationId.ToString("D"));
            builder.AppendFormat(CultureInfo.InvariantCulture, ",\"Name\":\"{0}\"", Escape(resolvedName));
            builder.Append("}}");

            return builder.ToString();
        }

        private static string FormatCoordinate(double value, string format = "0.#####")
        {
            return value.ToString(format, CultureInfo.InvariantCulture);
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
