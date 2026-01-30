using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using VideoOS.Platform;
using VideoOS.Platform.ConfigurationItems;

namespace CoreCommandMIP.Admin
{
    public static class C2AlarmWiringVerified
    {
        public sealed class WiringResult
        {
            public UserDefinedEvent UserDefinedEvent { get; set; }
            public AlarmDefinition AlarmDefinition { get; set; }
        }

        /// <summary>
        /// Create/ensure UDE + Alarm Definition, then VERIFY that both are query-visible
        /// (by polling reloaded configuration state) and return the objects found.
        /// </summary>
        public static WiringResult EnsureUdeAndAlarmDefinitionVerified(
            string udeName,
            string alarmDefinitionName,
            string alarmPriority,                 // "High" | "Medium" | "Low"
            string[] relatedCameraPaths = null,   // optional; comma-joined into related camera list
            TimeSpan? verifyTimeout = null,
            TimeSpan? initialPollDelay = null
        )
        {
            if (string.IsNullOrWhiteSpace(udeName)) throw new ArgumentNullException(nameof(udeName));
            if (string.IsNullOrWhiteSpace(alarmDefinitionName)) throw new ArgumentNullException(nameof(alarmDefinitionName));
            if (string.IsNullOrWhiteSpace(alarmPriority)) throw new ArgumentNullException(nameof(alarmPriority));

            var timeout = verifyTimeout ?? TimeSpan.FromSeconds(30);
            var startDelay = initialPollDelay ?? TimeSpan.FromMilliseconds(150);

            var udeFolder = new UserDefinedEventFolder();
            var alarmFolder = new AlarmDefinitionFolder();

            // 1) Ensure UDE exists (create if missing)
            EnsureUdeExists(udeFolder, udeName);

            // 2) Verify: poll until code can see the UDE object
            var ude = WaitForUdeVisible(udeFolder, udeName, timeout, startDelay);

            // 3) Ensure Alarm Definition exists (create if missing)
            EnsureAlarmDefinitionExists(alarmFolder, ude, alarmDefinitionName, alarmPriority, relatedCameraPaths);

            // 4) Verify: poll until code can see Alarm Definition object
            var alarm = WaitForAlarmVisible(alarmFolder, alarmDefinitionName, timeout, startDelay);

            return new WiringResult
            {
                UserDefinedEvent = ude,
                AlarmDefinition = alarm
            };
        }

        private static void EnsureUdeExists(UserDefinedEventFolder udeFolder, string udeName)
        {
            DiagnosticLogger.WriteLine($"EnsureUdeExists: '{udeName}'");
            
            var serverId = VideoOS.Platform.EnvironmentManager.Instance.MasterSite.ServerId;
            var ms = new VideoOS.Platform.ConfigurationItems.ManagementServer(serverId);
            
            // Use ManagementServer approach (WORKING!)
            try
            {
                DiagnosticLogger.WriteLine($"Checking via ManagementServer.UserDefinedEventFolder...");
                var events = ms.UserDefinedEventFolder.UserDefinedEvents.ToArray();
                DiagnosticLogger.WriteLine($"  Total events: {events.Length}");
                
                var existing = events.FirstOrDefault(e => string.Equals(e.Name, udeName, StringComparison.OrdinalIgnoreCase));
                
                if (existing != null)
                {
                    DiagnosticLogger.WriteLine($"  ✓ UDE already exists, skipping creation");
                    return;
                }
                
                DiagnosticLogger.WriteLine($"  UDE not found, creating...");
            }
            catch (Exception ex)
            {
                DiagnosticLogger.WriteLine($"  Error checking existing: {ex.Message}");
            }

            DiagnosticLogger.WriteLine($"  Creating UDE via AddUserDefinedEvent...");
            var task = udeFolder.AddUserDefinedEvent(udeName);
            WaitForTaskOrThrow(task, $"Failed creating UDE '{udeName}'");
            DiagnosticLogger.WriteLine($"  UDE creation task completed with state: {task.State}");
        }

