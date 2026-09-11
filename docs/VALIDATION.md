# Validation

Validated on Windows 11 x64 with .NET SDK 10.0.400 on September 11, 2026.

## Final checks

- Release solution build: passed, zero warnings and zero errors.
- Self-contained Windows x64 package: passed; runtime bundled in one executable.
- Core tests: **51 passed, 0 failed**.
- WPF editor integration tests: **78 passed**.
- Source whitespace formatting and Git whitespace checks: passed.

Core tests exercise Unicode/encoding and line-ending round trips, unchanged mixed endings, external-edit
conflicts, read-only files, rejected lossy encoding conversions, literal search/replacement, numbered names,
counter persistence, immediate auto-save file creation, numbering collisions, session backup recovery,
caret-offset conversion, binary rejection, and a large-text save/reopen.
The 1.1 update also covers all six Date/Time formats, English weekday/month names and lowercase am/pm,
midnight/noon/leap day, settings defaults and persistence, rename byte preservation, occupied destinations,
invalid Windows names, case-only rename, a missing source, and avoiding a just-deleted numbered filename.

The integration tests use the actual WPF editor and exercise menu rendering, native-title-bar configuration,
full-path titles, numbered tabs, independent documents, line numbers and caret positions, zoom, list/tabs
toggling, sidebar sizing and width persistence, undo/redo retention, find/replace, reordered tabs, long-document
scrolling, session restoration, selection restoration across CRLF normalization, physical auto-save files,
auto-save edits, write conflicts, and resetting the document sequence.
New checks verify menu-before-tabs geometry, removal of document close buttons, all five context commands,
Date/Time insertion/selection/undo/repeat, clipboard path copying (restoring the original clipboard afterward),
File Explorer arguments, rename across windows with retained edits and undo, paused auto-save during Delete,
cancellation/failure recovery, actual Windows Recycle Bin deletion of an isolated fixture, removal of all open
references, and prevention of file recreation by auto-save. Line-number checks inspect the rendered glyph
drawings after individual edits, a newline, undo, toggling, horizontal scrolling and wrapping; new documents
also inherit the line-number setting.
Deletion also checks the current open references after modal confirmation, retaining documents saved to a
different path during the dialog and closing any newly opened references to the deleted file.

## Native interface observations

- Launched the packaged executable successfully.
- Observed the standard Windows title bar with `Sin - Notepad - <full path>`, menus above tabs, aligned line numbers, and status bar.
- Used Ctrl+Shift+L to switch to Document List; confirmed text and caret remained in place.
- Dragged the divider from roughly 250 to 350 pixels; the list and editor resized correctly.
- Used Ctrl+Plus; text and gutter enlarged together and the status changed from 100% to 110%.
- Opened Settings and inspected the auto-save folder, Browse, persistent next number and Reset to 1 controls.
- Cancelled Settings without changing preferences.
- Opened File and verified plain-text commands, with no printing or Markdown commands.
- In an isolated 1.1 profile, typed into the native editor and observed that all three line numbers stayed visible.
- Inspected the five-command context menu in both tabs and Document List, with document close X buttons removed.
- Opened Rename and checked that the base name is selected while the extension remains visible; cancelled without changing the fixture.
- Inspected all six live Date/Time examples in the submenu.
- Used View → Line numbers and observed the gutter collapse while the status bar retained line/column data.

The initial compact-menu clipping defect and the CRLF caret-restoration mismatch were corrected and covered
by focused regression checks before the final build. The SDK formatting host initially followed a stale
DOTNET_ROOT; rerunning with the installed system runtime completed successfully.
The 1.1 gutter fix schedules a redraw after WPF finishes the text layout, rather than relying solely on
the early TextChanged render pass. A test setup initially edited a newly selected editor before it loaded;
waiting for its real Loaded/layout pass corrected that undo-test setup. The full suite subsequently passed.

## Scope and limits

No known unresolved defects from these checks. This is a plain-text editor: no printing, Markdown rendering,
rich-text formatting, or AI functionality. Auto-save and recovery occur at intervals; a sudden process or
power failure can lose keystrokes that have not yet reached the next save/snapshot. The executable is an
unsigned local build. No installer, certificate signing, or file-association changes were requested.
