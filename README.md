# SinNotePad

A small native Windows plain-text editor with tabs and a resizable Document List.

## Run

Double-click **SinNotePad.lnk** in the project folder, or run **app\Sin - Notepad.exe**.
The packaged Windows x64 app includes its .NET runtime. No installation or administrator access is required.

## Editing

- Create, open, edit and save plain-text files.
- New documents are named **Text 1**, **Text 2**, and so on. Typing does not rename them.
- A standard Windows title bar shows **SinNotePad - full file path**; unsaved documents show their numbered name.
- Horizontal tabs sit below the title bar. Choose **View → Document List** or press **Ctrl+Shift+L** to switch to a vertical list on the left. Drag its right edge to resize it. The width is remembered.
- Select documents in either layout; drag tabs or list rows to reorder them. The document's editor and undo stack stay intact when switching layouts.
- Logical line numbers appear to the left of the text. Wrapped continuations do not receive extra line numbers.
- The status bar shows line, column, total lines, character count, selection count, zoom, line endings and encoding.
- Zoom changes only how large text appears. Files contain plain text, without formatting.

## Auto-save and numbering

Open **Settings** using the gear button. Choose or enter an **Auto-save folder**, then click **Save**.
Every new document immediately becomes a physical file such as `Text 27.txt` in that folder. Edits are saved
after approximately 0.8 seconds without typing, and pending edits are flushed when the window closes.
The status bar shows **Saving…**, **Saved**, or **Auto-save paused**. Hover over a paused status for the reason;
use Save / Save as to resolve a write problem. A file changed outside the app is never silently overwritten.

The next document number persists across app launches. **Reset to 1** takes effect when Settings is saved.
Existing filenames are skipped: if `Text 1.txt` already exists, the app tries `Text 2.txt`, and so on.
Resetting never overwrites an existing file. Leaving the folder empty enables manual saving for future new
documents. Existing auto-saved documents continue saving to their own paths. Choosing a new folder does not
move or rename any already-open document.

## Keyboard shortcuts

| Action | Shortcut |
| --- | --- |
| New document | Ctrl+N or Ctrl+T |
| New window | Ctrl+Shift+N |
| Open | Ctrl+O |
| Save / Save as / Save all | Ctrl+S / Ctrl+Shift+S / Ctrl+Alt+S |
| Close tab / window | Ctrl+W / Ctrl+Shift+W |
| Switch tabs / Document List | Ctrl+Shift+L |
| Next / previous document | Ctrl+Tab / Ctrl+Shift+Tab |
| Zoom in / out | Ctrl+Plus / Ctrl+Minus or Ctrl+mouse wheel |
| Reset zoom to 100% | Ctrl+0 |
| Find / replace | Ctrl+F / Ctrl+H |
| Next / previous match | F3 / Shift+F3 |
| Go to line | Ctrl+G |
| Undo / redo | Ctrl+Z / Ctrl+Y |
| Insert time and date | F5 |
| Close search | Escape |

## Data and recovery

Settings and recoverable sessions are stored in `%LOCALAPPDATA%\Sin - Notepad`.
By default, closing the app preserves open documents, including unsaved text, and restores them next time.
Closing an individual modified tab asks whether to save, discard, or cancel. Disable session restoration
in Settings to be prompted about unsaved documents when closing a window.

Files are written using a temporary file and an atomic replace. UTF-8, UTF-8 with BOM, UTF-16, UTF-32 BOMs,
and ANSI input are supported; the original encoding and line-ending convention are retained. New files use
UTF-8. Unchanged mixed line endings are preserved byte-for-byte. Encoding choices and line endings are
available from their status-bar buttons. An encoding conversion that cannot represent the text is rejected.

Session snapshots are saved every two seconds when needed, with a previous-version backup. This is recovery
for ordinary app restarts and does not replace backups of important files. Very recent unsaved keystrokes
can be lost in a sudden process or power failure before the next snapshot or auto-save completes.

## Build and test

Requires Windows and the .NET 10 SDK. There are no external NuGet package dependencies.

```powershell
.\Build.ps1 -Package
.\Test.ps1
```

`Test.ps1` runs encoding/file-safety/search/numbering/session tests and integration checks against the real WPF
editor. It uses disposable data under `work/`, leaving your normal documents and settings untouched.

The solution is `SinNotepad.sln`. `SinNotepad.Core` owns files, documents, numbering, search and persistence.
`SinNotepad` owns the WPF interface, editor gutter, settings, auto-save scheduling and single-instance routing.
`--data-dir "D:\some\test-folder"` starts an isolated profile for development.

This project is a standalone plain-text editor. It is not affiliated with Microsoft, and it deliberately has
no printing, Markdown renderer, rich-text formatting or AI services.
