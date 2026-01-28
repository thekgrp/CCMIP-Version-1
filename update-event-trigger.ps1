$file = "Background\TrackAlarmEventHandler.cs"
$content = Get-Content $file -Raw

Write-Host "Updating TrackAlarmEventHandler to use EventTriggerService..."

# Add EventTriggerService field
$old = 'private readonly FQID _pluginFqid;[\r\n\s]+public TrackAlarmEventHandler'
$new = @'
private readonly FQID _pluginFqid;
		private readonly EventTriggerService _eventTrigger;
		
	public TrackAlarmEventHandler
'@

if ($content -match $old) {
    $content = $content -replace $old, $new
    Write-Host "? Added EventTriggerService field"
} else {
    Write-Host "? Could not add EventTriggerService field - may already exist"
}

# Initialize EventTriggerService in constructor
$old = '_pluginFqid = new FQID\(\);[\r\n\s]+\}'
$new = @'
_pluginFqid = new FQID();
		
		// Create event trigger service
		_eventTrigger = new EventTriggerService(CoreCommandMIPDefinition.CoreCommandMIPPluginId);
	}
'@

if ($content -match $old) {
    $content = $content -replace $old, $new
    Write-Host "? Added EventTriggerService initialization"
} else {
    Write-Host "? Could not add initialization - may already exist"
}

Set-Content $file -Value $content -NoNewline
Write-Host "? TrackAlarmEventHandler updated!"
