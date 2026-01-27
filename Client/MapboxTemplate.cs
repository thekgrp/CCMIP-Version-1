using System;
using System.Globalization;

namespace CoreCommandMIP.Client
{
	internal static class MapboxTemplate
	{
		internal static string GetMapHtml(string accessToken)
		{
			// Load icons using MapTemplate's icon loading logic
			System.Diagnostics.Debug.WriteLine("=== Loading Track Icons for Mapbox ===");
			var personIcon = MapTemplate.LoadIconDataUri("person.png");
			var vehicleIcon = MapTemplate.LoadIconDataUri("vehicle.png");
			var droneIcon = MapTemplate.LoadIconDataUri("drone.png");
			var aerialIcon = MapTemplate.LoadIconDataUri("aerial.jpg");
			var birdIcon = MapTemplate.LoadIconDataUri("bird.png");
			var arrowIcon = MapTemplate.LoadIconDataUri("arrow.png");
			System.Diagnostics.Debug.WriteLine("=== Mapbox Icons Loaded ===");

			var html = $@"<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='utf-8' />
<title>Remote Track Map - Mapbox</title>
<meta name='viewport' content='initial-scale=1,maximum-scale=1,user-scalable=no' />
<script src='https://api.mapbox.com/mapbox-gl-js/v3.0.1/mapbox-gl.js'></script>
<link href='https://api.mapbox.com/mapbox-gl-js/v3.0.1/mapbox-gl.css' rel='stylesheet' />
<style>
html, body, #map {{ height: 100%; margin: 0; padding: 0; background-color: #1b1b1b; }}
.mapboxgl-ctrl-attrib {{ font-size: 10px; }}
.track-icon {{
    width: 32px;
    height: 32px;
    display: block;
    filter: brightness(1.2) contrast(1.3) drop-shadow(0 0 3px rgba(255,255,255,0.8)) drop-shadow(0 2px 6px rgba(0,0,0,0.9));
}}
.track-label {{
    background: rgba(0,0,0,0.8);
    color: white;
    padding: 4px 8px;
    border-radius: 4px;
    font-size: 12px;
    font-family: 'Segoe UI', sans-serif;
    white-space: nowrap;
    box-shadow: 0 2px 4px rgba(0,0,0,0.3);
}}
</style>
</head>
<body>
<div id='map'></div>
<script>
mapboxgl.accessToken = '{accessToken}';

const map = new mapboxgl.Map({{
    container: 'map',
    style: 'mapbox://styles/mapbox/satellite-streets-v12',
    center: [__LON__, __LAT__],
    zoom: __ZOOM__,
    pitch: 0,
    bearing: 0
}});

map.addControl(new mapboxgl.NavigationControl());
map.addControl(new mapboxgl.FullscreenControl());

const DEFAULT_TAIL = __TAIL__;
let userZoom = __ZOOM__;
let applyZoomOnNextUpdate = false;
let regionLayers = [];
let trackMarkers = {{}};

// Icon mapping by classification type
const iconMap = {{
    'person': '__PERSON_ICON__',
    'vehicle': '__VEHICLE_ICON__',
    'drone': '__DRONE_ICON__',
    'aerial': '__AERIAL_ICON__',
    'bird': '__BIRD_ICON__',
    'animal': '__BIRD_ICON__',
    'unknown': '__ARROW_ICON__'
}};

console.log('=== Mapbox Icon Map Loaded ===');
console.log('Person icon length:', iconMap['person'].length);

function getIconUrl(classification) {{
    const key = (classification || 'unknown').toLowerCase();
    const url = iconMap[key] || iconMap['unknown'];
    console.log('getIconUrl:', classification, 'URL length:', url ? url.length : 0);
    return url;
}}

map.on('zoomend', () => {{
    userZoom = map.getZoom();
}});

window.getCurrentZoom = () => map.getZoom();
window.setApplyZoom = (shouldApply) => {{
    applyZoomOnNextUpdate = shouldApply;
}};

window.clearRegions = () => {{
    console.log('Clearing', regionLayers.length, 'regions');
    regionLayers.forEach(layerId => {{
        if (map.getLayer(layerId)) map.removeLayer(layerId);
        if (map.getSource(layerId)) map.removeSource(layerId);
    }});
    regionLayers = [];
}};

window.addRegion = (region) => {{
    if (!region || !region.vertices || region.vertices.length < 3) return;
    
    try {{
        const layerId = 'region-' + Math.random().toString(36).substr(2, 9);
        const coordinates = region.vertices.map(v => [v.lng, v.lat]);
        coordinates.push(coordinates[0]);
        
        const color = region.color || '#ff6b6b';
        const fillOpacity = (region.fill >= 0 && region.fill <= 1) ? region.fill : 0.2;
        
        map.addSource(layerId, {{
            type: 'geojson',
            data: {{
                type: 'Feature',
                geometry: {{ type: 'Polygon', coordinates: [coordinates] }},
                properties: {{ name: region.name || 'Region', exclusion: region.exclusion || false }}
            }}
        }});
        
        map.addLayer({{
            id: layerId + '-fill',
            type: 'fill',
            source: layerId,
            paint: {{ 'fill-color': color, 'fill-opacity': fillOpacity }}
        }});
        
        map.addLayer({{
            id: layerId + '-outline',
            type: 'line',
            source: layerId,
            paint: {{ 'line-color': color, 'line-width': 2, 'line-opacity': 0.8, 'line-dasharray': region.exclusion ? [2, 2] : [1, 0] }}
        }});
        
        regionLayers.push(layerId + '-fill', layerId + '-outline', layerId);
        
        // Disable region click popup - we only want track popups
        // map.on('click', layerId + '-fill', (e) => {{
        //     const name = e.features[0].properties.name;
        //     const type = e.features[0].properties.exclusion ? 'Exclusion Zone' : 'Inclusion Zone';
        //     new mapboxgl.Popup().setLngLat(e.lngLat).setHTML('<b>' + name + '</b><br/>' + type).addTo(map);
        // }});
    }} catch (e) {{ console.error('Error adding region:', e); }}
}};

window.updateTracks = (tracks) => {{
    if (!tracks || tracks.length === 0) return;
    
    const bounds = new mapboxgl.LngLatBounds();
    
    tracks.forEach(track => {{
        const trackId = track.id;
        const lngLat = [track.lng, track.lat];
        bounds.extend(lngLat);
        
        const tailLimit = (track.tail && track.tail > 0) ? track.tail : DEFAULT_TAIL;
        
        if (!trackMarkers[trackId]) {{
            console.log('Creating marker for track', trackId, 'classification:', track.classification);
            
            const iconUrl = getIconUrl(track.classification);
            const el = document.createElement('img');
            el.src = iconUrl;
            el.className = 'track-icon';
            el.style.cursor = 'pointer';
            
            const marker = new mapboxgl.Marker({{element: el}})
                .setLngLat(lngLat)
                .setPopup(new mapboxgl.Popup({{offset: 15}}).setHTML('<b>' + track.label + '</b><br/>' + track.details))
                .addTo(map);
            
            trackMarkers[trackId] = {{
                marker: marker,
                tailPoints: [lngLat],
                line: null,
                classification: track.classification
            }};
        }} else {{
            const trackData = trackMarkers[trackId];
            trackData.marker.setLngLat(lngLat);
            
            if (trackData.classification !== track.classification) {{
                trackData.marker.getElement().src = getIconUrl(track.classification);
                trackData.classification = track.classification;
            }}
            
            trackData.marker.setPopup(new mapboxgl.Popup({{offset: 15}}).setHTML('<b>' + track.label + '</b><br/>' + track.details));
            trackData.tailPoints.push(lngLat);
            if (trackData.tailPoints.length > tailLimit) trackData.tailPoints.shift();
            
            const lineId = 'track-line-' + trackId;
            if (map.getSource(lineId)) {{
                map.getSource(lineId).setData({{ type: 'Feature', geometry: {{ type: 'LineString', coordinates: trackData.tailPoints }} }});
            }} else {{
                map.addSource(lineId, {{ type: 'geojson', data: {{ type: 'Feature', geometry: {{ type: 'LineString', coordinates: trackData.tailPoints }} }} }});
                map.addLayer({{ id: lineId, type: 'line', source: lineId, paint: {{ 'line-color': track.color || '#1e88e5', 'line-width': 3, 'line-opacity': 0.8 }} }});
                trackData.line = lineId;
            }}
        }}
    }});
    
    if (applyZoomOnNextUpdate && !bounds.isEmpty()) {{
        map.fitBounds(bounds, {{padding: 50}});
        applyZoomOnNextUpdate = false;
    }}
}};

window.clearInactiveTracks = (activeIds) => {{
    Object.keys(trackMarkers).forEach(trackId => {{
        if (!activeIds.includes(parseInt(trackId))) {{
            const trackData = trackMarkers[trackId];
            trackData.marker.remove();
            if (trackData.line && map.getLayer(trackData.line)) {{
                map.removeLayer(trackData.line);
                map.removeSource(trackData.line);
            }}
            delete trackMarkers[trackId];
        }}
    }});
}};

window.clearAllTracks = () => {{
    Object.keys(trackMarkers).forEach(trackId => {{
        const trackData = trackMarkers[trackId];
        trackData.marker.remove();
        if (trackData.line && map.getLayer(trackData.line)) {{
            map.removeLayer(trackData.line);
            map.removeSource(trackData.line);
        }}
    }});
    trackMarkers = {{}};
}};
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
