Write-Host "Restoring MapTemplate.cs..." -ForegroundColor Yellow

$content = @'
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
            var personIcon = LoadIconDataUri("person.png");
            var vehicleIcon = LoadIconDataUri("vehicle.png");
            var droneIcon = LoadIconDataUri("drone.png");
            var aerialIcon = LoadIconDataUri("aerial.jpg");
            var birdIcon = LoadIconDataUri("bird.png");
            var arrowIcon = LoadIconDataUri("arrow.png");

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

                if (File.Exists(iconPath))
                {
                    var bytes = File.ReadAllBytes(iconPath);
                    var base64 = Convert.ToBase64String(bytes);
                    var extension = Path.GetExtension(iconName).ToLowerInvariant();
                    var mimeType = extension == ".png" ? "image/png" : "image/jpeg";
                    var dataUri = $"data:{mimeType};base64,{base64}";
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

        internal static string LoadIconDataUri(string iconName)
        {
            return GetIconDataUri(iconName);
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
'@

Set-Content "Client\MapTemplate.cs" -Value $content -NoNewline
Write-Host "MapTemplate.cs restored!" -ForegroundColor Green
Write-Host "Now rebuild the project." -ForegroundColor Cyan
