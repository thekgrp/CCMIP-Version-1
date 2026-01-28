using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace CoreCommandMIP.Client
{
	internal static class MapTemplate
	{
		private static readonly Dictionary<string, string> _iconCache = new Dictionary<string, string>();

		internal static string GetMapHtml()
		{
			System.Diagnostics.Debug.WriteLine("=== Loading Track Icons ===");
			var personIcon = LoadIconDataUri("person.png");
			var vehicleIcon = LoadIconDataUri("vehicle.png");
			var droneIcon = LoadIconDataUri("drone.png");
			var aerialIcon = LoadIconDataUri("aerial.jpg");
			var birdIcon = LoadIconDataUri("bird.png");
			var arrowIcon = LoadIconDataUri("arrow.png");
			System.Diagnostics.Debug.WriteLine("=== Icons Loaded ===");

			return @"<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='utf-8' />
<title>Remote Track Map</title>
<meta name='viewport' content='width=device-width, initial-scale=1.0'>
<link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css' />
<style>
html, body, #map { height: 100%; margin: 0; padding: 0; background-color: #1b1b1b; }
</style>
</head>
<body>
<div id='map'></div>
<script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
<script>
console.log('Initializing map...');

var defaultTail = __TAIL__;
var userZoom = __ZOOM__;
var regionPolygons = [];
var trackLayers = {};

const iconMap = {
    'person': '__PERSON_ICON__',
    'vehicle': '__VEHICLE_ICON__',
    'drone': '__DRONE_ICON__',
    'aerial': '__AERIAL_ICON__',
    'bird': '__BIRD_ICON__',
    'animal': '__BIRD_ICON__',
    'unknown': '__ARROW_ICON__'
};

function getIconUrl(classification) {
    const key = (classification || 'unknown').toLowerCase();
    return iconMap[key] || iconMap['unknown'];
}

var map = L.map('map').setView([__LAT__, __LON__], __ZOOM__);
L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    maxZoom: 19,
    attribution: '© OpenStreetMap'
}).addTo(map);

map.on('zoomend', function() {
    userZoom = map.getZoom();
});

window.getCurrentZoom = function() {
    return map.getZoom();
};

window.clearRegions = function() {
    console.log('Clearing', regionPolygons.length, 'regions');
    regionPolygons.forEach(function(p) { map.removeLayer(p); });
    regionPolygons = [];
};

window.addRegion = function(name, coords) {
    try {
        console.log('Adding region:', name, 'with', coords.length, 'points');
        var polygon = L.polygon(coords, {
            color: '#1e88e5',
            weight: 2,
            fillColor: '#1e88e5',
            fillOpacity: 0.1
        }).addTo(map);
        polygon.bindPopup('<b>' + name + '</b>');
        regionPolygons.push(polygon);
        console.log('Region added successfully');
    } catch (err) {
        console.error('Error adding region:', err);
    }
};

window.updateTracks = function(tracks) {
    try {
        if (!tracks || tracks.length === 0) {
            console.log('No tracks to display');
            window.clearAllTracks && window.clearAllTracks();
            return;
        }

        var activeIds = [];
        var bounds = [];

        tracks.forEach(function(track) {
            activeIds.push(track.id);
            
            var latLng = [track.lat, track.lon];
            bounds.push(latLng);
            
            var layer = trackLayers[track.id];
            if (!layer) {
                var iconUrl = getIconUrl(track.classification);
                var icon = L.icon({
                    iconUrl: iconUrl,
                    iconSize: [32, 32],
                    iconAnchor: [16, 16],
                    popupAnchor: [0, -16]
                });
                
                var marker = L.marker(latLng, { icon: icon }).addTo(map);
                var line = null;
                
                trackLayers[track.id] = {
                    marker: marker,
                    line: line,
                    tailPoints: []
                };
                layer = trackLayers[track.id];
            } else {
                layer.marker.setLatLng(latLng);
            }
            
            if (track.tailPoints && track.tailPoints.length > 0) {
                layer.tailPoints = track.tailPoints;
                
                if (layer.line) {
                    map.removeLayer(layer.line);
                }
                
                layer.line = L.polyline(track.tailPoints, {
                    color: track.color || '#1e88e5',
                    weight: 3,
                    opacity: 0.8
                }).addTo(map);
            }
            
            
            var popup = '<b>' + track.label + '</b><br/>' + track.details;
            layer.marker.bindPopup(popup);
        });
        
        // Map stays centered on site - no auto-panning to tracks
    } catch (err) {
        console.error('Error updating tracks:', err);
    }
};

function calculateBearing(lat1, lng1, lat2, lng2) {
    var dLon = (lng2 - lng1) * Math.PI / 180;
    var y = Math.sin(dLon) * Math.cos(lat2 * Math.PI / 180);
    var x = Math.cos(lat1 * Math.PI / 180) * Math.sin(lat2 * Math.PI / 180) -
            Math.sin(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) * Math.cos(dLon);
    var bearing = Math.atan2(y, x) * 180 / Math.PI;
    return (bearing + 360) % 360;
}

window.clearInactiveTracks = function(activeIds) {
    Object.keys(trackLayers).forEach(function(trackId) {
        if (!activeIds.includes(parseInt(trackId))) {
            var layer = trackLayers[trackId];
            map.removeLayer(layer.marker);
            if (layer.line) map.removeLayer(layer.line);
            delete trackLayers[trackId];
        }
    });
};

window.clearAllTracks = function() {
    Object.values(trackLayers).forEach(function(layer) {
        map.removeLayer(layer.marker);
        if (layer.line) map.removeLayer(layer.line);
    });
    trackLayers = {};
};

window.notifyMapReady = function() {
    console.log('Map ready, notifying host');
    window.chrome.webview.postMessage('MapReady');
};

console.log('Map initialized, posting ready message');
setTimeout(function() { window.notifyMapReady(); }, 100);
</script>
</body>
</html>"
				.Replace("__PERSON_ICON__", personIcon)
				.Replace("__VEHICLE_ICON__", vehicleIcon)
				.Replace("__DRONE_ICON__", droneIcon)
				.Replace("__AERIAL_ICON__", aerialIcon)
				.Replace("__BIRD_ICON__", birdIcon)
				.Replace("__ARROW_ICON__", arrowIcon);
		}

		internal static string LoadIconDataUri(string iconName)
		{
			if (_iconCache.ContainsKey(iconName))
			{
				return _iconCache[iconName];
			}

			try
			{
				var assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
				var assemblyDir = Path.GetDirectoryName(assemblyPath);
				var iconPath = Path.Combine(assemblyDir, "assets", iconName);

				System.Diagnostics.Debug.WriteLine($"Attempting to load icon: {iconPath}");

				if (File.Exists(iconPath))
				{
					var bytes = File.ReadAllBytes(iconPath);
					var base64 = Convert.ToBase64String(bytes);
					var extension = Path.GetExtension(iconName).ToLowerInvariant();
					var mimeType = extension == ".png" ? "image/png" : "image/jpeg";
					var dataUri = $"data:{mimeType};base64,{base64}";
					_iconCache[iconName] = dataUri;
					System.Diagnostics.Debug.WriteLine($"Icon loaded successfully: {iconName}, size: {bytes.Length} bytes");
					return dataUri;
				}
				else
				{
					System.Diagnostics.Debug.WriteLine($"Icon file not found: {iconPath}");
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Error loading icon {iconName}: {ex.Message}");
			}

			var fallback = GetFallbackIcon();
			_iconCache[iconName] = fallback;
			return fallback;
		}

		private static string GetFallbackIcon()
		{
			const string svgArrow = "<svg xmlns='http://www.w3.org/2000/svg' width='32' height='32' viewBox='0 0 32 32'><circle cx='16' cy='16' r='14' fill='#1e88e5' stroke='white' stroke-width='2'/><path d='M16 8 L16 20 M11 15 L16 20 L21 15' stroke='white' stroke-width='2' fill='none'/></svg>";
			var bytes = System.Text.Encoding.UTF8.GetBytes(svgArrow);
			var base64 = Convert.ToBase64String(bytes);
			return $"data:image/svg+xml;base64,{base64}";
		}
	}
}
