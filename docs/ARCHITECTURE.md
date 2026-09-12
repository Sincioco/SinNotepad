# Architecture

Sin Notepad is one native WPF executable with a small dependency-free core library. Keep behavior with its
current owner and add a new boundary only when substantial logic has no cohesive existing home.

## Module map

| Module | Responsibility and state owner | Allowed dependencies |
| --- | --- | --- |
| `App.xaml.cs` | Startup, process lifetime, single-instance routing, application settings/session lifecycle | WPF, `MainWindow`, `FileAssociations`, core persistence |
| `MainWindow.xaml` / `MainWindow.xaml.cs` | Window composition, active-document state, commands, navigation, status, search coordination | WPF, `EditorView`, dialogs, focused platform modules, core models |
| `FileActions.cs` | Rename, Delete, Copy path, Open folder and Close behavior for document rows | `MainWindow`, Windows shell/file APIs, core file behavior |
| `EditorView.cs` | Text editor ownership, delayed document synchronization, caret state and retained line-number rendering | WPF, core `Document` and text normalization |
| `Dialogs.cs` | Application-owned modal dialogs and their local input state | WPF, core settings |
| `FileAssociations.cs` | Per-user Windows registration and default-app settings navigation | Windows registry and shell APIs |
| `SinNotepad.Core` | Documents, text encoding/files, settings, sessions, persistence, search and date/time formatting | .NET runtime and file APIs; no WPF/application reference |
| `SinNotepad.Tests` | Fast core behavior checks | `SinNotepad.Core` only |
| `UiSelfTest.cs` | Native WPF integration and regression checks | Application UI and disposable `--data-dir` state |

Dependencies flow from the WPF application and tests into `SinNotepad.Core`. The core must never reference the
application or WPF. Platform-specific registry, shell and visual rendering behavior remains in the application.

## Enforced growth limits

`Check-Architecture.ps1` runs from `Test.ps1` and checks handwritten `.cs` and `.xaml` files under `src`, test
`.cs` files, and root PowerShell scripts. Generated `bin` and `obj` content is the only source exclusion.

| Classification | Review warning | Failure |
| --- | ---: | ---: |
| Entry/composition file (`App.xaml.cs`) | More than 200 lines | More than 300 lines |
| Ordinary production, UI, test or validation module | More than 500 lines | More than 800 lines |

These limits are review tripwires. They do not justify compressed formatting, removed comments, arbitrary splits,
or unrelated responsibility. Function length and semantic responsibility remain manual review items because the
checker intentionally does not pretend that a text heuristic is a C# architecture analyzer.

## Reviewed legacy baseline

`src/SinNotepad/MainWindow.xaml.cs` is an existing 584-line window-command coordinator. It is above the normal
500-line review warning and has an enforced no-growth baseline of **584 lines**. The next substantial feature that
touches this file must reuse a focused owner such as `FileActions`, `EditorView`, `Dialogs`, or `FileAssociations`,
or make the smallest behavior-specific extraction needed. A rename must preserve this baseline's history. If an
approved extraction reduces the file, tighten the baseline rather than filling the freed space with unrelated work.

`UiSelfTest.cs` currently has 360 lines and remains below its 500-line warning. When a distinct new test area would
push it toward that trigger, create one cohesive fixture rather than an arbitrary line-count split. `Documents.cs`
contains several related core data/file types in 213 lines; split only when one responsibility grows enough to need
an independent owner.

## Validation and maintenance

Run `Test.ps1` for the architecture checker, Release build, core tests and native WPF integration tests. Run
`Check-Architecture.ps1 -SelfTest` directly when changing checker behavior. The self-test covers warning, hard-limit,
and legacy-baseline outcomes. The checker also enforces the one-way project dependency and uses a clearly reported
namespace text check to prevent WPF dependencies in the core.

There is currently no repository CI configuration, so the guardrail is enforced through the normal local validation
path rather than claimed as a remote gate. Update this map when ownership boundaries change. Never change thresholds,
baselines, exclusions or dependency rules merely to make validation pass.
