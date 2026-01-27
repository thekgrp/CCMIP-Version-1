using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CoreCommandMIP.Client
{
    internal sealed class RemoteServerDataProvider
    {
        private static readonly Regex _numberPattern = new Regex("-?\\d+(?:\\.\\d+)?", RegexOptions.Compiled);
        private static readonly Regex _arrayCapturePattern = new Regex("\"Classifications\"\\s*:\\s*\\[(.*?)\\]", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex _trackListArrayPattern = new Regex("\"TrackList\"\\s*:\\s*\\[(.*?)\\]", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex _trackListCounterPattern = new Regex("\"TrackList\"\\s*:\\s*(\\d+)", RegexOptions.Compiled);
        private static readonly string[] _classificationLabels = new[] { "Unknown", "Person", "Animal", "Vehicle", "Drone", "Aerial", "Other" };
        private CookieContainer _cookies = new CookieContainer();

        static RemoteServerDataProvider()
        {
            ServicePointManager.ServerCertificateValidationCallback += IgnoreCertificateValidationErrors;
        }

        private static string GetClassificationColor(string classificationLabel)
        {
            if (string.IsNullOrWhiteSpace(classificationLabel))
            {
                return "#1e88e5";
            }

            switch (classificationLabel.Trim().ToLowerInvariant())
            {
                case "person":
                    return "#ffb300";
                case "animal":
                    return "#8e24aa";
                case "vehicle":
                    return "#00acc1";
                case "drone":
                case "aerial":
                    return "#f4511e";
                case "other":
                    return "#43a047";
                default:
                    return "#1e88e5";
            }
        }

        internal async Task<RemoteSiteConfiguration> FetchSiteConfigurationAsync(string baseUrl, string username, string password, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException("Remote URL is required.");

            var normalizedBaseUrl = NormalizeBaseUrl(baseUrl);
            _cookies = new CookieContainer();

            await LoginAsync(new Uri(normalizedBaseUrl + "/rest/session/login"), username, password, cancellationToken).ConfigureAwait(false);

            var response = await SendAsync(CreateRequest(new Uri(normalizedBaseUrl + "/rest/site/config")), null, cancellationToken).ConfigureAwait(false);
            if (!ResponseIsSuccess(response))
            {
                throw new InvalidOperationException("Failed to retrieve site configuration.");
            }

            return ParseSiteConfiguration(response);
        }

        internal async Task<IReadOnlyList<SmartMapLocation>> FetchActiveTracksAsync(string baseUrl, string username, string password, RemoteServerSettings defaults, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException("Remote URL is required.");

            var normalizedBaseUrl = NormalizeBaseUrl(baseUrl);
            _cookies = new CookieContainer();
            defaults = defaults ?? new RemoteServerSettings();

            await LoginAsync(new Uri(normalizedBaseUrl + "/rest/session/login"), username, password, cancellationToken).ConfigureAwait(false);

            var trackIds = await GetTrackIdsAsync(new Uri(normalizedBaseUrl + "/rest/tracks/list"), cancellationToken).ConfigureAwait(false);
            if (trackIds.Count == 0)
            {
                return new List<SmartMapLocation> { SmartMapLocation.FromSettings(defaults, "No active tracks reported.") };
            }

            var result = await FetchTracksByIdsAsync(normalizedBaseUrl, trackIds, defaults, cancellationToken).ConfigureAwait(false);
            if (result.Count == 0)
            {
                result.Add(SmartMapLocation.FromSettings(defaults, "Unable to parse track details."));
            }

            return result;
        }

        internal async Task<TrackFetchResult> FetchChangedTracksAsync(string baseUrl, string username, string password, RemoteServerSettings defaults, long? lastTrackListCounter, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException("Remote URL is required.");

            var normalizedBaseUrl = NormalizeBaseUrl(baseUrl);
            _cookies = new CookieContainer();
			defaults = defaults ?? new RemoteServerSettings();

            await LoginAsync(new Uri(normalizedBaseUrl + "/rest/session/login"), username, password, cancellationToken).ConfigureAwait(false);

            var changeInfo = await GetTrackChangeInfoAsync(new Uri(normalizedBaseUrl + "/rest/changes"), cancellationToken).ConfigureAwait(false);
            var hasExplicitIds = changeInfo.TrackIds.Count > 0;
            var counterChanged = !changeInfo.Counter.HasValue || !lastTrackListCounter.HasValue || changeInfo.Counter.Value != lastTrackListCounter.Value;

            var trackIds = hasExplicitIds
                ? changeInfo.TrackIds
                : await GetTrackIdsAsync(new Uri(normalizedBaseUrl + "/rest/tracks/list"), cancellationToken).ConfigureAwait(false);

			List<SmartMapLocation> locations;
			if (trackIds.Count == 0)
			{
				locations = new List<SmartMapLocation> { SmartMapLocation.FromSettings(defaults, "No active tracks reported.") };
			}
			else
			{
				locations = await FetchTracksByIdsAsync(normalizedBaseUrl, trackIds, defaults, cancellationToken).ConfigureAwait(false);
				if (locations.Count == 0)
				{
					locations.Add(SmartMapLocation.FromSettings(defaults, "Unable to parse track details."));
				}
			}

			var hasUpdates = hasExplicitIds || counterChanged || locations.Count > 0;
			return new TrackFetchResult(hasUpdates, changeInfo.Counter, locations);
        }

		private async Task<List<SmartMapLocation>> FetchTracksByIdsAsync(string normalizedBaseUrl, IEnumerable<long> trackIds, RemoteServerSettings defaults, CancellationToken token)
		{
			var result = new List<SmartMapLocation>();
			if (trackIds == null)
			{
				return result;
			}

			foreach (var trackId in trackIds)
			{
				var location = await GetTrackDetailsAsync(new Uri(string.Format(CultureInfo.InvariantCulture, "{0}/rest/tracks/{1}", normalizedBaseUrl, trackId)), defaults, trackId, token).ConfigureAwait(false);
				if (location != null)
				{
					result.Add(location);
				}
			}

			return result;
		}

        internal async Task<SmartMapLocation> FetchLatestTrackAsync(string baseUrl, string username, string password, RemoteServerSettings defaults, CancellationToken cancellationToken)
        {
            var tracks = await FetchActiveTracksAsync(baseUrl, username, password, defaults, cancellationToken).ConfigureAwait(false);
            return tracks.Count > 0 ? tracks[0] : SmartMapLocation.FromSettings(defaults, "No active tracks reported.");
        }

        internal static string NormalizeBaseUrl(string baseUrl)
        {
            var trimmed = baseUrl.Trim().TrimEnd('/');
            if (!trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Remote URL must start with https://");
            }

            return trimmed;
        }

        internal async Task LoginAsync(Uri uri, string username, string password, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("Username and password are required.");
            }

            var payload = string.Format(CultureInfo.InvariantCulture, "{{\"user\":\"{0}\",\"password\":\"{1}\"}}", EscapeJson(username), EscapeJson(password));
            var response = await SendAsync(CreateRequest(uri), payload, token).ConfigureAwait(false);
            if (!ResponseIsSuccess(response))
            {
                throw new InvalidOperationException("Login rejected by remote server.");
            }
        }

        private async Task<List<long>> GetTrackIdsAsync(Uri uri, CancellationToken token)
        {
            var response = await SendAsync(CreateRequest(uri), null, token).ConfigureAwait(false);
            if (!ResponseIsSuccess(response))
            {
                throw new InvalidOperationException("Failed to query active tracks.");
            }

            var ids = new List<long>();
            var match = Regex.Match(response, "\"Results\"\\s*:\\s*\\[(.*?)\\]", RegexOptions.Singleline);
            if (match.Success)
            {
                var parts = match.Groups[1].Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    if (long.TryParse(part.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
                    {
                        ids.Add(id);
                    }
                }
            }

            return ids;
        }

		private async Task<TrackChangeInfo> GetTrackChangeInfoAsync(Uri uri, CancellationToken token)
		{
			var response = await SendAsync(CreateRequest(uri), null, token).ConfigureAwait(false);
			if (!ResponseIsSuccess(response))
			{
				throw new InvalidOperationException("Failed to determine change state.");
			}

			return ParseTrackChangeInfo(response);
		}

		private static TrackChangeInfo ParseTrackChangeInfo(string payload)
		{
			var info = new TrackChangeInfo();
			if (string.IsNullOrWhiteSpace(payload))
			{
				return info;
			}

			var arrayMatch = _trackListArrayPattern.Match(payload);
			if (arrayMatch.Success)
			{
				var entries = arrayMatch.Groups[1].Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (var entry in entries)
				{
					if (long.TryParse(entry.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
					{
						info.TrackIds.Add(id);
					}
				}
			}
			else
			{
				var counterMatch = _trackListCounterPattern.Match(payload);
				if (counterMatch.Success && long.TryParse(counterMatch.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var counter))
				{
					info.Counter = counter;
				}
			}

			return info;
		}

        private async Task<SmartMapLocation> GetTrackDetailsAsync(Uri uri, RemoteServerSettings defaults, long trackId, CancellationToken token)
        {
            var response = await SendAsync(CreateRequest(uri), null, token).ConfigureAwait(false);
            if (!ResponseIsSuccess(response))
            {
                return SmartMapLocation.FromSettings(defaults, string.Format(CultureInfo.InvariantCulture, "Track {0} not available.", trackId));
            }

            var location = ParseTrackDetail(response, defaults, trackId);
            if (location != null)
            {
                location.StatusMessage = string.Format(CultureInfo.InvariantCulture, "Track {0} retrieved from remote server.", trackId);
            }

            return location;
        }

        internal HttpWebRequest CreateRequest(Uri uri)
        {
            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.CookieContainer = _cookies;
            request.Accept = "application/json";
            request.Timeout = (int)TimeSpan.FromSeconds(15).TotalMilliseconds;
            return request;
        }

        internal async Task<string> SendAsync(HttpWebRequest request, string body, CancellationToken token)
        {
            using (token.Register(request.Abort))
            {
                if (!string.IsNullOrEmpty(body))
                {
                    request.Method = "POST";
                    request.ContentType = "application/json";
                    using (var stream = await request.GetRequestStreamAsync().ConfigureAwait(false))
                    using (var writer = new StreamWriter(stream, Encoding.UTF8))
                    {
                        await writer.WriteAsync(body).ConfigureAwait(false);
                    }
                }
                else
                {
                    request.Method = "GET";
                }

                using (var response = (HttpWebResponse)await request.GetResponseAsync().ConfigureAwait(false))
                using (var stream = response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    return await reader.ReadToEndAsync().ConfigureAwait(false);
                }
            }
        }

        private static SmartMapLocation ParseTrackDetail(string payload, RemoteServerSettings defaults, long trackId)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return SmartMapLocation.FromSettings(defaults, "Empty track payload");
            }

            double defaultLatitude = defaults?.DefaultLatitude ?? 0;
            double defaultLongitude = defaults?.DefaultLongitude ?? 0;
            double latitude = ExtractGpsCoordinate(payload, "Latitude", defaultLatitude);
            double longitude = ExtractGpsCoordinate(payload, "Longitude", defaultLongitude);
            double altitude = ExtractValue(payload, 0, "Altitude");
            double velocity = ExtractValue(payload, 0, "Velocity");
            double timestamp = ExtractValue(payload, 0, "Timestamp");
            var classification = ExtractClassification(payload);
            bool alarming = ExtractBoolValue(payload, "Alarming");
            bool alerting = ExtractBoolValue(payload, "Alerting");
            var sources = ExtractSourcesArray(payload);

            DateTimeOffset? timestampValue = null;
            if (timestamp > 1000)
            {
                try
                {
                    timestampValue = DateTimeOffset.FromUnixTimeSeconds((long)timestamp);
                }
                catch
                {
                    // ignore malformed timestamp
                }
            }

            return new SmartMapLocation
            {
                TrackId = trackId,
                Latitude = latitude,
                Longitude = longitude,
                Altitude = altitude,
                Velocity = velocity,
                ZoomLevel = defaults?.DefaultZoomLevel ?? 8,
                ClassificationLabel = classification.Label,
                ClassificationConfidence = classification.Confidence,
                Description = timestampValue?.ToString("u", CultureInfo.InvariantCulture) ?? string.Empty,
                IconColorHex = GetClassificationColor(classification.Label),
                Timestamp = timestampValue,
                Alarming = alarming,
                Alerting = alerting,
                Sources = sources
            };
        }

        private static ClassificationResult ExtractClassification(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return ClassificationResult.Default;
            }

            var match = _arrayCapturePattern.Match(payload);
            if (!match.Success)
            {
                return ClassificationResult.Default;
            }

            var best = ClassificationResult.Default;
            var entries = match.Groups[1].Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var maxIndex = Math.Min(entries.Length, _classificationLabels.Length);
            for (int i = 0; i < maxIndex; i++)
            {
                if (double.TryParse(entries[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                {
                    if (value >= best.Confidence)
                    {
                        best = new ClassificationResult(_classificationLabels[i], value);
                    }
                }
            }

            return best;
        }

        private static List<string> ExtractSourcesArray(string payload)
        {
            var sources = new List<string>();
            if (string.IsNullOrWhiteSpace(payload))
            {
                return sources;
            }

            // Match "Sources":["flyover","radar"] pattern
            var match = Regex.Match(payload, "\"Sources\"\\s*:\\s*\\[([^\\]]+)\\]", RegexOptions.Singleline);
            if (match.Success)
            {
                var sourcesText = match.Groups[1].Value;
                // Extract quoted strings
                var sourceMatches = Regex.Matches(sourcesText, "\"([^\"]+)\"");
                foreach (Match sourceMatch in sourceMatches)
                {
                    sources.Add(sourceMatch.Groups[1].Value);
                }
            }

            return sources;
        }

		private static RemoteSiteConfiguration ParseSiteConfiguration(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return null;
            }

            var latitude = ExtractGpsCoordinate(payload, "Latitude", 0);
            var longitude = ExtractGpsCoordinate(payload, "Longitude", 0);
            var name = ExtractStringValue(payload, "Name");
            var radius = ExtractValue(payload, 0, "SiteRadius");
			var keepAlive = ExtractValue(payload, 1, "KeepAliveDuration", "KeepAliveDurationSeconds");
            var tailLength = ExtractValue(payload, 200, "TailLength");
            return new RemoteSiteConfiguration
            {
                Latitude = latitude,
                Longitude = longitude,
                Name = name,
                SiteRadius = radius,
                KeepAliveDurationSeconds = keepAlive,
                TailLength = tailLength
            };
        }

        internal static bool ResponseIsSuccess(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return false;
            }

            return payload.IndexOf("\"Error\":false", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static double ExtractGpsCoordinate(string payload, string key, double defaultValue)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return defaultValue;
            }

            var regionIndex = payload.IndexOf("\"GpsLocation\"", StringComparison.OrdinalIgnoreCase);
            var scope = regionIndex >= 0 ? payload.Substring(regionIndex) : payload;
            return ExtractValue(scope, defaultValue, key);
        }

        internal static double ExtractValue(string payload, double defaultValue, params string[] keys)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return defaultValue;
            }

            foreach (var key in keys)
            {
                var sanitizedKey = key?.Trim('"');
                if (string.IsNullOrWhiteSpace(sanitizedKey))
                {
                    continue;
                }

                var keyIndex = payload.IndexOf(sanitizedKey, StringComparison.OrdinalIgnoreCase);
                if (keyIndex < 0)
                {
                    continue;
                }

                var substring = payload.Substring(keyIndex);
                var match = _numberPattern.Match(substring);
                if (match.Success && double.TryParse(match.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                {
                    return value;
                }
            }

            return defaultValue;
        }

        internal static string ExtractStringValue(string payload, string key)
        {
            if (string.IsNullOrWhiteSpace(payload) || string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            var token = string.Format(CultureInfo.InvariantCulture, "\"{0}\"", key.Trim('"'));
            var keyIndex = payload.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            if (keyIndex < 0)
            {
                return string.Empty;
            }

            var colonIndex = payload.IndexOf(':', keyIndex + token.Length);
            if (colonIndex < 0 || colonIndex + 1 >= payload.Length)
            {
                return string.Empty;
            }

            var index = colonIndex + 1;
            while (index < payload.Length && char.IsWhiteSpace(payload[index]))
            {
                index++;
            }

            if (index >= payload.Length || payload[index] != '"')
            {
                return string.Empty;
            }

            index++;
            var builder = new StringBuilder();
            var escaped = false;
            while (index < payload.Length)
            {
                var ch = payload[index++];
                if (escaped)
                {
                    builder.Append(ch);
                    escaped = false;
                    continue;
                }

                if (ch == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (ch == '"')
                {
                    break;
                }

                builder.Append(ch);
            }

            return builder.ToString();
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

	internal async Task<List<RegionListItem>> FetchRegionListAsync(string baseUrl, string username, string password, CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(baseUrl))
			throw new InvalidOperationException("Remote URL is required.");

		var normalizedBaseUrl = NormalizeBaseUrl(baseUrl);
		_cookies = new CookieContainer();

		await LoginAsync(new Uri(normalizedBaseUrl + "/rest/session/login"), username, password, cancellationToken).ConfigureAwait(false);

		var response = await SendAsync(CreateRequest(new Uri(normalizedBaseUrl + "/rest/regions/list")), null, cancellationToken).ConfigureAwait(false);
		if (!ResponseIsSuccess(response))
		{
			return new List<RegionListItem>();
		}

		var regionList = ParseRegionList(response);
		
		// For GUID-based regions, fetch the full details to get the Name, Active, and Exclusion
		foreach (var region in regionList)
		{
			if (!string.IsNullOrEmpty(region.GuidId))
			{
				try
				{
					var detailResponse = await SendAsync(CreateRequest(new Uri(string.Format(CultureInfo.InvariantCulture, "{0}/rest/regions/{1}", normalizedBaseUrl, region.GuidId))), null, cancellationToken).ConfigureAwait(false);
					if (ResponseIsSuccess(detailResponse))
					{
						var regionDef = ParseRegionDefinition(detailResponse);
						if (regionDef != null && !string.IsNullOrEmpty(regionDef.Name))
						{
							region.Name = regionDef.Name;
							region.Active = regionDef.Active;
							region.Exclusion = regionDef.Exclusion;
						}
					}
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"Failed to fetch details for region {region.GuidId}: {ex.Message}");
					// Keep the truncated GUID as fallback
				}
			}
		}

		return regionList;
	}

	internal async Task<RegionDefinition> FetchRegionDetailsAsync(string baseUrl, string username, string password, string regionIdOrGuid, CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(baseUrl))
				throw new InvalidOperationException("Remote URL is required.");

			var normalizedBaseUrl = NormalizeBaseUrl(baseUrl);
			_cookies = new CookieContainer();

			await LoginAsync(new Uri(normalizedBaseUrl + "/rest/session/login"), username, password, cancellationToken).ConfigureAwait(false);

		var response = await SendAsync(CreateRequest(new Uri(string.Format(CultureInfo.InvariantCulture, "{0}/rest/regions/{1}", normalizedBaseUrl, regionIdOrGuid))), null, cancellationToken).ConfigureAwait(false);
			if (!ResponseIsSuccess(response))
			{
				return null;
			}

			return ParseRegionDefinition(response);
	}

	private static List<RegionListItem> ParseRegionList(string payload)
	{
		var result = new List<RegionListItem>();
		
		System.Diagnostics.Debug.WriteLine("=== ParseRegionList ===");
		System.Diagnostics.Debug.WriteLine($"Payload length: {payload?.Length ?? 0}");
		if (payload != null && payload.Length < 1000)
		{
			System.Diagnostics.Debug.WriteLine($"Full payload: {payload}");
		}
		
		if (string.IsNullOrWhiteSpace(payload))
		{
			System.Diagnostics.Debug.WriteLine("Payload is null or whitespace");
			return result;
		}

		var listMatch = Regex.Match(payload, "\"Results\"\\s*:\\s*\\[(.*)\\]", RegexOptions.Singleline);
		if (!listMatch.Success)
		{
			System.Diagnostics.Debug.WriteLine("Failed to match 'Results' wrapper - trying direct array");
			listMatch = Regex.Match(payload, "^\\s*\\[(.*)\\]\\s*$", RegexOptions.Singleline);
			if (!listMatch.Success)
			{
				System.Diagnostics.Debug.WriteLine("No JSON array pattern matched");
				return result;
			}
	}

	var itemsText = listMatch.Groups[1].Value;
	
	// Check if it's a GUID array or object array
	var guidMatches = Regex.Matches(itemsText, "\"([a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12})\"", RegexOptions.IgnoreCase);
	
	if (guidMatches.Count > 0)
	{
		// Server returns array of GUID strings
		System.Diagnostics.Debug.WriteLine($"Found {guidMatches.Count} GUID regions");
		
		foreach (Match guidMatch in guidMatches)
		{
			var guid = guidMatch.Groups[1].Value;
			result.Add(new RegionListItem
			{
				Id = result.Count + 1,
				Name = guid.Substring(0, 8),
				Active = true,
				GuidId = guid
			});
		}
		
		System.Diagnostics.Debug.WriteLine($"Total GUID regions: {result.Count}");
		return result;
	}
	
	// Try parsing as object array
	var itemMatches = Regex.Matches(itemsText, "\\{([^}]+)\\}");
	System.Diagnostics.Debug.WriteLine($"Found {itemMatches.Count} region items");
	
	foreach (Match itemMatch in itemMatches)
		{
			var itemText = itemMatch.Groups[1].Value;
			var idMatch = Regex.Match(itemText, "\"Id\"\\s*:\\s*(\\d+)");
			var nameMatch = Regex.Match(itemText, "\"Name\"\\s*:\\s*\"([^\"]+)\"");
			var activeMatch = Regex.Match(itemText, "\"Active\"\\s*:\\s*(true|false)", RegexOptions.IgnoreCase);

			if (idMatch.Success && long.TryParse(idMatch.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
			{
				var regionItem = new RegionListItem
				{
					Id = id,
					Name = nameMatch.Success ? nameMatch.Groups[1].Value : "Region " + id,
					Active = activeMatch.Success && string.Equals(activeMatch.Groups[1].Value, "true", StringComparison.OrdinalIgnoreCase)
				};
				
				result.Add(regionItem);
				System.Diagnostics.Debug.WriteLine($"Parsed region: ID={regionItem.Id}, Name={regionItem.Name}, Active={regionItem.Active}");
			}
		}

		System.Diagnostics.Debug.WriteLine($"ParseRegionList returning {result.Count} regions");
		return result;
	}

	private static RegionDefinition ParseRegionDefinition(string payload)
		{
			if (string.IsNullOrWhiteSpace(payload))
			{
				return null;
			}

			var resultsMatch = Regex.Match(payload, "\"Results\"\\s*:\\s*\\{(.*)\\}", RegexOptions.Singleline);
			if (!resultsMatch.Success)
			{
				return null;
			}

			var resultsText = resultsMatch.Groups[1].Value;
			var region = new RegionDefinition
			{
				Name = ExtractStringValue(resultsText, "Name"),
				Color = ExtractStringValue(resultsText, "Color"),
				Active = ExtractBoolValue(resultsText, "Active"),
				Exclusion = ExtractBoolValue(resultsText, "Exclusion"),
				Fill = ExtractValue(resultsText, 0, "Fill")
			};

			var altLimitsMatch = Regex.Match(resultsText, "\"AltitudeLimits\"\\s*:\\s*\\{([^}]+)\\}");
			if (altLimitsMatch.Success)
			{
				var limitsText = altLimitsMatch.Groups[1].Value;
				region.AltitudeLimits = new RegionAltitudeLimits
				{
					Greater = ExtractValue(limitsText, 0, "Greater"),
					Lesser = ExtractValue(limitsText, 0, "Lesser")
				};
			}

			var verticesMatch = Regex.Match(resultsText, "\"Vertices\"\\s*:\\s*\\[(.*)\\]", RegexOptions.Singleline);
			if (verticesMatch.Success)
			{
				var verticesText = verticesMatch.Groups[1].Value;
				var vertexMatches = Regex.Matches(verticesText, "\\{([^}]+)\\}");
				foreach (Match vertexMatch in vertexMatches)
				{
					var vertexText = vertexMatch.Groups[1].Value;
					region.Vertices.Add(new RegionVertex
					{
						Latitude = ExtractValue(vertexText, 0, "Latitude"),
						Longitude = ExtractValue(vertexText, 0, "Longitude"),
						Altitude = ExtractValue(vertexText, 0, "Altitude")
					});
				}
			}

			return region;
		}

		private static bool ExtractBoolValue(string payload, string propertyName)
		{
			if (string.IsNullOrWhiteSpace(payload) || string.IsNullOrWhiteSpace(propertyName))
			{
				return false;
			}

			var pattern = string.Format(CultureInfo.InvariantCulture, "\"{0}\"\\s*:\\s*(true|false)", Regex.Escape(propertyName));
			var match = Regex.Match(payload, pattern, RegexOptions.IgnoreCase);
			return match.Success && string.Equals(match.Groups[1].Value, "true", StringComparison.OrdinalIgnoreCase);
		}

        private static bool IgnoreCertificateValidationErrors(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }

    internal class SmartMapLocation
    {
        public long TrackId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Altitude { get; set; }
        public double Velocity { get; set; }
        public double ZoomLevel { get; set; }
        public string ClassificationLabel { get; set; }
        public double ClassificationConfidence { get; set; }
        public string Description { get; set; }
        public string StatusMessage { get; set; }
        public string IconColorHex { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public bool Alarming { get; set; }
        public bool Alerting { get; set; }
        public List<string> Sources { get; set; }
        
        // Display property for ListView binding
        public string SourcesDisplay => Sources != null && Sources.Count > 0 
            ? string.Join(", ", Sources) 
            : string.Empty;

        internal static SmartMapLocation FromSettings(RemoteServerSettings settings, string statusMessage)
        {
            return new SmartMapLocation
            {
                TrackId = 0,
                Latitude = settings?.DefaultLatitude ?? 0,
                Longitude = settings?.DefaultLongitude ?? 0,
                Altitude = 0,
                Velocity = 0,
                ZoomLevel = settings?.DefaultZoomLevel ?? 8,
                ClassificationLabel = "Unknown",
                ClassificationConfidence = 0,
                Description = statusMessage,
                StatusMessage = statusMessage,
                IconColorHex = "#78909c",
                Timestamp = null,
                Alarming = false,
                Alerting = false,
                Sources = new List<string>()
            };
        }
    }

    internal struct ClassificationResult
    {
        internal static readonly ClassificationResult Default = new ClassificationResult("Unknown", 0);

        internal ClassificationResult(string label, double confidence)
        {
            Label = label;
            Confidence = confidence;
        }

        internal string Label { get; }
        internal double Confidence { get; }
    }

    internal sealed class RemoteSiteConfiguration
    {
        internal double Latitude { get; set; }
        internal double Longitude { get; set; }
        internal string Name { get; set; }
        internal double SiteRadius { get; set; }
        internal double KeepAliveDurationSeconds { get; set; }
        internal double TailLength { get; set; }
    }
}
