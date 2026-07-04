# Visual UI Text Editor

The Visual UI Text Editor is a lightweight editor for placing translated AAF-rendered text on top of clean UI images.

This sprint focuses on static UI images only. It does not edit animated FRMs.

## Purpose

Typical workflow for Fallout 1/2 UI translation:

```text
static FRM
  -> export/convert to PNG or BMP
  -> remove original text in an image editor
  -> open the clean image in Fallout.Tools.UI
  -> open FONT3.AAF or FONT4.AAF
  -> add translated text visually with the mouse
  -> export final PNG
  -> later reimport the final image into a static FRM
```

## Run

From the repository root:

```bash
dotnet run --project src/Fallout.Tools.UI
```

If the project has not been added to the solution yet:

```bash
dotnet sln Fallout.Tools.sln add src/Fallout.Tools.UI/Fallout.Tools.UI.csproj
```

## Controls

- **Open image**: opens the clean PNG/BMP base image.
- **Open AAF**: opens a Fallout AAF font.
- **Add text**: creates a new text object.
- **Remove**: removes the selected text object.
- **Save layout**: saves the text positions as a text layout file.
- **Export PNG**: exports the final composed image.

Text objects can be dragged with the mouse.

## Text box layout

The saved layout uses the same format as the `compose-ui` command:

```text
NAME|X|Y|WIDTH|ALIGN|TEXT
```

Example:

```text
BARTER|10|8|80|center|negociar
TALK|95|8|60|center|falar
DONE|155|8|70|center|pronto
```

## Notes

- The editor currently exports PNG only.
- FRM reimport will be handled in a later sprint.
- This is intended for static UI images, not animated FRMs.
- The AAF text renderer preserves the original pixel texture of Fallout fonts.