        private static UserDefinedEvent WaitForUdeVisible(
            UserDefinedEventFolder udeFolder,
            string udeName,
            TimeSpan timeout,
            TimeSpan initialDelay)
        {
            DiagnosticLogger.WriteLine($"Waiting for UDE to be visible: '{udeName}'");
            
            var sw = Stopwatch.StartNew();
            var delay = initialDelay;
            int attemptNumber = 0;
            var serverId = VideoOS.Platform.EnvironmentManager.Instance.MasterSite.ServerId;
            var ms = new VideoOS.Platform.ConfigurationItems.ManagementServer(serverId);

            while (sw.Elapsed < timeout)
            {
                attemptNumber++;
                DiagnosticLogger.WriteLine($"Query attempt #{attemptNumber} (elapsed: {sw.Elapsed.TotalSeconds:0.##}s)");
                
                try
                {
                    // Use ManagementServer approach (WORKING!)
                    var events = ms.UserDefinedEventFolder.UserDefinedEvents.ToArray();
                    DiagnosticLogger.WriteLine($"  Found {events.Length} total events");
                    
                    var found = events.FirstOrDefault(e => string.Equals(e.Name, udeName, StringComparison.OrdinalIgnoreCase));
                    
                    if (found != null)
                    {
                        DiagnosticLogger.WriteLine($"✓ UDE '{udeName}' FOUND after {attemptNumber} attempts ({sw.Elapsed.TotalSeconds:0.##}s)");
                        DiagnosticLogger.WriteLine($"  Path: {found.Path}");
                        return found;
                    }
                    
                    DiagnosticLogger.WriteLine($"  Not found yet, sleeping {delay.TotalMilliseconds}ms...");
                }
                catch (Exception ex)
                {
                    DiagnosticLogger.WriteLine($"  Error: {ex.GetType().Name}: {ex.Message}");
                }

                Thread.Sleep(delay);
                delay = Backoff(delay);
            }

            DiagnosticLogger.WriteLine($"✗ TIMEOUT: UDE '{udeName}' not found after {attemptNumber} attempts ({sw.Elapsed.TotalSeconds:0.##}s)");
            throw new TimeoutException($"UDE '{udeName}' not found after {timeout.TotalSeconds:0.##}s.");
        }

