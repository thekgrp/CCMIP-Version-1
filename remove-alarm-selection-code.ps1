$file = "Client\CoreCommandMIPViewItemWpfUserControl.xaml.cs"
$content = Get-Content $file -Raw

Write-Host "Removing alarm selection handler code (not supported by Milestone MIP SDK)..."

# Remove the entire AlarmSelectionIndicationHandler and HighlightTrackOnMapAsync methods
$pattern = '[\r\n\t]+/// <summary>[\r\n\t]+/// Handle alarm selection from Alarm Manager[\r\n\t]+/// </summary>[\r\n\t]+private object AlarmSelectionIndicationHandler.*?private async Task HighlightTrackOnMapAsync.*?\r\n\t\}\r\n'

if ($content -match $pattern) {
    $content = $content -replace $pattern, ''
    Write-Host "? Removed alarm handler methods"
} else {
    Write-Host "Pattern not found, trying alternative..."
    # Try removing just from the start of the comment
    $startPattern = '\r\n\t/// <summary>\r\n\t/// Handle alarm selection from Alarm Manager'
    $start = $content.IndexOf($startPattern)
    
    if ($start -ge 0) {
        # Find the end - look for the next #endregion after this point
        $searchFrom = $start + 100
        $endPattern = '\r\n    #endregion\r\n\r\n    #endregion'
        $end = $content.IndexOf($endPattern, $searchFrom)
        
        if ($end -ge 0) {
            # Remove everything from start to just before the double endregion
            $toRemove = $content.Substring($start, $end - $start)
            $content = $content.Replace($toRemove, '')
            Write-Host "? Removed alarm handler methods (alternative method)"
        } else {
            Write-Host "Could not find end marker"
        }
    } else {
        Write-Host "Could not find start of alarm handlers - may already be removed"
    }
}

Set-Content $file -Value $content -NoNewline
Write-Host "? File updated"
Write-Host "`nAlarm selection code removed. This feature isn't available in Milestone MIP SDK."
Write-Host "Operators will use track list selection instead (which already works!)."
