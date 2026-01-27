# Region DataGrid Update Guide

## Overview
This guide documents the changes needed to convert the region selection from a CheckedListBox to a DataGridView with columns for Name, Type (Alarm/Exclusion), and Selection checkbox.

## Changes Made to Code

### 1. RegionModels.cs - Updated ?
Added new properties to `RegionListItem`:
- `Exclusion` (bool) - Indicates if region is Exclusion type (true) or Alarm type (false)
- `IsSelected` (bool) - For DataGrid selection state
- `TypeDisplay` (string) - Display property that returns "Exclusion" or "Alarm"

### 2. RemoteServerDataProvider.cs - Updated ?
Modified `FetchRegionListAsync` to fetch and populate the `Exclusion` property from region details.

## UI Changes Needed in Admin\CoreCommandMIPUserControl

### Step 1: Replace CheckedListBox with DataGridView in Designer

**In `CoreCommandMIPUserControl.Designer.cs`:**

1. **Remove the old control declaration:**
```csharp
private System.Windows.Forms.CheckedListBox checkedListBoxRegions;
```

2. **Add the new control declaration:**
```csharp
private System.Windows.Forms.DataGridView dataGridViewRegions;
```

3. **In `InitializeComponent()` method, replace:**
```csharp
// OLD:
this.checkedListBoxRegions = new System.Windows.Forms.CheckedListBox();
this.checkedListBoxRegions.CheckOnClick = true;
this.checkedListBoxRegions.FormattingEnabled = true;
this.checkedListBoxRegions.Location = new System.Drawing.Point(16, 505);
this.checkedListBoxRegions.Name = "checkedListBoxRegions";
this.checkedListBoxRegions.Size = new System.Drawing.Size(347, 94);
this.checkedListBoxRegions.TabIndex = 30;
this.checkedListBoxRegions.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.checkedListBoxRegions_ItemCheck);
this.Controls.Add(this.checkedListBoxRegions);
```

**With NEW:**
```csharp
// dataGridViewRegions
this.dataGridViewRegions = new System.Windows.Forms.DataGridView();
((System.ComponentModel.ISupportInitialize)(this.dataGridViewRegions)).BeginInit();
this.dataGridViewRegions.AllowUserToAddRows = false;
this.dataGridViewRegions.AllowUserToDeleteRows = false;
this.dataGridViewRegions.AllowUserToResizeRows = false;
this.dataGridViewRegions.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
this.dataGridViewRegions.Location = new System.Drawing.Point(16, 505);
this.dataGridViewRegions.Name = "dataGridViewRegions";
this.dataGridViewRegions.RowHeadersVisible = false;
this.dataGridViewRegions.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
this.dataGridViewRegions.Size = new System.Drawing.Size(500, 150);
this.dataGridViewRegions.TabIndex = 30;
((System.ComponentModel.ISupportInitialize)(this.dataGridViewRegions)).EndInit();
this.Controls.Add(this.dataGridViewRegions);
```

### Step 2: Update CoreCommandMIPUserControl.cs Code

**In `CoreCommandMIPUserControl.cs`:**

1. **Add DataGridView setup in constructor or Load event:**
```csharp
private void InitializeRegionDataGrid()
{
    dataGridViewRegions.AutoGenerateColumns = false;
    dataGridViewRegions.Columns.Clear();

    // Selection checkbox column
    var selectColumn = new DataGridViewCheckBoxColumn
    {
        Name = "SelectColumn",
        HeaderText = "Select",
        DataPropertyName = "IsSelected",
        Width = 60,
        ReadOnly = false
    };
    dataGridViewRegions.Columns.Add(selectColumn);

    // Name column
    var nameColumn = new DataGridViewTextBoxColumn
    {
        Name = "NameColumn",
        HeaderText = "Name",
        DataPropertyName = "Name",
        Width = 200,
        ReadOnly = true
    };
    dataGridViewRegions.Columns.Add(nameColumn);

    // Type column
    var typeColumn = new DataGridViewTextBoxColumn
    {
        Name = "TypeColumn",
        HeaderText = "Type",
        DataPropertyName = "TypeDisplay",
        Width = 100,
        ReadOnly = true
    };
    dataGridViewRegions.Columns.Add(typeColumn);

    // Active column (optional)
    var activeColumn = new DataGridViewCheckBoxColumn
    {
        Name = "ActiveColumn",
        HeaderText = "Active",
        DataPropertyName = "Active",
        Width = 60,
        ReadOnly = true
    };
    dataGridViewRegions.Columns.Add(activeColumn);
}
```

2. **Call InitializeRegionDataGrid in the constructor:**
```csharp
public CoreCommandMIPUserControl()
{
    InitializeComponent();
    InitializeRegionDataGrid();
}
```

