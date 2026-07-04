# Visual Resize Handles

The visual editor now supports mouse-based layout changes.

## Controls

| Action | Result |
|---|---|
| Drag text | Move X/Y |
| Drag right handle | Change Width/layout box |
| Drag bottom-right handle | Resize rendered text |
| Arrow keys | Move selected text by 1 px |
| Shift + arrow keys | Move selected text by 10 px |

The numeric fields remain available for precision edits.

## Layout compatibility

The editor keeps the existing layout serialization fields:

```text
NAME|X|Y|WIDTH|ALIGN|SCALE|WIDTHSCALE|HEIGHTSCALE|LETTERSPACING|uppercase|TEXT
```

So layouts saved before this sprint remain conceptually compatible with the current workflow, although loading layouts is not implemented yet.
