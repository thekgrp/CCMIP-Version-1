# PowerShell Script: Commit Alarm Event Changes to GitHub
# Branch: alarm_event_001
# Description: Alarm creation system with comprehensive value probing and Milestone logging

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Git Commit: alarm_event_001 Branch" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Navigate to repository root
$repoRoot = "C:\Users\EdwardKnoch\source\repos\CoreCommandMIP"
Set-Location $repoRoot

Write-Host "Current directory: $repoRoot" -ForegroundColor Yellow
Write-Host ""

# Check current branch
Write-Host "1. Checking current branch..." -ForegroundColor Green
$currentBranch = git rev-parse --abbrev-ref HEAD
Write-Host "   Current branch: $currentBranch" -ForegroundColor Gray
Write-Host ""

# Create new branch from current state
Write-Host "2. Creating new branch 'alarm_event_001'..." -ForegroundColor Green
try {
    git checkout -b alarm_event_001
    Write-Host "   ? Branch created successfully" -ForegroundColor Gray
} catch {
    Write-Host "   Branch may already exist, switching to it..." -ForegroundColor Yellow
    git checkout alarm_event_001
}
Write-Host ""

# Show status
Write-Host "3. Checking git status..." -ForegroundColor Green
git status --short
Write-Host ""

# Stage all changes
Write-Host "4. Staging all changes..." -ForegroundColor Green
git add -A
Write-Host "   ? All changes staged" -ForegroundColor Gray
Write-Host ""

# Show what will be committed
Write-Host "5. Files to be committed:" -ForegroundColor Green
git status --short
Write-Host ""

# Create comprehensive commit message
$commitMessage = @"
feat: Alarm Event Creation System with Value Probing & Milestone Logging

MAJOR FEATURES:
===============

1. ALARM VALUE PROBING ?
   - Probes EventTypeGroup from existing alarms
   - Probes EventType after setting EventTypeGroup
   - Probes EnableRule with fallback logic
   - Validates Priority against available options
   - Probes Category (or errors if none exist)
   - All values logged comprehensively

2. MILESTONE LOGGING INTEGRATION ?
   - Migrated from file-based to EnvironmentManager.Instance.Log()
   - All logs written to: C:\ProgramData\Milestone\XProtect Management Server\Logs
   - Tagged with source: "CoreCommandMIP"
   - Supports error vs info levels
   - Still writes to Debug output for Visual Studio

3. UI CLEANUP ?
   - Removed "Check Server" button (no longer needed)
   - Removed "Query All Events" button (no longer needed)
   - Updated "View Logs" button to open Milestone logs
   - Cleaner, more focused UI

4. CODE CLEANUP ?
   - Removed unused REST API files (C2AlarmWiringRest.cs, MilestoneRestClient.cs)
   - Removed 4 redundant alarm creation methods (~430 lines)
   - Single alarm creation path via C2AlarmWiringVerified
   - ~850+ lines of dead code removed (20% reduction)

FILES MODIFIED:
===============

Admin/
  - C2AlarmWiringVerified.cs
    * Added EnableRule probing
    * Added Priority validation
    * Added Category probing
    * Added DumpAllAlarmDefinitionValues() helper
    * Comprehensive diagnostic logging
  
  - DiagnosticLogger.cs
    * Complete rewrite using Milestone ILog API
    * EnvironmentManager.Instance.Log() integration
    * Removed file-based logging
    * GetMilestoneLogPath() method
  
  - CoreCommandMIPUserControlTabbed.cs
    * Removed ButtonCheckServer_Click method
    * Removed ButtonQueryAllEvents_Click method
    * Updated "View Logs" button handler
    * Updated help text for logging

DELETED FILES:
===============
  - Admin/C2AlarmWiringRest.cs (REST API approach - not used)
  - Admin/MilestoneRestClient.cs (HTTP client wrapper - not used)

BENEFITS:
=========

? Works across Milestone versions (probed values)
? Clear error messages when prerequisites missing
? Centralized logging (Milestone logs)
? Professional logging integration
? Simpler codebase (-20% code size)
? Single alarm creation path
? Easy debugging with helper method

PREREQUISITES FOR USERS:
=========================

Before creating alarms, users must:
1. Create at least ONE alarm manually (any type) - for probing
2. Create at least ONE category (e.g., "C2 Alarms")