3. **Replace `LoadRegionsAsync` method:**
```csharp
private async void LoadRegionsAsync()
{
    if (_remoteSettings == null)
    {
        MessageBox.Show("Settings not loaded yet.", "Region Load", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return;
    }
    
    if (!_remoteSettings.IsConfigured())
    {
        MessageBox.Show("Server not configured. Please enter server address, username, and password first.", "Region Load", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return;
    }
    
    try
    {
        buttonRefreshRegions.Enabled = false;
        buttonRefreshRegions.Text = "Loading...";
        
        var provider = new Client.RemoteServerDataProvider();
        var baseUrl = _remoteSettings.GetBaseUrl();
        
        var regionList = await provider.FetchRegionListAsync(baseUrl, _remoteSettings.DefaultUsername, _remoteSettings.DefaultPassword, CancellationToken.None);
        
        if (regionList == null || regionList.Count == 0)
        {
            MessageBox.Show("No regions found on server.", "Region Load", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        
        // Load previously selected region IDs
        var selectedRegionIds = ParseSelectedRegionIds(_remoteSettings.SelectedRegionIds);
        
        // Set IsSelected based on previously saved selections
        foreach (var region in regionList)
        {
            region.IsSelected = selectedRegionIds.Contains(region.Id) || 
                               (!string.IsNullOrEmpty(region.GuidId) && selectedRegionIds.Contains((long)region.GuidId.GetHashCode()));
        }
        
        // Bind to DataGridView
        var bindingList = new BindingList<Client.RegionListItem>(regionList);
        dataGridViewRegions.DataSource = bindingList;
        
        MessageBox.Show($"Loaded {regionList.Count} regions successfully.", "Region Load", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Failed to load regions: {ex.Message}", "Region Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
    finally
    {
        buttonRefreshRegions.Enabled = true;
        buttonRefreshRegions.Text = "Refresh Regions";
    }
}
```

4. **Replace `GetSelectedRegionIds` method:**
```csharp
private string GetSelectedRegionIds()
{
    if (dataGridViewRegions.DataSource == null)
        return string.Empty;
    
    var selectedIds = new List<string>();
    var bindingList = dataGridViewRegions.DataSource as BindingList<Client.RegionListItem>;
    
    if (bindingList != null)
    {
        foreach (var region in bindingList)
        {
            if (region.IsSelected)
            {
                selectedIds.Add(region.Id.ToString());
            }
        }
    }
    
    return string.Join(",", selectedIds);
}
```

5. **Remove the old event handler:**
```csharp
// DELETE THIS METHOD:
// private void checkedListBoxRegions_ItemCheck(object sender, ItemCheckEventArgs e)
```

6. **Add BindingList using directive at top of file:**
```csharp
using System.ComponentModel;
```

## Fix for Region Redrawing Issue

The issue where regions don't redraw when switching between maps is because `_siteRegions` is cleared but not reloaded.

**In `Client\CoreCommandMIPViewItemWpfUserControl.xaml.cs`:**

Find the method where configuration changes (around line 900-950) and ensure regions are reloaded:

```csharp
private async void OnConfigurationChanged(Guid newConfigurationId)
{
    // ... existing code to clear tracks and regions ...
    
    _siteRegions.Clear();
    
    // ADD THIS: Reload regions after clearing
    await Task.Delay(500); // Give time for map to clear
    await LoadAndRenderRegionsAsync().ConfigureAwait(false);
}
```

Or ensure that when the map view becomes active again, it triggers region reload:

```csharp
public override void Init()
{
    // ... existing Init code ...
    
    // Ensure regions are loaded when view initializes
    Task.Run(async () => 
    {
        await Task.Delay(1000); // Wait for map to be ready
        await LoadAndRenderRegionsAsync().ConfigureAwait(false);
    });
}
```

## Testing Checklist

- [ ] DataGridView displays with 4 columns: Select, Name, Type, Active
- [ ] Checkboxes in Select column can be toggled
- [ ] Name shows proper region names (not GUID truncations)
- [ ] Type shows "Alarm" or "Exclusion" correctly
- [ ] Selected regions persist when saving/loading configuration
- [ ] Regions render on map after loading
- [ ] Regions redraw when switching between maps/sites
- [ ] Regions persist when switching tabs and coming back to the map view

## Additional Notes

- The DataGridView uses data binding with `BindingList<T>` for automatic updates
- Region selection state is stored in the `IsSelected` property
- The `TypeDisplay` property provides a user-friendly display of region type
- Make sure to handle the case where regions might be GUID-based vs numeric ID-based
