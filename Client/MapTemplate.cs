using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace CoreCommandMIP.Client
{
	internal static class MapTemplate
	{
    private static Dictionary<string, string> _iconCache = new Dictionary<string, string>();

    // Public method for loading icons (used by MapboxTemplate too)
    internal static string LoadIconDataUri(string iconName)
    {
        return GetIconDataUri(iconName);
    }

    private static string GetIconDataUri(string iconName)
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

            System.Diagnostics.Debug.WriteLine($"=== Searching for {iconName} ===");
            System.Diagnostics.Debug.WriteLine($"Assembly path: {assemblyPath}");
            System.Diagnostics.Debug.WriteLine($"Assembly dir: {assemblyDir}");
            System.Diagnostics.Debug.WriteLine($"Looking for icon: {iconPath}");
            System.Diagnostics.Debug.WriteLine($"File exists: {File.Exists(iconPath)}");
            
            // Also check if assets folder exists
            var assetsDir = Path.Combine(assemblyDir, "assets");
            System.Diagnostics.Debug.WriteLine($"Assets folder exists: {Directory.Exists(assetsDir)}");
            if (Directory.Exists(assetsDir))
            {
                var files = Directory.GetFiles(assetsDir);
                System.Diagnostics.Debug.WriteLine($"Files in assets folder: {files.Length}");
                foreach (var file in files)
                {
                    System.Diagnostics.Debug.WriteLine($"  - {Path.GetFileName(file)}");
                }
            }

            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var base64 = Convert.ToBase64String(bytes);
                var extension = Path.GetExtension(iconName).ToLowerInvariant();
                var mimeType = extension == ".png" ? "image/png" : "image/jpeg";
                var dataUri = $"data:{mimeType};base64,{base64}";
                _iconCache[iconName] = dataUri;
                System.Diagnostics.Debug.WriteLine($"? Successfully loaded {iconName}: {bytes.Length} bytes");
                return dataUri;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"? File NOT FOUND: {iconPath}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Error loading icon {iconName}: {ex.Message}");
        }

        // Return fallback SVG icon if file not found or error occurred
        System.Diagnostics.Debug.WriteLine($"Using fallback SVG icon for {iconName}");
        return GetFallbackIcon();
    }

		private static string GetFallbackIcon()
		{
			// Simple SVG arrow as fallback
			const string svgArrow = "<svg xmlns='http://www.w3.org/2000/svg' width='32' height='32' viewBox='0 0 32 32'><circle cx='16' cy='16' r='14' fill='#1e88e5' stroke='white' stroke-width='2'/><path d='M16 8 L16 20 M11 15 L16 20 L21 15' stroke='white' stroke-width='2' fill='none'/></svg>";
			var bytes = System.Text.Encoding.UTF8.GetBytes(svgArrow);
			var base64 = Convert.ToBase64String(bytes);
			return $"data:image/svg+xml;base64,{base64}";
		}

	internal static string GetMapHtml()
	{
		// Pre-load all icons
		System.Diagnostics.Debug.WriteLine("=== Loading Track Icons ===");
		var personIcon = GetIconDataUri("person.png") ?? "";
		System.Diagnostics.Debug.WriteLine($"Person icon loaded: {personIcon.Length} chars, starts with: {(personIcon.Length > 30 ? personIcon.Substring(0, 30) : personIcon)}");
		
		var vehicleIcon = GetIconDataUri("vehicle.png") ?? "";
		System.Diagnostics.Debug.WriteLine($"Vehicle icon loaded: {vehicleIcon.Length} chars");
		
		var droneIcon = GetIconDataUri("drone.png") ?? "";
		System.Diagnostics.Debug.WriteLine($"Drone icon loaded: {droneIcon.Length} chars");
		
		var aerialIcon = GetIconDataUri("aerial.jpg") ?? "";
		System.Diagnostics.Debug.WriteLine($"Aerial icon loaded: {aerialIcon.Length} chars");
		
		var birdIcon = GetIconDataUri("bird.png") ?? "";
		System.Diagnostics.Debug.WriteLine($"Bird icon loaded: {birdIcon.Length} chars");
		
		var arrowIcon = GetIconDataUri("arrow.png") ?? "";
		System.Diagnostics.Debug.WriteLine($"Arrow icon loaded: {arrowIcon.Length} chars");
		System.Diagnostics.Debug.WriteLine("=== All Icons Loaded ===");

			var html = @"<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='utf-8' />
<title>Remote Track Map</title>
<link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css' />
<style>
html, body, #map { height: 100%; margin: 0; padding: 0; background-color: #1b1b1b; }
.leaflet-container { font-family: 'Segoe UI', sans-serif; }
.track-icon {
    filter: brightness(1.2) contrast(1.3) drop-shadow(0 0 3px rgba(255,255,255,0.8)) drop-shadow(0 2px 6px rgba(0,0,0,0.9));
}
.track-label {
    font-size: 11px;
    white-space: nowrap;
}
</style>
</head>
<body>
<div id='map'></div>
<script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
<script>
var map = L.map('map', { zoomControl: true }).setView([__LAT__, __LON__], __ZOOM__);

L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    maxZoom: 19,
    attribution: '&copy; OpenStreetMap contributors',
    subdomains: ['a', 'b', 'c']
}).addTo(map);

