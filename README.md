# LiveWallpaper

A Windows WPF app for previewing a video and using it as a live desktop background.

## Features

- Preview the bundled video or choose another video file.
- Play, pause, seek, loop, and adjust preview volume.
- Apply a video across the virtual desktop, with fill, fit, or stretch sizing.
- Mute the wallpaper by default and stop it to reveal the existing Windows background.

## Run

Open `LiveWallpaper.csproj` in Visual Studio, or build and run it from PowerShell:

```powershell
dotnet build
dotnet run
```

The project targets .NET 9 for Windows. WPF playback depends on Windows codecs; H.264 MP4 is the best starting format.

## Desktop integration note

Windows does not provide a public API for animated desktop wallpapers. This app attaches its background window to the Explorer `WorkerW` desktop layer. That shell arrangement is not a supported wallpaper contract and may change with Windows updates or display configurations. If attachment fails, the app reports the error and leaves the existing desktop background in place. Closing the app stops the video and reveals that background again.
