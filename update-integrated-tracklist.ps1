$file = "Client\CoreCommandMIPViewItemWpfUserControl.xaml.cs"
$content = Get-Content $file -Raw

Write-Host "Adding DataGrid selection handler and updating HandleTrackUpdate..."

# Add the TrackListDataGrid_SelectionChanged method after the ViewItemWpfUserControl_MouseDoubleClick method
$addAfter = 'FireDoubleClickEvent\(\);[\r\n\s]+\}[\r\n\s]+#endregion'
$newMethod = @'
	}

	/// <summary>
	/// Handle track selection from the integrated track list
	/// </summary>
	private void TrackListDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (_trackListDataGrid.SelectedItem is SmartMapLocation selectedTrack)
		{
			_selectedTrackId = selectedTrack.TrackId;
			_lastLocation = selectedTrack;
			_shouldApplyZoomOnNextUpdate = true;
			UpdateLocationDetails(selectedTrack);
			PersistMetadata(selectedTrack);
			
			// Broadcast selection to other views
			var message = new TrackSelectionMessage(_viewItemManager.SomeId, selectedTrack);
			EnvironmentManager.Instance.PostMessage(
				new VideoOS.Platform.Messaging.Message(CoreCommandMIPDefinition.TrackSelectedMessageId, message),
				null, null);
				
			System.Diagnostics.Debug.WriteLine($"Track {selectedTrack.TrackId} selected from integrated list");
		}
	}

	#endregion
'@

if ($content -match $addAfter) {
    $content = $content -replace $addAfter, $newMethod
    Write-Host "? Added TrackListDataGrid_SelectionChanged handler"
} else {
    Write-Host "? Could not find insertion point for selection handler"
}

# Update HandleTrackUpdate to populate DataGrid
$oldUpdate = 'if \(meaningfulTracks.Count == 0\)[\r\n\s]+\{[\r\n\s]+_latestTracks = Array.Empty<SmartMapLocation>\(\);[\r\n\s]+_lastLocation = null;[\r\n\s]+UpdateLocationDetails\(null\);[\r\n\s]+_statusTextBlock.Text = tracks\?.FirstOrDefault\(\)\?.StatusMessage \?\? "No active tracks.";[\r\n\s]+BroadcastTrackList\(_latestTracks\);[\r\n\s]+return;[\r\n\s]+\}'

$newUpdate = @'
if (meaningfulTracks.Count == 0)
		{
			_latestTracks = Array.Empty<SmartMapLocation>();
			_lastLocation = null;
			UpdateLocationDetails(null);
			_statusTextBlock.Text = tracks?.FirstOrDefault()?.StatusMessage ?? "No active tracks.";
			BroadcastTrackList(_latestTracks);
			
			// Update DataGrid
			_trackListDataGrid.ItemsSource = null;
			_trackCountTextBlock.Text = "0 tracks";
			return;
		}
'@

if ($content -match $oldUpdate) {
    $content = $content -replace $oldUpdate, $newUpdate
    Write-Host "? Updated empty tracks handling"
} else {
    Write-Host "? Pattern not found for empty tracks - may need manual update"
}

# Add DataGrid population after _latestTracks assignment
$oldAssignment = '_latestTracks = meaningfulTracks;[\r\n\s]+// Process alarms'
$newAssignment = @'
_latestTracks = meaningfulTracks;
		
		// Update DataGrid with tracks
		_trackListDataGrid.ItemsSource = meaningfulTracks;
		_trackCountTextBlock.Text = $"{meaningfulTracks.Count} track{(meaningfulTracks.Count != 1 ? "s" : "")}";
		
		// Process alarms
'@

if ($content -match $oldAssignment) {
    $content = $content -replace $oldAssignment, $newAssignment
    Write-Host "? Added DataGrid population"
} else {
    Write-Host "? Could not add DataGrid population"
}

# Add DataGrid selection sync after nextTrack assignment
$oldNext = 'if \(_selectedTrackId.HasValue\)[\r\n\s]+\{[\r\n\s]+nextTrack = _latestTracks.FirstOrDefault\(t => t.TrackId == _selectedTrackId.Value\);[\r\n\s]+\}'
$newNext = @'
if (_selectedTrackId.HasValue)
		{
			nextTrack = _latestTracks.FirstOrDefault(t => t.TrackId == _selectedTrackId.Value);
			
			// Also select in DataGrid
			if (nextTrack != null)
			{
				_trackListDataGrid.SelectedItem = nextTrack;
				_trackListDataGrid.ScrollIntoView(nextTrack);
			}
		}
'@

if ($content -match $oldNext) {
    $content = $content -replace $oldNext, $newNext
    Write-Host "? Added DataGrid selection sync"
} else {
    Write-Host "? Could not add selection sync"
}

Set-Content $file -Value $content -NoNewline
Write-Host "`n??? Code-behind updated!"
Write-Host "Now building..."