var defaultTail = __TAIL__;
var userZoom = __ZOOM__;
var applyZoomOnNextUpdate = false;
var regionPolygons = [];
var trackLayers = {};

// Icon mapping by classification type
var iconMap = {
    'person': '__PERSON_ICON__',
    'vehicle': '__VEHICLE_ICON__',
    'drone': '__DRONE_ICON__',
    'aerial': '__AERIAL_ICON__',
    'bird': '__BIRD_ICON__',
    'animal': '__BIRD_ICON__',
    'unknown': '__ARROW_ICON__'
};

console.log('=== Icon Map Loaded ===');
console.log('Person icon length:', iconMap['person'].length);
console.log('Vehicle icon length:', iconMap['vehicle'].length);
console.log('Drone icon length:', iconMap['drone'].length);
console.log('Arrow icon length:', iconMap['unknown'].length);
console.log('Person icon starts with:', iconMap['person'].substring(0, 30));

function getIconUrl(classification) {
    var key = (classification || 'unknown').toLowerCase();
    var url = iconMap[key] || iconMap['unknown'];
    console.log('getIconUrl called with classification:', classification, 'using key:', key, 'URL length:', url ? url.length : 0);
    return url;
}

map.on('zoomend', function() {
    userZoom = map.getZoom();
});

window.getCurrentZoom = function() {
    return map.getZoom();
};

window.setApplyZoom = function(shouldApply) {
    applyZoomOnNextUpdate = shouldApply;
};

window.clearRegions = function() {
    console.log('Clearing', regionPolygons.length, 'regions');
    regionPolygons.forEach(function(polygon) {
        map.removeLayer(polygon);
    });
    regionPolygons = [];
    console.log('Regions cleared');
};

window.addRegion = function(region) {
    console.log('addRegion called with:', JSON.stringify(region));
    if (!region || !region.vertices || region.vertices.length < 3) {
        console.log('Region skipped - invalid data:', region);
        return;
    }
    console.log('Adding region:', region.name, 'with', region.vertices.length, 'vertices');
    try {
        var latLngs = region.vertices.map(function(v) { 
            console.log('Vertex:', v);
            return [v.lat, v.lng]; 
        });
        console.log('LatLngs:', latLngs);
        var color = region.color || '#ff6b6b';
        var fillOpacity = region.fill >= 0 && region.fill <= 1 ? region.fill : 0.2;
        console.log('Creating polygon with color:', color, 'fill:', fillOpacity);
        var polygon = L.polygon(latLngs, {
            color: color,
            weight: 2,
            opacity: 0.8,
            fillColor: color,
            fillOpacity: fillOpacity,
            dashArray: region.exclusion ? '5, 10' : null
        });
        console.log('Polygon created, adding to map');
        polygon.addTo(map);
        // Disable region popup - we only want track popups
        // if (region.name) {
        //     polygon.bindPopup('<b>' + region.name + '</b><br/>' + (region.exclusion ? 'Exclusion Zone' : 'Inclusion Zone'));
        // }
        regionPolygons.push(polygon);
        console.log('Region added successfully. Total regions:', regionPolygons.length);
    } catch (e) {
        console.error('Error adding region:', e);
    }
};

