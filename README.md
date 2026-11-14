> **Moved to uno-lab.** This project now lives in the `memory-drift/` folder of [noknowscode/uno-lab](https://github.com/noknowscode/uno-lab/tree/main/memory-drift) (private). This repository is archived and kept for its history.

# Memory Drift

An infinite 3D photo canvas: fly through an endless space of images from a photo library, with a landing page and a timeline scrubber.

## What's inside

- **Screens:** Landing.
- **Custom controls:** `ImagePlaneControl`, `TimelineScrubberControl`.

## Built with

- Uno Platform single project, `Uno.Sdk` 6.5.36 (pinned in `global.json`).
- Targets: Android, WebAssembly, Desktop (net10.0).
- Material theme, Hosting and dependency injection, Uno Toolkit, MVVM (CommunityToolkit), MVUX, Skia renderer.

## Run it

You need the .NET 10 SDK. Run `uno-check` first on a fresh machine.

```bash
dotnet run --project InfiniteImage/InfiniteImage.csproj -f net10.0-desktop
```

## Design docs

- [`DESIGN_ARCHITECTURE_BRIEF.md`](DESIGN_ARCHITECTURE_BRIEF.md)

## License

MIT. See [LICENSE](LICENSE).
