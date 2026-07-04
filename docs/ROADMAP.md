# Roadmap

## v0.1 - AAF export MVP

- [x] Project skeleton
- [x] AAF reader
- [x] CLI `info`
- [x] CLI `export`
- [x] Individual glyph PNG export
- [x] Atlas PNG export
- [ ] Test with Fallout `FONT0.AAF` through `FONT4.AAF`

## v0.2 - AAF round-trip

- [ ] Import edited PNG glyphs
- [ ] Save `.AAF`
- [ ] Preserve original header bytes
- [ ] Validate in Fallout 1/2 and Fallout Et Tu

## v0.3 - TTF export

- [x] Generate monochrome/vectorized TTF from `.AAF`
- [x] Preserve glyph advance widths
- [ ] Validate generated TTF in Windows and Photoshop
- [ ] Add PT-BR glyph generation helpers

## v0.4 - FRM workflow

- [ ] Read `COLOR.PAL`
- [ ] Export `.FRM` with correct palette
- [ ] Reimport `.FRM`
- [ ] Preview UI text replacement
