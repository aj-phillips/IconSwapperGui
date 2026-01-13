# IconSwapperGui – Windows Shortcut & Folder Icon Changer

[![License](https://img.shields.io/github/license/aj-phillips/IconSwapperGui)](LICENSE)
[![Latest Release](https://img.shields.io/github/v/release/aj-phillips/IconSwapperGui)](https://github.com/aj-phillips/IconSwapperGui/releases)
[![Downloads](https://img.shields.io/github/downloads/aj-phillips/IconSwapperGui/total)](https://github.com/aj-phillips/IconSwapperGui/releases)
[![Stars](https://img.shields.io/github/stars/aj-phillips/IconSwapperGui?style=social)](https://github.com/aj-phillips/IconSwapperGui/stargazers)
[![Issues](https://img.shields.io/github/issues/aj-phillips/IconSwapperGui)](https://github.com/aj-phillips/IconSwapperGui/issues)
[![Last Commit](https://img.shields.io/github/last-commit/aj-phillips/IconSwapperGui)](https://github.com/aj-phillips/IconSwapperGui/commits/main)
![Platform](https://img.shields.io/badge/platform-Windows-blue)
![Framework](https://img.shields.io/badge/.NET-8-purple)

**IconSwapperGui** is a powerful **Windows desktop application** for changing **shortcut and folder icons** without navigating Windows' properties menus. It supports **.lnk shortcuts**, **Steam .url files**, and **folder icons**, making it ideal for customizing your Windows desktop experience.

A standout feature of IconSwapperGui is **icon version management**, allowing you to track, preview, and restore **previously used icons** for any shortcut — making experimentation safe and fully reversible.

---

## 🚀 Features

### 🔄 Icon Swapper
- **Multi-Location Support**  
  - Configure multiple shortcut and icon folder locations
  - Automatically loads all shortcuts and icons from configured directories
  
- **Dual-Mode Icon Swapping**  
  - **Shortcuts Tab**: Change icons for **.lnk** and **Steam .url** shortcut files
  - **Folders Tab**: Change icons for Windows folders directly
  
- **Icon Version History & Restore** ⭐  
  - Right-click any shortcut to view **current and previous icon versions**
  - Preview all historical icons with timestamps
  - Instantly restore a previously used icon with one click
  - Safely experiment with icon styles without losing past versions
  
- **Advanced Icon Search & Filtering**  
  - Real-time search bar to quickly find icons by name
  - Instant filtering across all loaded icon directories
  
- **Auto-Refresh & File Watching**  
  - Automatically detects newly added shortcuts or icons while the app is running
  - No need to manually refresh — changes are detected in real-time

---

### 🔁 Icon Converter (PNG / JPG → ICO)
- **Batch Image Conversion**  
  - Convert entire folders of images into Windows-compatible **.ico** files
  
- **Multi-Format Support**  
  - Supports **.png**, **.jpg**, and **.jpeg** image formats
  
- **Searchable Image List**  
  - Quickly locate specific images before conversion with built-in search
  
- **Multiple Icon Directories**  
  - Configure multiple source folders for image conversion
  
- **Optional Image Cleanup**  
  - Automatically delete original images after successful conversion
  
- **Quality Preservation**  
  - Maintains image quality during conversion process

---
 
### 🎨 Pixel Art Icon Editor
- **Custom Canvas Size**  
  - Adjustable grid up to **512×512** pixels
  
- **Background Customization**  
  - Choose any background color
  - Full transparency support for professional-looking icons
  
- **Professional Drawing Tools**  
  - Color picker for precise brush color selection
  - Left-click to draw pixels
  - Right-click to erase pixels
  - Undo/Redo support
  
- **Dynamic Zoom Control**  
  - Zoom in and out for precise pixel-level editing
  - Grid visualization for accurate pixel placement
  
- **Export Functionality**  
  - Save your pixel art directly as **.ico** files
  - Ready to use immediately as Windows icons

---
 
### ⚙️ Application Settings

#### Appearance
- **Theme Selection**  
  - Light, Dark and Custom mode themes

- **Custom Colours** (custom mode enabled)
    - Accent Colour
    - Background
    - Surface
    - Primary Text
    - Secondary Text
  
#### Application
- **Shortcut Locations**  
  - Add/remove multiple shortcut folder paths
    - The desktop path is a common one
  - Supports both .lnk and .url shortcuts
  
- **Icon Locations**  
  - Configure multiple icon source directories
  - Automatically scans for .ico files
  
- **Folder Shortcut Locations**  
  - Manage folders for folder icon swapping
  
- **Converter Icons Locations**  
  - Configure image source folders for conversion
  
- **Export Location**  
  - Set default save location for pixel art creations

#### General
- **Auto-Update Settings**  
  - Toggle automatic update checks on startup
  - Manual update check available
  - One-click update installation
  
- **Launch At Startup**  
  - Launch IconSwapperGui automatically with Windows

#### Notifications
- **Sound Notifications**  
  - Toggle sound alerts for operations
  
#### Advanced
- **Logging**  
  - Enable detailed logging for troubleshooting
  - Logs saved to application directory

---

### 🔄 Built-In Auto-Updater (Velopack)
- **Seamless Automatic Updates**  
  - Checks for updates on startup (configurable)
  - Download and install updates without leaving the app
  
- **GitHub Release Integration**  
  - Updates fetched directly from official GitHub releases
  - Secure and verified update process
  
- **Manual Update Check**  
  - Check for updates anytime from Settings
  - Clear update status messages
  
- **Delta Updates**  
  - Only downloads changed files for faster updates
  - Minimal bandwidth usage

---

## 🏁 Getting Started

### Installation

1. **Download the Installer**  
   - Visit the [**Releases**](https://github.com/aj-phillips/IconSwapperGui/releases) page
   - Download `IconSwapperGui-win-Setup.exe`

2. **Run the Installer**  
   - Double-click `IconSwapperGui-win-Setup.exe`
   - Follow the installation wizard
   - The app will be installed to your system

3. **Launch IconSwapperGui**  
   - Use the Start Menu shortcut or desktop icon
   - The app will automatically check for updates on first launch

### First-Time Setup

1. **Configure Shortcut Locations**  
   - Open **Settings** > **Application**
   - Add folders containing your shortcuts (e.g., Desktop, Start Menu)
   
2. **Configure Icon Locations**  
   - Add folders containing your custom .ico files
   - IconSwapperGui will automatically scan these directories

3. **Start Swapping Icons**  
   - Navigate to the **Swapper** sidebar tab
   - Select a shortcut from the left panel
   - Select an icon from the right panel
   - Click **Swap**

### Additional Features

- **Manage Icon History**: Right-click any shortcut to view and restore previous icons
- **Convert Images**: Use the **Converter** tab to turn PNG/JPG images into .ico files
- **Create Custom Icons**: Use the **Pixel Art Editor** tab to design icons from scratch

---

## 🔄 Updates

IconSwapperGui includes a built-in updater powered by **Velopack**. By default, the app checks for updates on startup. You can:
- Disable automatic checks in **Settings** > **General**
- Manually check for updates anytime in **Settings** > **About**
- Install updates with one click when available

---

## 💬 Feedback & Support

Your feedback is invaluable!  
If you encounter bugs or have feature requests, please open an issue on the [**GitHub Issues**](https://github.com/aj-phillips/IconSwapperGui/issues) page.

---

## 🤝 Contributing

Contributions are welcome!  
Please read the [**Contributing Guidelines**](CONTRIBUTING.md) before submitting a pull request.

---

## 📄 License

This project is licensed under the [GNU General Public License v3.0](LICENSE).

---

## 🛠️ Built With

- **.NET 10** - Modern cross-platform framework
- **WPF** - Windows Presentation Foundation for rich UI
- **Velopack** - Auto-update framework
- **CommunityToolkit.Mvvm** - MVVM architecture support

---

## ⭐ Show Your Support

If you find IconSwapperGui useful, please consider giving it a star on GitHub! It helps others discover the project and motivates continued development.