        private static void EnsureAlarmDefinitionExists(
            AlarmDefinitionFolder alarmFolder,
            UserDefinedEvent ude,
            string alarmDefinitionName,
            string alarmPriority,
            string[] relatedCameraPaths)
        {
            DiagnosticLogger.WriteLine($"EnsureAlarmDefinitionExists: '{alarmDefinitionName}'");
            
            var serverId = VideoOS.Platform.EnvironmentManager.Instance.MasterSite.ServerId;
            var ms = new VideoOS.Platform.ConfigurationItems.ManagementServer(serverId);
            
            // Query alarms from array (same pattern as events)
            try
            {
                DiagnosticLogger.WriteLine($"Checking via ManagementServer.AlarmDefinitionFolder...");
                var alarms = ms.AlarmDefinitionFolder.AlarmDefinitions.ToArray();
                DiagnosticLogger.WriteLine($"  Total alarms: {alarms.Length}");
                
                var existing = alarms.FirstOrDefault(a => string.Equals(a.Name, alarmDefinitionName, StringComparison.OrdinalIgnoreCase));
                
                if (existing != null)
                {
                    DiagnosticLogger.WriteLine($"  ✓ Alarm already exists, skipping creation");
                    return;
                }
                
                DiagnosticLogger.WriteLine($"  Alarm not found, creating...");
            }
            catch (Exception ex)
            {
                DiagnosticLogger.WriteLine($"  Error checking existing: {ex.Message}");
            }

            string udePath = ude.Path;
            DiagnosticLogger.WriteLine($"  Wiring to UDE path: {udePath}");
            
            // Get an existing alarm definition to probe valid values
            // We MUST use an existing alarm's path to get populated EventTypeGroupValues
            DiagnosticLogger.WriteLine($"  Probing for valid EventTypeGroup and EventType values...");
            
            AlarmDefinition probe = null;
            try
            {
                DiagnosticLogger.WriteLine($"  Trying ManagementServer.AlarmDefinitionFolder...");
                var alarms = ms.AlarmDefinitionFolder.AlarmDefinitions.ToArray();
                if (alarms.Length > 0)
                {
                    DiagnosticLogger.WriteLine($"  Found {alarms.Length} alarms, using first: {alarms[0].Name}");
                    DiagnosticLogger.WriteLine($"  Alarm path: {alarms[0].Path}");
                    
                    // Create new AlarmDefinition using the existing alarm's path to get valid values
                    probe = new AlarmDefinition(serverId, alarms[0].Path);
                    DiagnosticLogger.WriteLine($"  Created probe from existing alarm path");
                    DiagnosticLogger.WriteLine($"  EventTypeGroupValues count: {probe.EventTypeGroupValues?.Count ?? 0}");
                }
                else
                {
                    DiagnosticLogger.WriteLine($"  No existing alarms found");
                }
            }
            catch (Exception ex)
            {
                DiagnosticLogger.WriteLine($"  Error probing existing alarms: {ex.Message}");
                DiagnosticLogger.WriteLine($"  Stack: {ex.StackTrace}");
            }
            
            // If no probe, we cannot proceed
            if (probe == null || probe.EventTypeGroupValues == null || probe.EventTypeGroupValues.Count == 0)
            {
                DiagnosticLogger.WriteLine($"  ERROR: No existing alarm definitions found to probe values!");
                DiagnosticLogger.WriteLine($"  Cannot determine valid EventTypeGroup and EventType values.");
                DiagnosticLogger.WriteLine($"");
                DiagnosticLogger.WriteLine($"  SOLUTION:");
                DiagnosticLogger.WriteLine($"  1. Open Management Client");
                DiagnosticLogger.WriteLine($"  2. Go to Rules and Events > Alarms");
                DiagnosticLogger.WriteLine($"  3. Create ANY alarm definition manually (any type)");
                DiagnosticLogger.WriteLine($"  4. Save it");
                DiagnosticLogger.WriteLine($"  5. Then try creating C2 alarms again");
                throw new InvalidOperationException(
                    "Cannot create alarm: No existing alarm definitions found to probe valid values. " +
                    "Please create at least one alarm definition manually in Management Client first " +
                    "(any type), then this plugin can probe the valid EventTypeGroup and EventType options.");
            }
            
            DiagnosticLogger.WriteLine($"  ✓ Successfully created probe with {probe.EventTypeGroupValues.Count} EventTypeGroup options");
            
            // Get EventTypeGroup value (Key = Display Name, Value = Internal GUID)
            DiagnosticLogger.WriteLine($"  Looking up EventTypeGroup...");
            DiagnosticLogger.WriteLine($"  Available EventTypeGroupValues:");
            foreach (var kvp in probe.EventTypeGroupValues)
            {
                DiagnosticLogger.WriteLine($"    '{kvp.Key}' = '{kvp.Value}'");
            }
            
            string eventTypeGroup = null;
            if (probe.EventTypeGroupValues.ContainsKey("External Events"))
            {
                eventTypeGroup = probe.EventTypeGroupValues["External Events"];
                DiagnosticLogger.WriteLine($"  ✓ Found 'External Events' -> '{eventTypeGroup}'");
            }
            else if (probe.EventTypeGroupValues.ContainsKey("User-defined Events"))
            {
                eventTypeGroup = probe.EventTypeGroupValues["User-defined Events"];
                DiagnosticLogger.WriteLine($"  ✓ Found 'User-defined Events' -> '{eventTypeGroup}'");
            }
            else if (probe.EventTypeGroupValues.ContainsKey("User-Defined Events"))
            {
                eventTypeGroup = probe.EventTypeGroupValues["User-Defined Events"];
                DiagnosticLogger.WriteLine($"  ✓ Found 'User-Defined Events' -> '{eventTypeGroup}'");
            }
            
            if (string.IsNullOrEmpty(eventTypeGroup))
            {
                DiagnosticLogger.WriteLine($"  ERROR: Could not find valid EventTypeGroup!");
                throw new InvalidOperationException($"Could not find valid EventTypeGroup. Check diagnostic log for available options.");
            }
            
            // Set EventTypeGroup and validate to populate EventTypeValues
            probe.EventTypeGroup = eventTypeGroup;
            probe.ValidateItem();
            
            // Dump EventTypeValues to see what's available
            DiagnosticLogger.WriteLine($"  Looking up EventType...");
            DiagnosticLogger.WriteLine($"  Available EventTypeValues:");
            foreach (var kvp in probe.EventTypeValues)
            {
                DiagnosticLogger.WriteLine($"    '{kvp.Key}' = '{kvp.Value}'");
            }
            
            string eventType = null;
            if (probe.EventTypeValues.ContainsKey("External Event"))
            {
                eventType = probe.EventTypeValues["External Event"];
                DiagnosticLogger.WriteLine($"  ✓ Found 'External Event' -> '{eventType}'");
            }
            else if (probe.EventTypeValues.ContainsKey("User-defined Event"))
            {
                eventType = probe.EventTypeValues["User-defined Event"];
                DiagnosticLogger.WriteLine($"  ✓ Found 'User-defined Event' -> '{eventType}'");
            }
            else if (probe.EventTypeValues.ContainsKey("User-Defined Event"))
            {
                eventType = probe.EventTypeValues["User-Defined Event"];
                DiagnosticLogger.WriteLine($"  ✓ Found 'User-Defined Event' -> '{eventType}'");
            }
            
            
            if (string.IsNullOrEmpty(eventType))
            {
                DiagnosticLogger.WriteLine($"  ERROR: Could not find valid EventType!");
                throw new InvalidOperationException($"Could not find valid EventType. Check diagnostic log for available options.");
            }

            // Probe for EnableRule values
            DiagnosticLogger.WriteLine($"  Looking up EnableRule...");
            DiagnosticLogger.WriteLine($"  Available EnableRuleValues:");
            foreach (var kvp in probe.EnableRuleValues)
            {
                DiagnosticLogger.WriteLine($"    '{kvp.Key}' = '{kvp.Value}'");
            }
            
            string enableRule = null;
            if (probe.EnableRuleValues.ContainsKey("Always"))
            {
                enableRule = probe.EnableRuleValues["Always"];
                DiagnosticLogger.WriteLine($"  ✓ Found 'Always' -> '{enableRule}'");
            }
            else if (probe.EnableRuleValues.Count > 0)
            {
                // Fallback to first available option
                var firstRule = probe.EnableRuleValues.First();
                enableRule = firstRule.Value;
                DiagnosticLogger.WriteLine($"  ⚠ Warning: 'Always' not found, using first available: '{firstRule.Key}' -> '{enableRule}'");
            }
            
            if (string.IsNullOrEmpty(enableRule))
            {
                DiagnosticLogger.WriteLine($"  ERROR: Could not find valid EnableRule!");
                throw new InvalidOperationException($"Could not find valid EnableRule. Check diagnostic log for available options.");
            }

            // Probe for Priority values (validate the priority parameter)
            DiagnosticLogger.WriteLine($"  Looking up Priority...");
            DiagnosticLogger.WriteLine($"  Available PriorityValues:");
            foreach (var kvp in probe.PriorityValues)
            {
                DiagnosticLogger.WriteLine($"    '{kvp.Key}' = '{kvp.Value}'");
            }
            
            string priorityValue = null;
            if (probe.PriorityValues.ContainsKey(alarmPriority))
            {
                priorityValue = probe.PriorityValues[alarmPriority];
                DiagnosticLogger.WriteLine($"  ✓ Found '{alarmPriority}' -> '{priorityValue}'");
            }
            else
            {
                DiagnosticLogger.WriteLine($"  ERROR: Priority '{alarmPriority}' not valid!");
                DiagnosticLogger.WriteLine($"  Valid priorities are: {string.Join(", ", probe.PriorityValues.Keys)}");
                throw new InvalidOperationException($"Priority '{alarmPriority}' is not valid. Valid options: {string.Join(", ", probe.PriorityValues.Keys)}");
            }

            // Probe for Category values
            DiagnosticLogger.WriteLine($"  Looking up Category...");
            DiagnosticLogger.WriteLine($"  Available CategoryValues:");
            foreach (var kvp in probe.CategoryValues)
            {
                DiagnosticLogger.WriteLine($"    '{kvp.Key}' = '{kvp.Value}'");
            }
            
            string categoryValue = null;
            if (probe.CategoryValues != null && probe.CategoryValues.Count > 0)
            {
                // If categories exist, we MUST use one of them
                if (probe.CategoryValues.ContainsKey("C2 Alarms"))
                {
                    categoryValue = probe.CategoryValues["C2 Alarms"];
                    DiagnosticLogger.WriteLine($"  ✓ Found 'C2 Alarms' -> '{categoryValue}'");
                }
                else
                {
                    // Use first available category
                    var firstCategory = probe.CategoryValues.First();
                    categoryValue = firstCategory.Value;
                    DiagnosticLogger.WriteLine($"  ⚠ Warning: 'C2 Alarms' not found, using first available: '{firstCategory.Key}' -> '{categoryValue}'");
                }
            }
            else
            {
                // No categories defined on this system
                DiagnosticLogger.WriteLine($"  ⚠ No categories defined on this system");
                DiagnosticLogger.WriteLine($"  ERROR: Cannot create alarm without categories!");
                DiagnosticLogger.WriteLine($"");
                DiagnosticLogger.WriteLine($"  SOLUTION:");
                DiagnosticLogger.WriteLine($"  1. Open Management Client");
                DiagnosticLogger.WriteLine($"  2. Go to Alarms");
                DiagnosticLogger.WriteLine($"  3. Click on Alarm Data Settings");
                DiagnosticLogger.WriteLine($"  4. Create a new category (e.g., 'C2 Alarms')");
                DiagnosticLogger.WriteLine($"  5. Click OK to save");
                DiagnosticLogger.WriteLine($"  6. Then try creating C2 alarms again");
                throw new InvalidOperationException(
                    "Cannot create alarm: No alarm categories defined in system. " +
                    "Please create at least one alarm category in Management Client first " +
                    "Alarms > Alarm Data Settings > Categories), then try again.");
            }

            var relatedCameraList = (relatedCameraPaths != null && relatedCameraPaths.Length > 0)
                ? string.Join(",", relatedCameraPaths)
                : string.Empty;

            DiagnosticLogger.WriteLine($"  Creating alarm via AddAlarmDefinition...");
            DiagnosticLogger.WriteLine($"    Name: {alarmDefinitionName}");
            DiagnosticLogger.WriteLine($"    EventTypeGroup: {eventTypeGroup}");
            DiagnosticLogger.WriteLine($"    EventType: {eventType}");
            DiagnosticLogger.WriteLine($"    SourceList: {udePath}");
            DiagnosticLogger.WriteLine($"    EnableRule: {enableRule}");
            DiagnosticLogger.WriteLine($"    Priority: {priorityValue}");
            DiagnosticLogger.WriteLine($"    Category: {categoryValue}");
            DiagnosticLogger.WriteLine($"    Cameras: {relatedCameraPaths?.Length ?? 0}");
            
            var addAlarmTask = ms.AlarmDefinitionFolder.AddAlarmDefinition(
                name: alarmDefinitionName,
                description: $"Auto-created. Triggered by UDE '{ude.Name}'.",
                eventTypeGroup: eventTypeGroup,  // Internal GUID from dictionary
                eventType: eventType,            // Internal token from dictionary
                sourceList: udePath,
                enableRule: enableRule,          // Probed value
                timeProfile: string.Empty,
                enableEventList: string.Empty,
                disableEventList: string.Empty,
                managementTimeoutTime: string.Empty,
                managementTimeoutEventList: string.Empty,
                relatedCameraList: relatedCameraList,
                mapType: string.Empty,
                relatedMap: string.Empty,
                owner: string.Empty,
                priority: priorityValue,         // Probed value
                category: categoryValue,         // Probed value
                triggerEventlist: string.Empty
            );

            WaitForTaskOrThrow(addAlarmTask, $"Failed creating Alarm Definition '{alarmDefinitionName}'");
            DiagnosticLogger.WriteLine($"  ✓ Alarm creation task completed with state: {addAlarmTask.State}");
        }
        