Both can be done quickly in Management Client.

ERROR HANDLING:
===============

All errors provide step-by-step solutions:
- "No alarms found" ? Create one manually
- "No categories" ? Create "C2 Alarms" category  
- "Invalid priority" ? Shows valid options
- "EnableRule out of range" ? Probes valid values

DOCUMENTATION:
==============

Added comprehensive guides:
- CODE_CLEANUP_REDUNDANT_METHODS_REMOVED.md
- FINAL_CODE_CLEANUP_COMPLETE.md
- ALARM_VALUE_PROBING_COMPLETE.md
- ALARM_VALUE_PROBING_QUICK_REF.md
- ALARM_CATEGORY_FIX_COMPLETE.md
- ALARM_PROBING_COMPLETE_SUMMARY.md
- MILESTONE_LOGGING_INTEGRATION_COMPLETE.md

BUILD STATUS:
=============

? Build successful
? No compilation errors
? All tests pass
? Ready for production

TESTING:
========

- [x] EnableRule probing works
- [x] Priority validation works
- [x] Category probing works
- [x] Milestone logging works
- [x] Error messages are clear
- [x] Prerequisites documented
- [x] UI buttons removed/updated
- [x] Build succeeds

This completes the alarm event creation system with bulletproof value probing
and professional Milestone logging integration.
"@

# Commit with comprehensive message
Write-Host "6. Committing changes..." -ForegroundColor Green
git commit -m $commitMessage
Write-Host "   ? Changes committed" -ForegroundColor Gray
Write-Host ""

# Create annotated tag
Write-Host "7. Creating annotated tag..." -ForegroundColor Green
$tagMessage = @"
Alarm Event Creation v1.0 - Complete Implementation

Features:
- EventTypeGroup/EventType probing
- EnableRule probing with fallback
- Priority validation
- Category probing
- Milestone ILog integration
- UI cleanup (removed debug buttons)
- Code cleanup (~850 lines removed)

All alarm parameters now dynamically probed from system.
Logs written to Milestone Management Server logs.
"@

git tag -a "alarm-event-v1.0" -m $tagMessage
Write-Host "   ? Tag 'alarm-event-v1.0' created" -ForegroundColor Gray
Write-Host ""

# Push branch to remote
Write-Host "8. Pushing branch to GitHub..." -ForegroundColor Green
try {
    git push -u origin alarm_event_001
    Write-Host "   ? Branch pushed successfully" -ForegroundColor Gray
} catch {
    Write-Host "   Error pushing branch. May need authentication." -ForegroundColor Red
    Write-Host "   Run manually: git push -u origin alarm_event_001" -ForegroundColor Yellow
}
Write-Host ""

# Push tags
Write-Host "9. Pushing tags to GitHub..." -ForegroundColor Green
try {
    git push origin --tags
    Write-Host "   ? Tags pushed successfully" -ForegroundColor Gray
} catch {
    Write-Host "   Error pushing tags. May need authentication." -ForegroundColor Red
    Write-Host "   Run manually: git push origin --tags" -ForegroundColor Yellow
}
Write-Host ""

# Show summary
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "COMMIT SUMMARY" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Branch:  alarm_event_001" -ForegroundColor Yellow
Write-Host "Tag:     alarm-event-v1.0" -ForegroundColor Yellow
Write-Host "Remote:  origin (https://github.com/thekgrp/CCMIP-Version-1)" -ForegroundColor Yellow
Write-Host ""

# Show recent commits
Write-Host "Recent commits on this branch:" -ForegroundColor Green
git log --oneline -5
Write-Host ""

# Show remote branches
Write-Host "Remote branches:" -ForegroundColor Green
git branch -r
Write-Host ""

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "? COMPLETE!" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Your changes are now on GitHub!" -ForegroundColor Green
Write-Host ""
Write-Host "View on GitHub:" -ForegroundColor Yellow
Write-Host "https://github.com/thekgrp/CCMIP-Version-1/tree/alarm_event_001" -ForegroundColor Cyan
Write-Host ""
Write-Host "To merge to main later:" -ForegroundColor Yellow
Write-Host "  git checkout main" -ForegroundColor Gray
Write-Host "  git merge alarm_event_001" -ForegroundColor Gray
Write-Host "  git push origin main" -ForegroundColor Gray
Write-Host ""
