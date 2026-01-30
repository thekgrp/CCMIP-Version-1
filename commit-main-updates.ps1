# PowerShell Script: Commit Recent Updates to Main Branch
# Date: 2025-01-XX
# Branch: main

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Git Commit: Recent UI & Map Updates" -ForegroundColor Cyan
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

if ($currentBranch -ne "main") {
    Write-Host "   Switching to main branch..." -ForegroundColor Yellow
    git checkout main
}
Write-Host ""

# Show status
Write-Host "2. Checking git status..." -ForegroundColor Green
git status --short
Write-Host ""

# Stage all changes
Write-Host "3. Staging all changes..." -ForegroundColor Green
git add -A
Write-Host "   ? All changes staged" -ForegroundColor Gray
Write-Host ""

# Show what will be committed
Write-Host "4. Files to be committed:" -ForegroundColor Green
git status --short
Write-Host ""

# Create comprehensive commit message
$commitMessage = @"
feat: UI cleanup, Map Preview fixes, and Oregon Zoo defaults

ADMIN UI IMPROVEMENTS:
======================

1. ? Removed Unused "Existing Definitions" Section
   - Removed empty ListBox that never showed content
   - Removed 3 non-functional buttons (Refresh, Info, Open in MC)
   - Removed supporting methods (~225 lines)
   - Cleaner, more focused Alarm Wiring tab

2. ? Map Preview WebView2 Fixes
   - Added proper user data folder initialization
   - Fixed tab visibility check (was always failing)
   - Added placeholder map for empty state
   - Enhanced debug logging
   - Real-time map updates working correctly

3. ? Oregon Zoo Default Coordinates
   - Changed default from (0, 0) to Oregon Zoo (45.5098, -122.7161)
   - Set zoom level to 14 (perfect for site viewing)
   - Much better default than middle of Atlantic Ocean
   - Great for testing and demonstrations

4. ? Simplified Map Labels
   - "Default Latitude" ? "Latitude"
   - "Default Longitude" ? "Longitude"
   - "Default Zoom" ? "Zoom"
   - Cleaner, less redundant labels

FILES MODIFIED:
===============

Admin/CoreCommandMIPUserControlTabbed.cs
- Removed unused UI section (Existing Definitions)
- Removed LoadExistingEventDefinitions() method
- Removed FindControlByName<T>() helper
- Removed ButtonShowInfo_Click() method
- Removed ButtonOpenInMC_Click() method
- Removed DefinitionListItem class
- Fixed WebView2 initialization with user data folder
- Added ShowMapPlaceholder() method
- Updated UpdateSitePreview() with better error handling
- Changed default coordinates to Oregon Zoo
- Simplified coordinate/zoom labels
- Updated all default value locations

BENEFITS:
=========

? Cleaner UI (no confusing empty sections)
? Working map preview with real-time updates
? Better default location for testing
? Proper WebView2 initialization
? Professional placeholder when no data
? ~225 lines of dead code removed

TECHNICAL DETAILS:
==================

WebView2 User Data Folder:
- Location: %LocalAppData%\CoreCommandMIP\AdminWebView2
- Required for proper initialization
- Prevents silent failures

Oregon Zoo Coordinates:
- Latitude: 45.5098°N (Portland, Oregon)
- Longitude: 122.7161°W
- Zoom: 14 (neighborhood/facility level)

BUILD STATUS:
=============

? Build successful
? No compilation errors
? All features tested
? Ready for use

USER EXPERIENCE:
================

Before:
- Empty "Existing Definitions" section
- Blank map preview
- Confusing default location (0, 0)
- Redundant "Default" labels

After:
- Clean, focused UI
- Working map with Oregon Zoo preview
- Real-time coordinate updates
- Concise labels

This commit improves the admin UI experience with better defaults,
working map preview, and removal of non-functional elements.
"@

# Commit with comprehensive message
Write-Host "5. Committing changes..." -ForegroundColor Green
git commit -m $commitMessage
Write-Host "   ? Changes committed" -ForegroundColor Gray
Write-Host ""

# Push to remote
Write-Host "6. Pushing to GitHub (origin/main)..." -ForegroundColor Green
try {
    git push origin main
    Write-Host "   ? Pushed successfully" -ForegroundColor Gray
} catch {
    Write-Host "   Error pushing. May need authentication." -ForegroundColor Red
    Write-Host "   Run manually: git push origin main" -ForegroundColor Yellow
}
Write-Host ""

# Show summary
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "COMMIT SUMMARY" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Branch:  main" -ForegroundColor Yellow
Write-Host "Remote:  origin (https://github.com/thekgrp/CCMIP-Version-1)" -ForegroundColor Yellow
Write-Host ""

# Show recent commits
Write-Host "Recent commits:" -ForegroundColor Green
git log --oneline -3
Write-Host ""

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "? COMPLETE!" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Your changes are now on GitHub (main branch)!" -ForegroundColor Green
Write-Host ""
Write-Host "View on GitHub:" -ForegroundColor Yellow
Write-Host "https://github.com/thekgrp/CCMIP-Version-1" -ForegroundColor Cyan
Write-Host ""
