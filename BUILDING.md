# Building Instructions
# Prerequisites
- .NET 8.0 SDK
- Visual Studio Code/vscode/VSCodium (optional)
- Ability to read
# Cloning the repository
## Linux
1. If you do not have it already, install git from your distribution's package manager.
2. Run `git clone https://github.com/null822/ElectroSim.git`
## Windows
### With vscode
1. Download [Git for Windows](https://gitforwindows.org)
2. Open vscode and click on the Source Control icon in the activity bar. The icon should look something like a branch.
3. Click on Clone Repository.
4. When prompted type `https://github.com/null822/ElectroSim.git`
### Without vscode
1. Download [Git for Windows](https://gitforwindows.org)
2. Open Git for Windows (BASH)
3. Run `git clone https://github.com/null822/ElectroSim.git`

# Building the code
## Linux
### Universal Executable (tar.gz)
- Run `debug.sh` for a debug executable for local testing
- Run `release.sh` to build a release version with no debug symbols
- If you want a self-contained build put `sc-` in front of the sh file you want to run.
- Optionally, compress the contents in `bin/(Debug|Release)/net8.0/linux-x64` into a tar.gz archive for easy sharing.
### AppImage
- Run `dl-appimagetool.sh` to download appimagetool (required for publish-appimage to work)
- Run `publish-appimage` to create a release appimage file
- If you really want you can change the configuration in `publish-appimage.conf` to publish a 'Debug' build but if you are really testing the program you might as well just build it normally
## Windows
- Run `debug.bat` for a debug build or `release.bat` for a release build.
- Run `sc-debug.bat` or `sc-release.bat` for a self-contained build.
