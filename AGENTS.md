# SinNotePad project instructions

## Product scope

Build a Windows plain-text editor, not a Windows Notepad clone. Preserve these requirements:

- Create and edit plain-text files; no printing, Markdown rendering, rich text, or AI features.
- Native standard Windows title bar, displaying `SinNotePad - <full path>` or the numbered name before a file has a path.
- Horizontal tabs below the title bar, switchable to a left Document List with a resizable width.
- Text zoom, logical line numbers in the editor gutter, and status-bar line/column information.
- New documents numbered Text 1, Text 2, etc.; persist the counter across launches and offer an explicit reset.
- Optional auto-save folder; create the file immediately and save subsequent edits. Never overwrite an existing file when numbering is reset.
- Preserve text, undo history, selection and scrolling when changing navigation mode.

## Commit rules (adopted from SMILE 2.0)

Every Codex-created commit subject starts exactly with `Sin and Codex:` and a meaningful description.
Nontrivial commits have a detailed body with `Summary:`, `Changes:`, `Validation:`, and `Known limitations:`.
List only checks actually performed. State `None identified.` when appropriate; never claim missing validation passed.
Commit and push coherent validated milestones. Do not commit a broken milestone. Do not amend, rebase, force-push,
or rewrite pushed history without explicit direction. Never discard uncommitted user work.

## Validation and data ownership

Run `Test.ps1` for file-handling tests and real WPF editor integration tests. Inspect the native app after UI changes.
Use isolated `--data-dir` folders under `work/` for testing. Normal user sessions live in LocalAppData.
Do not commit `work/`, app binaries, personal settings, auto-saved documents, or credentials.
Run `Build.ps1 -Package` to produce the self-contained Windows x64 app and project-root shortcut.
