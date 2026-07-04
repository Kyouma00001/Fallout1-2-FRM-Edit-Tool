# Roadmap

## Done / current

- [x] Project skeleton with Core, CLI, UI, and tests.
- [x] AAF reader.
- [x] AAF glyph PNG export and atlas export.
- [x] AAF text renderer.
- [x] Batch text rendering.
- [x] UI composition over PNG/BMP images.
- [x] Experimental AAF to TTF export.
- [x] Static FRM info/export/import workflow.
- [x] Indexed BMP 8-bit export with ACT palette.
- [x] Visual editor for static UI text placement.
- [x] Text move/resize handles.
- [x] Erase/clone patches.
- [x] Project save/load with `.fui.json`.
- [x] Open FRM and export FRM directly from the editor.
- [x] Safety checks to prevent unsafe overwrites.
- [x] Fallout-inspired editor theme.
- [x] Recent file picker directories.
- [x] Zoom controls, mouse wheel zoom, and canvas panning.

## v0.1 release candidate

- [ ] Final pre-PR cleanup.
- [ ] Release build validation.
- [ ] Manual in-game validation with edited static FRM.
- [ ] Merge polish/release PR.

## v0.2 packaging

- [ ] Publish Windows CLI binary.
- [ ] Publish Windows UI binary.
- [ ] Create clean user ZIP without game assets.
- [ ] Add basic release notes.

## Future improvements

- [ ] Split `MainWindow.cs` into smaller partial classes or services.
- [ ] Add automated tests for FRM reader/writer.
- [ ] Add automated tests for indexed BMP writer/reader.
- [ ] Add tests for editor settings serialization.
- [ ] Improve project missing-file recovery UX.
- [ ] Add optional grid/pixel overlay for precise editing.
- [ ] Add undo/redo for text and erase patch edits.
- [ ] Consider multi-frame FRM support.
- [ ] Consider safe synthetic fixture files for test coverage.
