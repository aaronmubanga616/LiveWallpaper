# Commit and push this project to GitHub

This repository is already initialized on the `main` branch, but it has no commits and no remote configured. Follow these steps from PowerShell in the project folder.

## 1. Create an empty GitHub repository

On GitHub, create a repository and choose whether it should be public or private. Leave the options to add a README, `.gitignore`, or license unchecked; the project already has local files to push.

## 2. Open the project folder in PowerShell

```powershell
Set-Location "$env:USERPROFILE\Desktop\LiveWallpaper"
```

## 3. Connect this folder to your GitHub repository

Replace the placeholders with your GitHub username and repository name:

```powershell
git remote add origin https://github.com/<YOUR-USERNAME>/<YOUR-REPOSITORY>.git
git remote -v
```

If Git says `origin` already exists, update it instead:

```powershell
git remote set-url origin https://github.com/<YOUR-USERNAME>/<YOUR-REPOSITORY>.git
```

## 4. Review and stage the project files

```powershell
git status --short
git add -A
git status --short
git diff --cached --stat
git diff --cached --name-only
```

The initial commit should contain these project files:

- `.gitignore`
- `GITHUB_COMMIT_GUIDE.md`
- `App.xaml`
- `App.xaml.cs`
- `AssemblyInfo.cs`
- `Assets/855432-hd_1840_1034_25fps.mp4`
- `LiveWallpaper.csproj`
- `MainWindow.xaml`
- `MainWindow.xaml.cs`

Check the staged list before continuing. `bin/` and `obj/` should not appear; `.gitignore` excludes those generated build files. The MP4 asset is about 36.5 MB and is included because it is part of the project.

## 5. Create the first commit

```powershell
git commit -m "Initial commit: LiveWallpaper project"
```

## 6. Push the `main` branch to GitHub

```powershell
git push -u origin main
```

Git may open a browser or credential prompt for GitHub sign-in. Do not put a password or access token directly into the remote URL.

## 7. Confirm the push

```powershell
git status --short --branch
```

The working tree should be clean and the branch should show that it tracks `origin/main`. Refresh the GitHub repository page to see the committed files.
