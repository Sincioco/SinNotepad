# SinNotePad project instructions

## Product scope

Build a Windows plain-text editor, not a Windows Notepad clone. Preserve these requirements:

- Create and edit plain-text files; no printing, Markdown rendering, rich text, or AI features.
- Native standard Windows title bar, displaying `Sin - Notepad - <filename>` (including extension, without the folder path), or the numbered name before a file has a path.
- Horizontal tabs below the menu bar, switchable to a left Document List with a resizable width. No document close X buttons in either layout.
- Both navigation layouts offer Rename, Delete, Copy full path, Open containing folder, and Close through a right-click menu. Confirm deletion, use the Windows Recycle Bin, and preserve state on cancellation or failure.
- Text zoom, logical line numbers in the editor gutter, and status-bar line/column information. Provide a persistent View → Line numbers toggle; numbers must remain visible without flickering while typing when enabled. Retain valid drawings during text layout and test intermediate rendered frames, not just the final state after edits.
- Keep typing responsive in long documents. Coalesce full-text normalization, line-index rebuilding, search recounting, and session persistence until a brief typing pause; saves, tab changes, and shutdown must synchronize pending text first.
- Keep launch responsive. Show the restored editor before nonessential shell registration maintenance, skip registration when the version and executable path are unchanged, and package with the measured fast self-contained settings.
- Use the text insertion cursor over editable text and the standard Windows arrow cursor over the editor scrollbars.
- Edit → Date/Time offers the six requested English date/time formats. F5 repeats the last choice.
- Ctrl+D always inserts the long English date/time (for example, Friday, September 11, 2026 at 7:08 pm) without changing the F5 preference. Ctrl+L inserts exactly 80 underscores; Ctrl+Shift+L still toggles Document List.
- Ctrl+I inserts the long English date/time followed by a newline, exactly 80 underscores, and two trailing line breaks as one undoable edit, preserving the F5 preference.
- New documents numbered Text 1, Text 2, etc.; persist the counter across launches and offer an explicit reset.
- Optional auto-save folder; create the file immediately and save subsequent edits. Never overwrite an existing file when numbering is reset.
- Auto-save all changed documents that already have a file path when the app closes, enabled by default and configurable in Settings. Keep pathless documents in session recovery or prompt for a path when session restoration is disabled.
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

## Architecture and controlled growth

Keep each behavior understandable, testable, and changeable within a small, coherent set of modules. Minimize
avoidable future refactoring and agent context without sacrificing correctness, performance, or readability.

Before substantial implementation:

- Read these instructions, relevant architecture documentation, implementation, callers, and tests.
- Identify the responsibility, state owner, appropriate module, dependencies, and validation.
- Reuse an existing cohesive module when it is the correct owner. Establish a focused boundary before adding substantial logic that does not belong in an existing module.
- Scale planning to the task; do not write a design document for a trivial change.

Structure and growth:

- Keep bootstrap files limited to startup, wiring, lifecycle coordination, delegation, and shutdown. Do not place feature algorithms, detailed UI behavior, persistence or rendering implementations in the entry point.
- Give each module one coherent responsibility, explicit state ownership, and a small public surface. Pass only the required state and dependencies.
- Do not introduce dependency cycles, reverse dependencies into entry points, broad mutable globals, generic dumping grounds, giant replacement controllers, or tightly coupled file families.
- Prefer the simplest adequate design. Avoid speculative frameworks, unnecessary dependencies, excessive tiny files, and interfaces without a concrete purpose.
- Follow repository file-size and complexity guardrails. Treat thresholds as review triggers rather than architectural grades, and never compress code, remove useful comments, or split files arbitrarily to satisfy line counts.
- Respect reviewed no-growth baselines for oversized legacy files. Never silently raise limits, reset baselines, expand exclusions, disable checks, or create exceptions.
- Add substantial behavior to its proper owner and make only the smallest local extraction needed. Preserve behavior and public formats unless a change is authorized.
- Add behavior-capturing tests before risky extraction. Keep structural changes distinguishable from behavior changes, and record unrelated architectural debt instead of starting a repository-wide refactor.
- Verify actual language and runtime capabilities. Report limitations that prevent sound modularity or state ownership instead of centralizing state or expanding language/compiler requirements without authorization.

At completion, run relevant tests and available architecture checks. Review the diff for new coupling, hidden state,
unnecessary churn, and changed-file growth. Report ownership decisions, growth, checks, actual validation, exceptions,
and remaining risks. A passing build or smaller entry point alone does not prove that the architecture is healthy.
