# Sprint 5 - Visual UI Text Editor

This package adds a first visual editor project:

```text
src/Fallout.Tools.UI
```

It lets you:

- open a clean PNG/BMP UI image;
- open an AAF font;
- add translated text visually;
- drag text with the mouse;
- save a layout file;
- export the composed PNG.

## Apply

Extract this package into the repository root.

Then add the UI project to the solution:

```bash
dotnet sln Fallout.Tools.sln add src/Fallout.Tools.UI/Fallout.Tools.UI.csproj
```

Build and test:

```bash
dotnet build
dotnet test
```

Run:

```bash
dotnet run --project src/Fallout.Tools.UI
```

## Scope

This sprint is for static UI images only. FRM reimport will come later and should target static/single-image FRM files first.
