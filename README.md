# Infinite Quest

A 2D game built with [MonoGame 3.8](https://monogame.net/) and .NET 9.0.

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (or newer)
- [MonoGame Content Builder](https://docs.monogame.net/articles/getting_started/1_setting_up_your_development_environment.html) (included as a dotnet tool)

## Project Structure

```txt
src/
  Core/       Game logic library
  Desktop/    Desktop executable (OpenGL)
```

- **Core** contains the `MainGame` class and shared game logic. Game content assets live in `Core/Content/`.
- **Desktop** is the platform-specific launcher that references Core and builds content via the MonoGame Content Pipeline.

## Getting Started

### 1. Restore tools and dependencies

From the repository root:

```bash
dotnet tool restore
dotnet restore
```

### 2. Build and run

From the repository root:

```bash
dotnet build CatalinaLabs.InfiniteQuest.slnx
dotnet run --project src/Desktop
```

Or from the Desktop project directory:

```bash
cd src/Desktop
dotnet run
```

### 3. Debug in VS Code

Open `src/Desktop` in VS Code and press **F5** to launch using the included `launch.json` configuration.

## Managing Content

Game assets (textures, fonts, sounds, etc.) are managed through the MonoGame Content Pipeline.

To open the content editor:

```bash
cd src/Core
dotnet mgcb-editor Content/Content.mgcb
```

The Desktop project references `Core/Content/Content.mgcb` via a `<MonoGameContentReference>` in its `.csproj`, so any assets added in the editor are automatically built when you compile the Desktop project.

## Publishing

```bash
dotnet publish src/Desktop -c Release -r win-x64 --self-contained
```

Replace `win-x64` with `linux-x64` or `osx-x64` for other platforms.
