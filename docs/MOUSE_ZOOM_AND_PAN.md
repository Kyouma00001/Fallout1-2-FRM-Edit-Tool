# Mouse zoom and panning

This hotfix completes the zoom/pan implementation in the visual editor.

## Added

- Mouse wheel zoom on the canvas area.
- Left-button panning when dragging the empty/base image area.
- Zoom keeps the pointer location stable while scrolling.
- Export size and coordinates remain unchanged; zoom is visual only.

## Notes

Dragging text items and erase patches should continue to work normally because those controls handle their own pointer events.
