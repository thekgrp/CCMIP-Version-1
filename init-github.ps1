# Initialize Git Repository and Prepare for GitHub
# Project: CCMIP Version 1

Write-Host "=== CoreCommandMIP Git Initialization ===" -ForegroundColor Cyan
Write-Host ""

# Check if Git is installed
try {
    $gitVersion = git --version
    Write-Host "? Git is installed: $gitVersion" -ForegroundColor Green
} catch {
    Write-Host "? Git is not installed!" -ForegroundColor Red
    Write-Host "Please install Git from: https://git-scm.com/download/win"
    exit 1
}

# Initialize Git repository
Write-Host ""
Write-Host "Step 1: Initializing Git repository..." -ForegroundColor Yellow

if (Test-Path ".git") {
    Write-Host "? Git repository already initialized" -ForegroundColor Green
} else {
    git init
    Write-Host "? Git repository initialized" -ForegroundColor Green
}

# Configure Git (update with your info)
Write-Host ""
Write-Host "Step 2: Configuring Git..." -ForegroundColor Yellow
Write-Host "Current Git configuration:"
$userName = git config user.name
$userEmail = git config user.email

if ($userName) {
    Write-Host "  Name: $userName"
} else {
    Write-Host "  Name: Not configured" -ForegroundColor Yellow
    Write-Host "  Run: git config --global user.name 'Your Name'"
}

if ($userEmail) {
    Write-Host "  Email: $userEmail"
} else {
    Write-Host "  Email: Not configured" -ForegroundColor Yellow
    Write-Host "  Run: git config --global user.email 'your.email@example.com'"
}

# Add all files
Write-Host ""
Write-Host "Step 3: Adding files to Git..." -ForegroundColor Yellow
git add .
Write-Host "? Files added to staging area" -ForegroundColor Green

# Show status
Write-Host ""
Write-Host "Step 4: Git status:" -ForegroundColor Yellow
git status --short

# Create initial commit
Write-Host ""
Write-Host "Step 5: Creating initial commit..." -ForegroundColor Yellow
git commit -m "Initial commit - CoreCommandMIP v1.0.0

Features:
- Dual map support (Leaflet and Mapbox)
- Real-time track visualization with custom icons
- Track list data grid view
- Region/geofence display with selection
- Automatic alarm generation and Event Server integration
- Multi-site configuration support
- Change-based polling optimization
- Comprehensive logging and diagnostics"

if ($LASTEXITCODE -eq 0) {
    Write-Host "? Initial commit created" -ForegroundColor Green
} else {
    Write-Host "? Commit failed - check for errors above" -ForegroundColor Red
}

# Instructions for GitHub
Write-Host ""
Write-Host "=== Next Steps: Create GitHub Repository ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Go to GitHub.com and sign in"
Write-Host "2. Click '+' in top-right corner ? 'New repository'"
Write-Host "3. Repository name: CCMIP-Version-1"
Write-Host "4. Description: CoreCommandMIP - Milestone XProtect MIP Plugin for Track Monitoring and Mapping"
Write-Host "5. Choose: Public or Private"
Write-Host "6. Do NOT initialize with README (we already have one)"
Write-Host "7. Click 'Create repository'"
Write-Host ""
Write-Host "8. GitHub will show commands - use these:" -ForegroundColor Yellow
Write-Host ""
Write-Host "   git remote add origin https://github.com/YOUR_USERNAME/CCMIP-Version-1.git" -ForegroundColor White
Write-Host "   git branch -M main" -ForegroundColor White
Write-Host "   git push -u origin main" -ForegroundColor White
Write-Host ""
Write-Host "=== Or use these commands now ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "After creating the GitHub repository, run:" -ForegroundColor Yellow
Write-Host ""
Write-Host "# Replace YOUR_USERNAME with your actual GitHub username"
Write-Host 'git remote add origin https://github.com/YOUR_USERNAME/CCMIP-Version-1.git'
Write-Host 'git branch -M main'
Write-Host 'git push -u origin main'
Write-Host ""
Write-Host "=== Repository Information ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Project: CoreCommandMIP Version 1"
Write-Host "Type: Milestone XProtect MIP Plugin"
Write-Host "Framework: .NET Framework 4.8"
Write-Host "Components: Smart Client + Event Server"
Write-Host ""

# Show repository stats
Write-Host "=== Repository Statistics ===" -ForegroundColor Cyan
$fileCount = (git ls-files | Measure-Object).Count
$csharpFiles = (git ls-files *.cs | Measure-Object).Count
$xamlFiles = (git ls-files *.xaml | Measure-Object).Count

Write-Host "Total files tracked: $fileCount"
Write-Host "C# files: $csharpFiles"
Write-Host "XAML files: $xamlFiles"
Write-Host ""

Write-Host "? Git repository ready for GitHub!" -ForegroundColor Green
Write-Host ""
