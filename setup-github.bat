@echo off
echo ============================================
echo CoreCommandMIP GitHub Setup
echo ============================================
echo.

:: Check if Git is installed
git --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] Git is not installed!
    echo.
    echo Please install Git from: https://git-scm.com/download/win
    echo.
    echo After installing:
    echo 1. Restart this command prompt
    echo 2. Run this script again
    echo.
    pause
    exit /b 1
)

echo [OK] Git is installed
echo.

:: Check Git configuration
echo Checking Git configuration...
git config user.name >nul 2>&1
if %errorlevel% neq 0 (
    echo.
    echo [SETUP REQUIRED] Please configure Git:
    echo.
    echo   git config --global user.name "Your Name"
    echo   git config --global user.email "your.email@example.com"
    echo.
    pause
    exit /b 1
)

:: Check if already a Git repository
if exist ".git" (
    echo [OK] Git repository already exists
    echo.
    goto :status
)

:: Initialize Git repository
echo Initializing Git repository...
git init
if %errorlevel% neq 0 (
    echo [ERROR] Failed to initialize Git repository
    pause
    exit /b 1
)
echo [OK] Git repository initialized
echo.

:: Add files
echo Adding files...
git add .
if %errorlevel% neq 0 (
    echo [ERROR] Failed to add files
    pause
    exit /b 1
)
echo [OK] Files added
echo.

:: Create initial commit
echo Creating initial commit...
git commit -m "Initial commit - CoreCommandMIP v1.0.0"
if %errorlevel% neq 0 (
    echo [ERROR] Failed to create commit
    pause
    exit /b 1
)
echo [OK] Commit created
echo.

:status
echo ============================================
echo Git Repository Status
echo ============================================
git status
echo.

echo ============================================
echo Next Steps
echo ============================================
echo.
echo 1. Create repository on GitHub.com:
echo    - Name: CCMIP-Version-1
echo    - Description: CoreCommandMIP MIP Plugin
echo    - Do NOT initialize with README
echo.
echo 2. Connect to GitHub (replace YOUR_USERNAME):
echo.
echo    git remote add origin https://github.com/YOUR_USERNAME/CCMIP-Version-1.git
echo    git branch -M main
echo    git push -u origin main
echo.
echo 3. See GITHUB_SETUP_GUIDE.md for detailed instructions
echo.
pause
