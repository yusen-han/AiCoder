# AiCoder

A cross-platform AI chat client built with .NET MAUI. It connects to multiple LLM providers, supports multi-turn conversations with persistent sessions, handles images and file attachments, and can delegate desktop tasks to the model through a small set of local tools.

## Features

- **Multi-provider support** — Ollama, OpenAI, OpenAI-compatible endpoints, and the OpenAI Responses API.
- **Local-first providers** — runs against local models (e.g., `gpt-oss:20b` via Ollama) as well as hosted services.
- **Persistent sessions** — conversations and provider settings are saved locally and restored on launch.
- **Rich chat UI** — text, image, and tool-call bubbles; light/dark theme support; English and Simplified Chinese localization.
- **Attachments** — attach images from the gallery, capture a photo with the camera, or attach arbitrary files.
- **Speech-to-text** — dictate messages using the native speech recognizer.
- **Tool use** — the model can:
  - Read, write, and edit files
  - Search file contents across directories
  - Run PowerShell commands
  - Take screenshots
  - Ask the user follow-up questions inline
- **Tool approval modes** — choose whether tools run automatically, never run, or ask for approval each time.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A supported MAUI workload:
  - Android
  - iOS / Mac Catalyst
  - Windows 10.0.17763.0 or later

## Build & Run

Restore workloads and run the project for your target platform:

```bash
# Android
dotnet build -t:Run -f net10.0-android

# Windows (unpackaged)
dotnet build -t:Run -f net10.0-windows10.0.19041.0

# iOS (requires macOS + Xcode)
dotnet build -t:Run -f net10.0-ios

# Mac Catalyst
dotnet build -t:Run -f net10.0-maccatalyst
```

## Adding a Provider

Providers are configured through the in-app **Manage Providers** screen. Supported provider kinds:

| Kind            | Typical endpoint                | Notes                                        |
|-----------------|---------------------------------|----------------------------------------------|
| `ollama`        | `http://localhost:11434`        | Local models via Ollama; no API key required |
| `openai-chat`   | `https://api.openai.com/v1`     | Chat Completions API                         |
| `openai-responses` | `https://api.openai.com/v1`  | OpenAI Responses API                         |
| `openai-compatible` | Any OpenAI-compatible URL    | Works with Azure, local proxies, etc.        |

Provider configurations are stored in the app's local data directory as `providers.json`.

## Project Structure

```
AiCoder/
├── Converters/          # XAML value converters
├── Models/              # Data models and DTOs
├── Services/            # Storage, localization, provider factories
│   └── Factories/       # IProviderFactory implementations
├── Views/               # Additional pages and popups
├── Platforms/           # Platform-specific code (Android, iOS, MacCatalyst, Windows)
├── Resources/           # Icons, splash screens, fonts, and localized strings
├── MainPage.xaml        # Main chat UI
├── MainPage.xaml.cs     # Chat logic and tool implementations
├── MauiProgram.cs       # MAUI app bootstrap
└── App.xaml.cs          # App lifecycle
```

## Key Dependencies

- [Microsoft.Maui.Controls](https://github.com/dotnet/maui)
- [CommunityToolkit.Maui](https://github.com/CommunityToolkit/Maui)
- [Microsoft.Agents.AI](https://www.nuget.org/packages/Microsoft.Agents.AI)
- [Microsoft.Extensions.AI.OpenAI](https://www.nuget.org/packages/Microsoft.Extensions.AI.OpenAI)
- [OllamaSharp](https://github.com/awaescher/OllamaSharp)
- [OpenAI .NET](https://github.com/openai/openai-dotnet)

## License

This project is licensed under the [GNU General Public License v3.0](LICENSE).