        /// <summary>
        /// Helper to find the internal KEY for a display VALUE in Milestone value dictionaries.
        /// Example: FindKeyByDisplayValue(values, "External Events") returns "ExternalEvents"
        /// Dictionary structure: Key = internal enum, Value = display name
        /// </summary>
        private static string FindKeyByDisplayValue(System.Collections.Generic.Dictionary<string, string> values, string displayValue)
        {
            if (values == null || string.IsNullOrWhiteSpace(displayValue))
            {
                DiagnosticLogger.WriteLine($"    FindKeyByDisplayValue: null input");
                return null;
            }
            
            foreach (var kvp in values)
            {
                // Compare the VALUE (display name) to find the KEY (internal enum)
                if (string.Equals(kvp.Value, displayValue, StringComparison.OrdinalIgnoreCase))
                {
                    DiagnosticLogger.WriteLine($"    Found: Display '{displayValue}' -> Key '{kvp.Key}'");
                    return kvp.Key;  // Return the KEY (internal value)
                }
            }
            
            DiagnosticLogger.WriteLine($"    Not found: '{displayValue}'");
            return null;
        }

        /// <summary>
        /// Diagnostic helper to dump all available values from an AlarmDefinition probe.
        /// Useful for troubleshooting "out of range" errors.
        /// </summary>
        public static void DumpAllAlarmDefinitionValues(AlarmDefinition probe)
        {
            if (probe == null)
            {
                DiagnosticLogger.WriteLine("DumpAllAlarmDefinitionValues: probe is null");
                return;
            }

            DiagnosticLogger.WriteSection("ALARM DEFINITION VALUE PROBE");
            
            DiagnosticLogger.WriteLine("EventTypeGroupValues:");
            foreach (var kvp in probe.EventTypeGroupValues)
            {
                DiagnosticLogger.WriteLine($"  '{kvp.Key}' = '{kvp.Value}'");
            }
            
            DiagnosticLogger.WriteLine("EnableRuleValues:");
            foreach (var kvp in probe.EnableRuleValues)
            {
                DiagnosticLogger.WriteLine($"  '{kvp.Key}' = '{kvp.Value}'");
            }
            
            DiagnosticLogger.WriteLine("PriorityValues:");
            foreach (var kvp in probe.PriorityValues)
            {
                DiagnosticLogger.WriteLine($"  '{kvp.Key}' = '{kvp.Value}'");
            }
            
            DiagnosticLogger.WriteLine("CategoryValues:");
            foreach (var kvp in probe.CategoryValues)
            {
                DiagnosticLogger.WriteLine($"  '{kvp.Key}' = '{kvp.Value}'");
            }
            
            // Note: EventTypeValues only populated after setting EventTypeGroup
            if (probe.EventTypeValues != null && probe.EventTypeValues.Count > 0)
            {
                DiagnosticLogger.WriteLine("EventTypeValues:");
                foreach (var kvp in probe.EventTypeValues)
                {
                    DiagnosticLogger.WriteLine($"  '{kvp.Key}' = '{kvp.Value}'");
                }
            }
            else
            {
                DiagnosticLogger.WriteLine("EventTypeValues: (not populated - set EventTypeGroup first)");
            }
        }

