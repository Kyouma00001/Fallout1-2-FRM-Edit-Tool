# Sprint 4 - UI compose workflow

Adds `compose-ui`, a command that overlays AAF-rendered text onto a base BMP/PNG image using coordinate layouts.

Main use case:

```text
FRM -> BMP/PNG -> clean old text -> compose-ui -> PNG -> FRM
```

New files:

- `src/Fallout.Tools.Core/Imaging/UiTextAlignment.cs`
- `src/Fallout.Tools.Core/Imaging/UiTextPlacement.cs`
- `src/Fallout.Tools.Core/Imaging/UiTextLayoutParser.cs`
- `src/Fallout.Tools.Core/Imaging/UiTextComposer.cs`
- `docs/UI_COMPOSE.md`
- `samples/ui-compose.example.txt`
