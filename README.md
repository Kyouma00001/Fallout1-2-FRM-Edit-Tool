# Sprint 6 - Visual Resize Handles

This patch improves the visual UI text editor.

## Added

- Selection border around the active text object.
- Right-side handle to change the layout box width with the mouse.
- Bottom-right handle to resize the rendered text with the mouse.
- Arrow key movement:
  - Arrow keys move the selected object by 1 pixel.
  - Shift + arrow keys move it by 10 pixels.
- The side panel updates automatically while dragging/resizing.

## Usage

1. Open a clean UI image.
2. Open an AAF font.
3. Add a text object.
4. Click/select the text object.
5. Drag the text to move it.
6. Drag the right cyan handle to change the layout box width.
7. Drag the bottom-right cyan handle to resize the text.
8. Export the final PNG.

## Notes

This sprint does not add FRM import/export yet. It only improves visual editing of translated UI text over static images.
