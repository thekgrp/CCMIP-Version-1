# Oregon Zoo Default Coordinates - Complete! ?

## Date: 2025-01-XX
## Change: Updated default map location to Oregon Zoo
## Status: ? COMPLETE

---

## Summary

Changed default map coordinates from (0, 0) to Oregon Zoo location with better labels and appropriate zoom level for viewing a specific site.

---

## Changes Made

### 1. ? Updated Label Text
```csharp
// BEFORE:
"Default Latitude:"
"Default Longitude:"
"Default Zoom:"

// AFTER:
"Latitude:"
"Longitude:"
"Zoom:"
```

**Reason:** Simpler, cleaner labels. "Default" is redundant in this context.

### 2. ? Set Oregon Zoo as Default Location
```csharp
// Oregon Zoo Coordinates
Latitude:  45.5098
Longitude: -122.7161
Zoom:      14
```

**Location:** Portland, Oregon, USA (Oregon Zoo)

### 3. ? Updated All Occurrences

#### CreateMapRegionsTab() - Initial Values
```csharp
textBoxLatitude = new TextBox { Text = "45.5098" };   // Oregon Zoo
textBoxLongitude = new TextBox { Text = "-122.7161" }; // Oregon Zoo
textBoxZoom = new TextBox { Text = "14" };             // Good zoom level
```

#### ClearContent() - Reset Values
```csharp
textBoxLatitude.Text = "45.5098";   // Oregon Zoo
textBoxLongitude.Text = "-122.7161"; // Oregon Zoo
textBoxZoom.Text = "14";             // Good zoom for viewing a specific location
```

#### CollectCurrentSettings() - Fallback Values
```csharp
DefaultLatitude = ParseDoubleOrDefault(textBoxLatitude.Text, 45.5098),   // Oregon Zoo
DefaultLongitude = ParseDoubleOrDefault(textBoxLongitude.Text, -122.7161), // Oregon Zoo
DefaultZoomLevel = ParseDoubleOrDefault(textBoxZoom.Text, 14),
```

#### SaveToItem() - Fallback Values
```csharp
settings.DefaultLatitude = ParseDoubleOrDefault(textBoxLatitude.Text, 45.5098);   // Oregon Zoo
settings.DefaultLongitude = ParseDoubleOrDefault(textBoxLongitude.Text, -122.7161); // Oregon Zoo
settings.DefaultZoomLevel = ParseDoubleOrDefault(textBoxZoom.Text, 14);
```

---

## Why Oregon Zoo?

### Benefits:
1. **Real Location** - Not the middle of the ocean (0, 0)
2. **Interesting Site** - A well-known location users can recognize
3. **Good Test Case** - Urban area with buildings, roads, terrain
4. **Appropriate Zoom** - Zoom 14 shows the entire zoo clearly

### Previous Default Issues:
- **Latitude: 0, Longitude: 0** ? Middle of Atlantic Ocean off Africa
- **Zoom: 8** ? Too far out to see specific location details
- Not useful for testing or demonstration

### New Default Advantages:
- **Shows Portland metro area** at zoom 14
- **Clear landmark** (Oregon Zoo is easily identifiable)
- **Urban features** to test map functionality
- **Good for screenshots/demos**

---

## Zoom Level Reference

| Zoom | View Scale | Example |
|------|-----------|---------|
| 1-5  | World/Continent | World map |
| 6-8  | Country/State | Oregon state |
| 9-11 | City | Portland metro |
| 12-14 | Neighborhood | **Oregon Zoo** ? |
| 15-17 | Street | Individual buildings |
| 18-20 | Building | Rooms/parking spots |

**Zoom 14** is perfect for viewing a specific site like a zoo, campus, or facility!

---

## Map Preview Behavior

### On First Load:
- Shows Oregon Zoo at center
- Zoom level 14 (can see entire zoo grounds)
- Marker at center of zoo
- Site name popup

### When User Changes Coordinates:
- Map updates in real-time
- New coordinates shown with marker
- Preview updates immediately

### Example Usage:
```
Site: "Portland Zoo Surveillance"
Lat:  45.5098
Lon:  -122.7161
Zoom: 14
? Map shows Oregon Zoo with marker
```

---

## Files Modified

### Admin/CoreCommandMIPUserControlTabbed.cs

#### Changes:
1. ? Updated label text (removed "Default" prefix)
2. ? Changed initial textbox values to Oregon Zoo
3. ? Updated ClearContent() defaults
4. ? Updated CollectCurrentSettings() fallbacks
5. ? Updated SaveToItem() fallbacks
6. ? Added comments explaining Oregon Zoo coordinates

---

## User Experience

### Before:
- ? Default showed Atlantic Ocean (0, 0)
- ? Zoom 8 too far out
- ? Not useful for testing
- ? Labels had redundant "Default" word

### After:
- ? Shows Oregon Zoo (45.5098, -122.7161)
- ? Zoom 14 perfect for site view
- ? Great for testing and demos
- ? Cleaner, shorter labels

---

## Testing Checklist

- [x] Build succeeds
- [x] Map preview shows Oregon Zoo on load
- [x] Marker at center of zoo
- [x] Zoom level 14 shows good detail
- [x] Labels are shorter ("Latitude" not "Default Latitude")
- [x] ClearContent() resets to Oregon Zoo
- [x] User can still change to any coordinates
- [x] Map updates in real-time when changed

---

## Oregon Zoo Location Details

**Full Address:**
```
Oregon Zoo
4001 SW Canyon Road
Portland, OR 97221
United States
```

**Coordinates:**
```
Latitude:  45.5098°N
Longitude: 122.7161°W
```

**Features Visible at Zoom 14:**
- Entire zoo grounds
- Parking lots
- Surrounding forest (Washington Park)
- Road access
- Nearby neighborhoods

**Good Test Cases:**
- Urban area with buildings
- Mixed terrain (hills, forest)
- Known landmark (easy to verify)
- Good tile coverage from OSM/Mapbox

---

## Alternative Default Locations

If you want to change the default to another location:

### Portland City Center:
```csharp
Latitude:  45.5152
Longitude: -122.6784
Zoom:      13
```

### Microsoft Campus (Redmond):
```csharp
Latitude:  47.6423
Longitude: -122.1304
Zoom:      14
```

### Your Actual Site:
```csharp
Latitude:  [Your site latitude]
Longitude: [Your site longitude]
Zoom:      12-16 (depending on site size)
```

---

## Build Status

? **Build Successful**  
? **No compilation errors**  
? **All changes applied**  
? **Map preview working**  

---

## Summary

?? **Goal:** Better default coordinates for map preview  
? **Result:** Oregon Zoo (45.5098, -122.7161) at zoom 14  
??? **Labels:** Shortened to "Latitude", "Longitude", "Zoom"  
?? **Documentation:** Complete  

---

**The map preview now shows Oregon Zoo by default - a much better starting point than the middle of the ocean!** ?????

Users can immediately see what the map looks like with a real location, and the zoom level is perfect for viewing a specific site or facility!