        private static AlarmDefinition WaitForAlarmVisible(
            AlarmDefinitionFolder alarmFolder,
            string alarmDefinitionName,
            TimeSpan timeout,
            TimeSpan initialDelay)
        {
            DiagnosticLogger.WriteLine($"Waiting for Alarm to be visible: '{alarmDefinitionName}'");
            
            var sw = Stopwatch.StartNew();
            var delay = initialDelay;
            int attemptNumber = 0;
            var serverId = VideoOS.Platform.EnvironmentManager.Instance.MasterSite.ServerId;
            var ms = new VideoOS.Platform.ConfigurationItems.ManagementServer(serverId);

            while (sw.Elapsed < timeout)
            {
                attemptNumber++;
                DiagnosticLogger.WriteLine($"Query attempt #{attemptNumber} (elapsed: {sw.Elapsed.TotalSeconds:0.##}s)");
                
                try
                {
                    // Use ManagementServer approach (WORKING!)
                    var alarms = ms.AlarmDefinitionFolder.AlarmDefinitions.ToArray();
                    DiagnosticLogger.WriteLine($"  Found {alarms.Length} total alarms");
                    
                    var found = alarms.FirstOrDefault(a => string.Equals(a.Name, alarmDefinitionName, StringComparison.OrdinalIgnoreCase));
                    
                    if (found != null)
                    {
                        DiagnosticLogger.WriteLine($"✓ Alarm '{alarmDefinitionName}' FOUND after {attemptNumber} attempts ({sw.Elapsed.TotalSeconds:0.##}s)");
                        DiagnosticLogger.WriteLine($"  Path: {found.Path}");
                        DiagnosticLogger.WriteLine($"  Priority: {found.Priority}");
                        return found;
                    }
                    
                    DiagnosticLogger.WriteLine($"  Not found yet, sleeping {delay.TotalMilliseconds}ms...");
                }
                catch (Exception ex)
                {
                    DiagnosticLogger.WriteLine($"  Error: {ex.GetType().Name}: {ex.Message}");
                }

                Thread.Sleep(delay);
                delay = Backoff(delay);
            }

            DiagnosticLogger.WriteLine($"✗ TIMEOUT: Alarm '{alarmDefinitionName}' not found after {attemptNumber} attempts ({sw.Elapsed.TotalSeconds:0.##}s)");
            throw new TimeoutException($"Alarm Definition '{alarmDefinitionName}' not found after {timeout.TotalSeconds:0.##}s.");
        }