window.updateTracks = function(tracks) {{
    if (!tracks || tracks.length === 0) {{
        return;
    }}
    var bounds = [];
    tracks.forEach(function(track) {{
        var trackId = track.id;
        var latLng = [track.lat, track.lng];
        bounds.push(latLng);
        var tailLimit = track.tail && track.tail > 0 ? track.tail : defaultTail;
        
        if (!trackLayers[trackId]) {{
            var iconUrl = getIconUrl(track.classification);
            console.log('Creating new track marker for ID', trackId, 'classification:', track.classification, 'iconUrl length:', iconUrl ? iconUrl.length : 0);
            console.log('IconUrl starts with:', iconUrl ? iconUrl.substring(0, 50) : 'EMPTY');
            
            var trackIcon = L.icon({{
                iconUrl: iconUrl,
                iconSize: [32, 32],
                iconAnchor: [16, 16],
                className: 'track-icon'
            }});
            
            console.log('Track icon object created:', trackIcon);
            
            trackLayers[trackId] = {{
                line: L.polyline([], {{ color: track.color || '#1e88e5', weight: 3, opacity: 0.8 }}).addTo(map),
                marker: L.marker(latLng, {{ icon: trackIcon, rotationAngle: 0 }}).addTo(map),
                label: L.marker(latLng, {{ 
                    icon: L.divIcon({{ 
                        className: 'track-label',
                        html: '<div style=""background:rgba(0,0,0,0.7);color:white;padding:2px 6px;border-radius:3px;font-size:11px;white-space:nowrap;"">' + track.label + '</div>',
                        iconSize: [60, 20],
                        iconAnchor: [30, -10]
                    }})
                }}).addTo(map),
                tailPoints: [],
                lastBearing: 0,
                classification: track.classification
            }};
            
            console.log('Track marker added to map for ID', trackId);
        }}
        
        var layer = trackLayers[trackId];
        var bearing = 0;
        if (layer.tailPoints.length > 0) {{
            var lastPoint = layer.tailPoints[layer.tailPoints.length - 1];
            bearing = calculateBearing(lastPoint[0], lastPoint[1], latLng[0], latLng[1]);
            layer.lastBearing = bearing;
        }} else if (layer.lastBearing) {{
            bearing = layer.lastBearing;
        }}
        
        layer.tailPoints.push(latLng);
        if (layer.tailPoints.length > tailLimit) {{
            layer.tailPoints.shift();
        }}
        
        layer.line.setLatLngs(layer.tailPoints);
        layer.line.setStyle({{ color: track.color || '#1e88e5' }});
        
        // Update icon if classification changed
        if (layer.classification !== track.classification) {{
            var iconUrl = getIconUrl(track.classification);
            var trackIcon = L.icon({{
                iconUrl: iconUrl,
                iconSize: [32, 32],
                iconAnchor: [16, 16],
                className: 'track-icon'
            }});
            layer.marker.setIcon(trackIcon);
            layer.classification = track.classification;
        }}
        
        // Apply rotation to icon (using CSS transform if supported)
        layer.marker.setLatLng(latLng);
        var iconElement = layer.marker.getElement();
        if (iconElement) {{
            iconElement.style.transform = 'rotate(' + bearing + 'deg)';
            iconElement.style.transformOrigin = 'center center';
        }}
        
        layer.label.setLatLng(latLng);
        layer.label.setIcon(L.divIcon({{ 
            className: 'track-label',
            html: '<div style=""background:rgba(0,0,0,0.7);color:white;padding:2px 6px;border-radius:3px;font-size:11px;white-space:nowrap;"">' + track.label + '</div>',
            iconSize: [60, 20],
            iconAnchor: [30, -10]
        }}));
        
        var popup = '<b>' + track.label + '</b><br/>' + track.details;
        layer.marker.bindPopup(popup);
    }});
    
    if (applyZoomOnNextUpdate && bounds.length > 0) {{
        if (bounds.length === 1) {{
            map.setView(bounds[0], userZoom);
        }} else {{
            map.fitBounds(bounds, {{ padding: [50, 50] }});
        }}
        applyZoomOnNextUpdate = false;
    }}
}};

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
        if (activeIds.indexOf(parseInt(trackId)) === -1) {
            var layer = trackLayers[trackId];
            map.removeLayer(layer.line);
            map.removeLayer(layer.marker);
            map.removeLayer(layer.label);
            delete trackLayers[trackId];
        }
    });
};

window.clearAllTracks = function() {
    Object.keys(trackLayers).forEach(function(trackId) {
        var layer = trackLayers[trackId];
        map.removeLayer(layer.line);
        map.removeLayer(layer.marker);
        map.removeLayer(layer.label);
    });
    trackLayers = {};
};
</script>
</body>
</html>";

			// Replace icon placeholders with actual base64 data URIs
			html = html.Replace("__PERSON_ICON__", personIcon);
			html = html.Replace("__VEHICLE_ICON__", vehicleIcon);
			html = html.Replace("__DRONE_ICON__", droneIcon);
			html = html.Replace("__AERIAL_ICON__", aerialIcon);
			html = html.Replace("__BIRD_ICON__", birdIcon);
			html = html.Replace("__ARROW_ICON__", arrowIcon);

			return html;
		}
	}
}
