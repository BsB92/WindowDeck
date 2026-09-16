# WindowDeck contributor instructions

- Read `SPEC.md` before making any functional or architectural decision. It is the source of truth; do not add features that it does not specify.
- WindowDeck is a Windows-only C# application using .NET 10 LTS and WinForms.
- Prefer built-in .NET functionality and documented Win32 APIs. Add third-party dependencies only when genuinely necessary.
- Keep designs simple, changes small and reviewable, and preserve working functionality. Do not rewrite working areas or add abstractions for hypothetical future needs.
- Write all source code, identifiers, comments, documentation comments, and UI text in English. Keep nullable reference types enabled.
- Build after meaningful changes. Fix errors and warnings introduced by a change; never suppress warnings merely to obtain a green build.
- Distinguish successful compilation from runtime validation. When the current environment cannot exercise Windows-specific behavior, report it as **Requires local Windows testing**.
