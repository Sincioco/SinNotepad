# Validation

Validated on Windows 11 x64 with .NET SDK 10.0.400 on September 11, 2026.

## Final checks

- Release solution build: passed, zero warnings and zero errors.
- Self-contained Windows x64 package: passed; runtime bundled in one executable.
- Core tests: **37 passed, 0 failed**.
- WPF editor integration tests: **40 passed**.
- Source whitespace formatting and Git whitespace checks: passed.

Core tests exercise Unicode/encoding and line-ending round trips, unchanged mixed endings, external-edit
conflicts, read-only files, rejected lossy encoding conversions, literal search/replacement, numbered names,
counter persistence, immediate auto-save file creation, numbering collisions, session backup recovery,
caret-offset conversion, binary rejection, and a large-text save/reopen.

The integration tests use the actual WPF editor and exercise menu rendering, native-title-bar configuration,
full-path titles, numbered tabs, independent documents, line numbers and caret positions, zoom, list/tabs
toggling, sidebar sizing and width persistence, undo/redo retention, find/replace, reordered tabs, long-document
scrolling, session restoration, selection restoration across CRLF normalization, physical auto-save files,
auto-save edits, write conflicts, and resetting the document sequence.

## Native interface observations

- Launched the packaged executable successfully.
- Observed the standard Windows title bar, tabs beneath it, restored text, aligned line numbers, and status bar.
- Used Ctrl+Shift+L to switch to Document List; confirmed text and caret remained in place.
- Dragged the divider from roughly 250 to 350 pixels; the list and editor resized correctly.
- Used Ctrl+Plus; text and gutter enlarged together and the status changed from 100% to 110%.
- Opened Settings and inspected the auto-save folder, Browse, persistent next number and Reset to 1 controls.
- Cancelled Settings without changing preferences.
- Opened File and verified plain-text commands, with no printing or Markdown commands.

The initial compact-menu clipping defect and the CRLF caret-restoration mismatch were corrected and covered
by focused regression checks before the final build. The SDK formatting host initially followed a stale
DOTNET_ROOT; rerunning with the installed system runtime completed successfully.

## Scope and limits

No known unresolved defects from these checks. This is a plain-text editor: no printing, Markdown rendering,
rich-text formatting, or AI functionality. Auto-save and recovery occur at intervals; a sudden process or
power failure can lose keystrokes that have not yet reached the next save/snapshot. The executable is an
unsigned local build. No installer, certificate signing, or file-association changes were requested.
