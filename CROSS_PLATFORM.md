# Cross-Platform Support

This document describes the cross-platform architecture of the Evergine 3D Demo application.

## Project Structure

The application is organized into multiple projects to support different platforms:

### Core Projects

1. **EvergineDemo.Shared** - Shared models and contracts
   - Platform-agnostic data models
   - SignalR hub interfaces
   - Common utilities

2. **EvergineDemo.Backend** - Server backend
   - ASP.NET Core Web API
   - SignalR hub implementation
   - 3D simulation service

### Frontend Projects

#### Desktop
- **EvergineDemo.Frontend.Desktop** - Standalone desktop application
  - Target Framework: .NET 10.0
  - Platforms: Windows, macOS, Linux
  - Graphics: OpenGL
  - UI: Avalonia UI

#### Cross-Platform (Alternative Architecture)
- **EvergineDemo.Frontend.CrossPlatform** - Shared UI code
  - Target Framework: .NET 10.0
  - Contains ViewModels, Views, and business logic

- **EvergineDemo.Frontend.CrossPlatform.Android** - Android application
  - Target Framework: net10.0-android
  - Graphics: OpenGL ES / Vulkan

- **EvergineDemo.Frontend.CrossPlatform.iOS** - iOS application
  - Target Framework: net10.0-ios
  - Graphics: Metal (via Evergine)

- **EvergineDemo.Frontend.CrossPlatform.Browser** - WebAssembly application
  - Target Framework: net10.0-browser
  - Graphics: WebGL (via Evergine.Web)

## Platform-Specific Considerations

### Windows
- Uses DirectX 11 or OpenGL for rendering
- Full desktop window support
- Native file dialogs for STL loading

### macOS
- Uses Metal or OpenGL for rendering
- macOS-specific UI conventions
- Native file dialogs

### Linux
- Uses OpenGL or Vulkan for rendering
- X11/Wayland display server support
- GTK-based file dialogs

### iOS
- Uses Metal for rendering
- Touch-based interaction
- iOS file picker integration
- App Store distribution

### Android
- Uses OpenGL ES or Vulkan for rendering
- Touch-based interaction
- Android file picker integration
- Google Play Store distribution

### WebAssembly (Browser)
- Uses WebGL for rendering
- Browser-based file upload
- Runs in any modern web browser
- No installation required

## Evergine Packages by Platform

### Desktop Platforms
```xml
<PackageReference Include="Evergine.OpenGL" />
<PackageReference Include="Evergine.DirectX11" /> <!-- Windows only -->
<PackageReference Include="Evergine.Vulkan" />    <!-- Optional -->
```

### Mobile Platforms
```xml
<!-- iOS -->
<PackageReference Include="Evergine.iOS" />

<!-- Android -->
<PackageReference Include="Evergine.Android" />
```

### Web Platform
```xml
<PackageReference Include="Evergine.Web" />
<PackageReference Include="Evergine.Targets.Web" />
```

## Building for Different Platforms

### Desktop (Windows, macOS, Linux)
```bash
cd src/Frontend/EvergineDemo.Frontend.Desktop
dotnet build
dotnet run
```

### Android
```bash
cd src/Frontend/EvergineDemo.Frontend.CrossPlatform/EvergineDemo.Frontend.CrossPlatform.Android
dotnet build -f net10.0-android
```

### iOS
```bash
cd src/Frontend/EvergineDemo.Frontend.CrossPlatform/EvergineDemo.Frontend.CrossPlatform.iOS
dotnet build -f net10.0-ios
```

### WebAssembly
```bash
cd src/Frontend/EvergineDemo.Frontend.CrossPlatform/EvergineDemo.Frontend.CrossPlatform.Browser
dotnet build
dotnet run
```

## Platform-Specific Features

### File Picker Implementation

Each platform requires a specific file picker implementation:

**Desktop (Avalonia)**
```csharp
var storageProvider = TopLevel.GetTopLevel(this).StorageProvider;
var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
{
    Title = "Select STL File",
    AllowMultiple = false,
    FileTypeFilter = new[] { new FilePickerFileType("STL Files") { Patterns = new[] { "*.stl" } } }
});
```

**Mobile (iOS/Android)**
- Use platform-specific APIs via Avalonia's storage provider
- iOS: UIDocumentPickerViewController
- Android: Intent.ActionOpenDocument

**Web**
```html
<input type="file" accept=".stl" onchange="handleFileUpload(this.files[0])" />
```

### Touch vs Mouse Input

The application automatically adapts to touch or mouse input based on the platform:
- Desktop: Mouse click to select and drop models
- Mobile: Touch tap to select and drop models
- Web: Mouse or touch depending on device

### Performance Considerations

Each platform has different performance characteristics:

1. **Desktop**: Highest performance, can handle complex models
2. **Mobile**: Moderate performance, optimize for battery life
3. **Web**: Limited by browser capabilities, optimize for load time

## Deployment

### Desktop
- Self-contained executable with runtime included
- Platform-specific installers (MSI, PKG, DEB/RPM)
- No internet connection required after installation

### Mobile
- App Store (iOS) and Google Play Store (Android) distribution
- Platform-specific packaging (.ipa, .apk, .aab)
- Requires platform-specific signing certificates

### Web
- Host on any web server or CDN
- No installation required
- Progressive Web App (PWA) capabilities
- Requires HTTPS for production

## Future Enhancements

- [ ] Implement actual STL file loader
- [ ] Add platform-specific file pickers
- [ ] Optimize rendering for mobile devices
- [ ] Add offline mode for desktop/mobile
- [ ] Implement progressive loading for web
- [ ] Add platform-specific UI adaptations
- [ ] Implement touch gestures for mobile
- [ ] Add keyboard shortcuts for desktop
- [ ] Implement proper 3D model rendering with Evergine
- [ ] Add AR capabilities for mobile platforms