        private static void ReloadUdeFolder(UserDefinedEventFolder folder)
        {
            // IMPORTANT: Clear cache before FillChildren to avoid stale reads.
            //folder.ClearChildrenCache();
            DiagnosticLogger.WriteLine("UDE cache cleared - !!!ReloadUdeFolder triggered!!!");

            // Fill children for UDEs
            ServerId msServerId = Configuration.Instance.ServerFQID.ServerId;
            var ms = new ManagementServer(msServerId);
            folder = ms.UserDefinedEventFolder;
            folder.ClearChildrenCache();
            folder.FillChildren(new[] { "UserDefinedEvent" });

            //DiagnosticLogger.WriteLine($"UDEName used for FillChildren {nameof(UserDefinedEvent)}");
          
            
            var count = folder.UserDefinedEvents?.Count ?? 0;

            DiagnosticLogger.WriteLine($"FillChildren(UDE) returned {count} UserDefinedEvent items");
            
            // Try to get raw count from base wrapper (if accessible)
            try
            {
                var allItems = folder.GetType()
                    .BaseType
                    .GetProperty("Wrapper", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(folder);
                
                if (allItems != null)
                {
                    var childrenMethod = allItems.GetType().GetMethod("Children");
                    if (childrenMethod != null)
                    {
                        var rawChildren = childrenMethod.Invoke(allItems, null) as Array;
                        if (rawChildren != null)
                        {
                            DiagnosticLogger.WriteLine($"  Raw server response: {rawChildren.Length} ConfigurationItem(s) total");
                            
                            // Log all item types
                            var itemTypes = new System.Collections.Generic.Dictionary<string, int>();
                            foreach (var item in rawChildren)
                            {
                                var itemType = item.GetType().GetProperty("ItemType")?.GetValue(item) as string ?? "Unknown";
                                if (!itemTypes.ContainsKey(itemType))
                                    itemTypes[itemType] = 0;
                                itemTypes[itemType]++;
                            }
                            
                            DiagnosticLogger.WriteLine("  Item types in response:");
                            foreach (var kvp in itemTypes)
                            {
                                DiagnosticLogger.WriteLine($"    - {kvp.Key}: {kvp.Value}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DiagnosticLogger.WriteLine($"  Could not inspect raw response: {ex.Message}");
            }
            
            if (count > 0)
            {
                DiagnosticLogger.WriteLine("UDEs found after filtering:");
                foreach (var ude in folder.UserDefinedEvents)
                {
                    DiagnosticLogger.WriteLine($"  - '{ude.Name}'");
                }
            }
        }

        private static void ReloadAlarmFolder(AlarmDefinitionFolder folder)
        {
            folder.ClearChildrenCache();
            DiagnosticLogger.WriteLine("Alarm cache cleared");
            
            folder.FillChildren(new[] { nameof(AlarmDefinition) });
            
            var count = folder.AlarmDefinitions?.Count ?? 0;
            DiagnosticLogger.WriteLine($"FillChildren(Alarm) returned {count} AlarmDefinition items");
            
            // Try to get raw count from base wrapper
            try
            {
                var allItems = folder.GetType()
                    .BaseType
                    .GetProperty("Wrapper", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(folder);
                
                if (allItems != null)
                {
                    var childrenMethod = allItems.GetType().GetMethod("Children");
                    if (childrenMethod != null)
                    {
                        var rawChildren = childrenMethod.Invoke(allItems, null) as Array;
                        if (rawChildren != null)
                        {
                            DiagnosticLogger.WriteLine($"  Raw server response: {rawChildren.Length} ConfigurationItem(s) total");
                            
                            var itemTypes = new System.Collections.Generic.Dictionary<string, int>();
                            foreach (var item in rawChildren)
                            {
                                var itemType = item.GetType().GetProperty("ItemType")?.GetValue(item) as string ?? "Unknown";
                                if (!itemTypes.ContainsKey(itemType))
                                    itemTypes[itemType] = 0;
                                itemTypes[itemType]++;
                            }
                            
                            DiagnosticLogger.WriteLine("  Item types in response:");
                            foreach (var kvp in itemTypes)
                            {
                                DiagnosticLogger.WriteLine($"    - {kvp.Key}: {kvp.Value}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DiagnosticLogger.WriteLine($"  Could not inspect raw response: {ex.Message}");
            }
            
            if (count > 0)
            {
                DiagnosticLogger.WriteLine("Alarm Definitions found after filtering:");
                foreach (var alarm in folder.AlarmDefinitions)
                {
                    DiagnosticLogger.WriteLine($"  - '{alarm.Name}' (Priority: {alarm.Priority})");
                }
            }
        }

        private static TimeSpan Backoff(TimeSpan current)
        {
            // Cap backoff to keep UI responsive and avoid long sleeps
            var nextMs = Math.Min(current.TotalMilliseconds * 1.6, 1500);
            return TimeSpan.FromMilliseconds(nextMs);
        }

        private static void WaitForTaskOrThrow(ServerTask task, string errorPrefix)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));

            // Wait for task completion (Milestone uses InProgress and Idle)
            while (task.State == StateEnum.InProgress || task.State == StateEnum.Idle)
                Thread.Sleep(50);

            if (task.State != StateEnum.Success && task.State != StateEnum.Completed)
            {
                var msg = string.IsNullOrWhiteSpace(task.ErrorText) ? task.State.ToString() : task.ErrorText;
                throw new InvalidOperationException($"{errorPrefix}. Task state={task.State}. Error={msg}");
            }
        }
    }
}
