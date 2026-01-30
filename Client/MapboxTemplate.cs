using System;
using System.Globalization;

namespace CoreCommandMIP.Client
{
	internal static class MapboxTemplate
	{
		internal static string GetMapHtml(string accessToken)
		{
			// Load icons BEFORE building HTML
			System.Diagnostics.Debug.WriteLine("=== Loading Track Icons for Mapbox ===");
			var personIcon = MapTemplate.LoadIconDataUri("person.png");
			var vehicleIcon = MapTemplate.LoadIconDataUri("vehicle.png");
			var droneIcon = MapTemplate.LoadIconDataUri("drone.png");
			var aerialIcon = MapTemplate.LoadIconDataUri("aerial.jpg");
			var birdIcon = MapTemplate.LoadIconDataUri("bird.png");
			var arrowIcon = MapTemplate.LoadIconDataUri("arrow.png");
			System.Diagnostics.Debug.WriteLine("=== Mapbox Icons Loaded ===");

			// Build HTML with placeholders
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
    filter: brightness(1.2) contrast(1.3) drop-shadow(0 0 3px rgba(255,255,255,0.8));
}}
</style>
</head>
<body>
<div id='map'></div>
<script>
mapboxgl.accessToken = '{accessToken}';

const map = new mapboxgl.Map({{
    container: 'map',
    style: '__3D_ENABLED__' === 'True' ? 'mapbox://styles/mapbox/satellite-streets-v12' : 'mapbox://styles/mapbox/streets-v12',
    center: [__LON__, __LAT__],
    zoom: __ZOOM__,
    pitch: __PITCH__,
    bearing: __BEARING__
}});

map.addControl(new mapboxgl.NavigationControl());
map.addControl(new mapboxgl.FullscreenControl());

// 3D features
'__3D_ENABLED__' === 'True' && map.on('load', () => {{
    if ('__3D_TERRAIN__' === 'True') {{
        map.addSource('mapbox-dem', {{ type: 'raster-dem', url: 'mapbox://mapbox.mapbox-terrain-dem-v1', tileSize: 512, maxzoom: 14 }});
        map.setTerrain({{ source: 'mapbox-dem', exaggeration: 1.5 }});
    }}
    if ('__3D_BUILDINGS__' === 'True') {{
        const layers = map.getStyle().layers;
        const labelLayerId = layers.find(l => l.type === 'symbol' && l.layout['text-field']).id;
        map.addLayer({{ id: '3d-buildings', source: 'composite', 'source-layer': 'building', filter: ['==', 'extrude', 'true'], type: 'fill-extrusion', minzoom: 15, paint: {{ 'fill-extrusion-color': '#aaa', 'fill-extrusion-height': ['interpolate', ['linear'], ['zoom'], 15, 0, 15.05, ['get', 'height']], 'fill-extrusion-base': ['interpolate', ['linear'], ['zoom'], 15, 0, 15.05, ['get', 'min_height']], 'fill-extrusion-opacity': 0.6 }} }}, labelLayerId);
    }}
}});

const DEFAULT_TAIL = __TAIL__;
let userZoom = __ZOOM__;
let regionLayers = [];
let trackMarkers = {{}};

const iconMap = {{ person: '__PERSON_ICON__', vehicle: '__VEHICLE_ICON__', drone: '__DRONE_ICON__', aerial: '__AERIAL_ICON__', bird: '__BIRD_ICON__', animal: '__BIRD_ICON__', unknown: '__ARROW_ICON__' }};

window.clearRegions = () => {{ regionLayers.forEach(id => {{ if(map.getLayer(id)) map.removeLayer(id); if(map.getSource(id)) map.removeSource(id); }}); regionLayers = []; }};
window.addRegion = (r) => {{ if(!r || !r.vertices || r.vertices.length < 3) return; try {{ const id = 'region-' + Math.random().toString(36).substr(2, 9); const coords = r.vertices.map(v => [v.lng, v.lat]); coords.push(coords[0]); map.addSource(id, {{ type: 'geojson', data: {{ type: 'Feature', geometry: {{ type: 'Polygon', coordinates: [coords] }}, properties: {{ name: r.name, exclusion: r.exclusion }} }} }}); map.addLayer({{ id: id + '-fill', type: 'fill', source: id, paint: {{ 'fill-color': r.color || '#ff0000', 'fill-opacity': r.fill || 0.2 }} }}); map.addLayer({{ id: id + '-outline', type: 'line', source: id, paint: {{ 'line-color': r.color || '#ff0000', 'line-width': 2, 'line-dasharray': r.exclusion ? [2,2] : null }} }}); regionLayers.push(id, id+'-fill', id+'-outline'); }} catch(e) {{ console.error(e); }} }};
window.updateTracks = (tracks) => {{ if(!tracks || !tracks.length) return; tracks.forEach(t => {{ const id = t.id; const pos = [t.lng, t.lat]; if(!trackMarkers[id]) {{ const el = document.createElement('img'); el.src = iconMap[t.classification] || iconMap.unknown; el.className = 'track-icon'; trackMarkers[id] = {{ marker: new mapboxgl.Marker({{element: el}}).setLngLat(pos).addTo(map), tailPoints: [pos], line: null }}; }} else {{ trackMarkers[id].marker.setLngLat(pos); }} }}); }};
window.clearAllTracks = () => {{ Object.values(trackMarkers).forEach(t => t.marker.remove()); trackMarkers = {{}}; }};
window.clearInactiveTracks = (ids) => {{ Object.keys(trackMarkers).forEach(id => {{ if(!ids.includes(parseInt(id))) {{ trackMarkers[id].marker.remove(); delete trackMarkers[id]; }} }}); }};
</script>
</body>
</html>";

			// Replace icon placeholders
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