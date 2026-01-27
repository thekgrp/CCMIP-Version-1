# CoreCommandMIP GitHub Setup Guide

## Prerequisites

### Install Git for Windows

1. **Download Git:**
   - Go to: https://git-scm.com/download/win
   - Download and run the installer
   - Use default options during installation

2. **Verify Git is installed:**
   ```powershell
   git --version
   ```
   Should show: `git version 2.x.x.windows.x`

### Configure Git (First Time Setup)

```powershell
# Set your name (will appear in commits)
git config --global user.name "Your Name"

# Set your email (use your GitHub email)
git config --global user.email "your.email@example.com"

# Verify configuration
git config --global --list
```

## Step 1: Initialize Local Git Repository

**Open PowerShell in your project directory:**

```powershell
# Navigate to project root (where CoreCommandMIP.sln is)
cd C:\Users\EdwardKnoch\source\repos\CoreCommandMIP

# Initialize Git repository
git init

# Add all files
git add .

# Create initial commit
git commit -m "Initial commit - CoreCommandMIP v1.0.0"
```

## Step 2: Create GitHub Repository

### Option A: Via GitHub Website (Recommended)

1. **Go to GitHub.com** and sign in (create account if needed)

2. **Create new repository:**
   - Click **+** icon (top-right) ? **New repository**
   - **Repository name:** `CCMIP-Version-1`
   - **Description:** `CoreCommandMIP - Milestone XProtect MIP Plugin for Track Monitoring and Mapping`
   - **Visibility:** Choose **Private** or **Public**
   - **Important:** Do NOT check "Initialize with README" (we already have one)
   - Click **Create repository**

3. **GitHub will show connection commands** - copy them for next step

### Option B: Via GitHub CLI (Advanced)

If you have GitHub CLI installed:

```powershell
# Install GitHub CLI first (one-time)
winget install GitHub.cli

# Login to GitHub
gh auth login

# Create repository
gh repo create CCMIP-Version-1 --private --source=. --remote=origin --push
```

## Step 3: Connect Local Repository to GitHub

**After creating the GitHub repository**, connect your local code:

```powershell
# Add GitHub as remote (replace YOUR_USERNAME)
git remote add origin https://github.com/YOUR_USERNAME/CCMIP-Version-1.git

# Rename branch to 'main' (GitHub default)
git branch -M main

# Push code to GitHub
git push -u origin main
```

**Example (replace with your username):**
```powershell
git remote add origin https://github.com/edwardknoch/CCMIP-Version-1.git
git branch -M main
git push -u origin main
```

**You'll be prompted for credentials:**
- Use your GitHub username
- For password, use a **Personal Access Token** (not your account password)

## Step 4: Create Personal Access Token (If Needed)

If push asks for credentials:

1. **Go to GitHub.com** ? Settings ? Developer settings ? Personal access tokens ? Tokens (classic)
2. **Generate new token (classic)**
3. **Name:** `CoreCommandMIP Development`
4. **Expiration:** Choose duration (90 days recommended)
5. **Scopes:** Check `repo` (full control of private repositories)
6. **Generate token** and **copy it** (you won't see it again!)
7. **Use token as password** when Git asks

## Step 5: Verify Upload

1. **Go to your GitHub repository:**
   ```
   https://github.com/YOUR_USERNAME/CCMIP-Version-1
   ```

2. **You should see:**
   - All your source code files
   - README.md displayed on main page
   - File structure matching your local project

## Daily Workflow

### Making Changes and Committing

```powershell
# Check what files changed
git status

# Add specific files
git add Client/MapTemplate.cs

# Or add all changed files
git add .

# Commit with message
git commit -m "Fix: Updated icon loading logic"

# Push to GitHub
git push
```

### Common Commands

```powershell
# View commit history
git log --oneline

# View changes before committing
git diff

# Undo changes to a file
git checkout -- filename.cs

# Create a new branch for features
git checkout -b feature/new-alarm-system

# Switch branches
git checkout main

# Merge branch into main
git merge feature/new-alarm-system

# Pull latest changes from GitHub
git pull
```

## .gitignore

Already created! This file tells Git to ignore:
- Build output (`bin/`, `obj/`)
- Visual Studio files (`.vs/`, `*.user`)
- Temporary files
- NuGet packages folder

## Repository Structure on GitHub

```
CCMIP-Version-1/
??? .gitignore              (Git ignore rules)
??? README.md               (Project documentation)
??? CoreCommandMIP.sln      (Visual Studio solution)
??? CoreCommandMIPDefinition.cs
??? RemoteServerSettings.cs
??? Admin/                  (Configuration UI)
??? Background/             (Event Server plugin)
??? Client/                 (Smart Client plugin)
?   ??? MapTemplate.cs
?   ??? MapboxTemplate.cs
?   ??? CoreCommandMIPViewItemWpfUserControl.cs
?   ??? ...
??? assets/                 (Icon images)
??? Properties/
```

## Recommended: Add .editorconfig

For consistent code style across developers:

```ini
# .editorconfig
root = true

[*]
charset = utf-8
end_of_line = crlf
insert_final_newline = true
indent_style = tab
tab_width = 4

[*.cs]
indent_style = tab
csharp_indent_case_contents = true
csharp_space_after_cast = false
```

## Branching Strategy

### Simple Workflow:
- `main` - stable, tested code
- `dev` - active development
- `feature/feature-name` - specific features

### Create branches:
```powershell
# Create and switch to dev branch
git checkout -b dev

# Create feature branch from dev
git checkout -b feature/better-alarm-filtering
```

## Troubleshooting

### "Git is not recognized"
- Restart PowerShell after installing Git
- Or add Git to PATH manually

### "Authentication failed"
- Use Personal Access Token instead of password
- Make sure token has `repo` scope

### "Everything up-to-date" but changes not on GitHub
- Make sure you committed: `git commit -m "message"`
- Then push: `git push`

### Want to undo last commit
```powershell
# Undo commit but keep changes
git reset --soft HEAD~1

# Undo commit and discard changes (careful!)
git reset --hard HEAD~1
```

## Collaborative Development

### Inviting Others to Repository

1. **GitHub repository** ? Settings ? Collaborators
2. **Add people** by GitHub username or email
3. They can now push/pull to your repository

### Handling Conflicts

When multiple people edit same file:

```powershell
# Pull latest changes
git pull

# If conflict, Git will mark conflicts in files
# Edit files to resolve conflicts
# Then:
git add conflicted-file.cs
git commit -m "Resolved merge conflict"
git push
```

## Current Status

? Created `.gitignore` for .NET projects
? Created `README.md` with full documentation
? Created `init-github.ps1` setup script
? Created this comprehensive guide

## Next Steps

1. **Install Git** if not already installed
2. **Run commands** from Step 1 to initialize local repo
3. **Create GitHub repository** (Step 2)
4. **Connect and push** (Step 3)
5. **Verify** upload successful (Step 5)

## Quick Start (After Git Installed)

```powershell
# One-time setup
git config --global user.name "Your Name"
git config --global user.email "your@email.com"

# Initialize and commit
git init
git add .
git commit -m "Initial commit - CoreCommandMIP v1.0.0"

# After creating GitHub repo, connect it
git remote add origin https://github.com/YOUR_USERNAME/CCMIP-Version-1.git
git branch -M main
git push -u origin main
```

**That's it! Your code is now on GitHub!** ??
