Write-Host "Creating MapTemplate.cs..." -ForegroundColor Yellow

$content = @'
using System;
using System.Collections.Generic;
using System.IO;

namespace CoreCommandMIP.Client
{
	internal static class MapTemplate
	{
		private static readonly Dictionary<string, string> _iconCache = new Dictionary<string, string>();

		internal static string GetMapHtml()
		{
			var personIcon = LoadIconDataUri("person.png");
			var vehicleIcon = LoadIconDataUri("vehicle.png");
			var droneIcon = LoadIconDataUri("drone.png");
			var aerialIcon = LoadIconDataUri("aerial.jpg");
			var birdIcon = LoadIconDataUri("bird.png");
			var arrowIcon = LoadIconDataUri("arrow.png");

			return $@"<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='utf-8' />
<title>Track Map</title>
<link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css' />
<style>
html, body, #map {{ height: 100%; margin: 0; padding: 0; }}
.track-icon {{ width: 32px; height: 32px; }}
</style>
</head>
<body>
<div id='map'></div>
<script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
<script>
var map = L.map('map').setView([__LAT__, __LON__], __ZOOM__);
L.tileLayer('https://{{s}}.tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png').addTo(map);
var regionPolygons = [];
var trackLayers = {{}};
window.updateTracks = function(t) {{ console.log('tracks:', t); }};
window.clearAllTracks = function() {{ }};
window.clearInactiveTracks = function(ids) {{ }};
window.addRegion = function(r) {{ console.log('region:', r); }};
window.clearRegions = function() {{ }};
</script>
</body>
</html>";
		}

		internal static string LoadIconDataUri(string iconName)
		{
			return GetIconDataUri(iconName);
		}

		private static string GetIconDataUri(string iconName)
		{
			if (_iconCache.ContainsKey(iconName))
				return _iconCache[iconName];

			try
			{
				var assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
				var assemblyDir = System.IO.Path.GetDirectoryName(assemblyPath);
				var iconPath = System.IO.Path.Combine(assemblyDir, "assets", iconName);

				if (System.IO.File.Exists(iconPath))
				{
					var bytes = System.IO.File.ReadAllBytes(iconPath);
					var base64 = Convert.ToBase64String(bytes);
					var ext = System.IO.Path.GetExtension(iconName).ToLowerInvariant();
					var mime = ext == ".png" ? "image/png" : "image/jpeg";
					var dataUri = $"data:{mime};base64,{base64}";
					_iconCache[iconName] = dataUri;
					return dataUri;
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Error loading icon {iconName}: {ex.Message}");
			}

			return GetFallbackIcon();
		}

		private static string GetFallbackIcon()
		{
			const string svg = "<svg xmlns='http://www.w3.org/2000/svg' width='32' height='32'><circle cx='16' cy='16' r='14' fill='#1e88e5'/></svg>";
			var bytes = System.Text.Encoding.UTF8.GetBytes(svg);
			return $"data:image/svg+xml;base64,{Convert.ToBase64String(bytes)}";
		}
	}
}
'@

Set-Content "Client\MapTemplate.cs" -Value $content -NoNewline
Write-Host "MapTemplate.cs created!" -ForegroundColor Green
