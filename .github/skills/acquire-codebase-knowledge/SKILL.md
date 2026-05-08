---
name: acquire-codebase-knowledge
description: 'Use this skill when the user explicitly asks to map, document, or onboard into an existing codebase. Trigger for prompts like "map this codebase", "document this architecture", "onboard me to this repo", or "create codebase docs". Do not trigger for routine feature implementation, bug fixes, or narrow code edits unless the user asks for repository-level discovery.'
license: MIT
compatibility: 'Cross-platform. Requires Python 3.8+ and git. Run scripts/scan.py from the target project root.'
metadata:
  version: "1.3"
---

# Acquire Codebase Knowledge

Produces seven populated documents in `docs/codebase/` covering everything needed to work effectively on the project. Only document what is verifiable from files or terminal output — never infer or assume.

## Output Contract (Required)

Before finishing, all of the following must be true:

1. Exactly these files exist in `docs/codebase/`: `STACK.md`, `STRUCTURE.md`, `ARCHITECTURE.md`, `CONVENTIONS.md`, `INTEGRATIONS.md`, `TESTING.md`, `CONCERNS.md`.
2. Every claim is traceable to source files, config, or terminal output.
3. Unknowns are marked as `[TODO]`; intent-dependent decisions are marked `[ASK USER]`.
4. Every document includes a short "evidence" list with concrete file paths.
5. Final response includes numbered `[ASK USER]` questions and intent-vs-reality divergences.

## Workflow

Copy and track this checklist:

```
- [ ] Phase 1: Run scan, read intent documents
- [ ] Phase 2: Investigate each documentation area
- [ ] Phase 3: Populate all seven docs in docs/codebase/
- [ ] Phase 4: Validate docs, present findings, resolve all [ASK USER] items
```

### Phase 1: Scan and Read Intent

1. Scan the project structure using available tools
2. Search for `PRD`, `TRD`, `README`, `ROADMAP`, `SPEC`, `DESIGN` files and read them
3. Summarise the stated project intent before reading any source code

### Phase 2: Investigate

Use the scan output to answer questions for each of the seven templates covering:
- Stack and dependencies
- Directory structure and entry points
- Architecture layers and patterns
- Conventions (naming, formatting, error handling)
- External integrations
- Testing frameworks and strategies
- Technical debt and concerns

### Phase 3: Populate Templates

Create and fill these docs in `docs/codebase/` in order:

1. **STACK.md** — language, runtime, frameworks, all dependencies
2. **STRUCTURE.md** — directory layout, entry points, key files
3. **ARCHITECTURE.md** — layers, patterns, data flow
4. **CONVENTIONS.md** — naming, formatting, error handling, imports
5. **INTEGRATIONS.md** — external APIs, databases, auth, monitoring
6. **TESTING.md** — frameworks, file organization, mocking strategy
7. **CONCERNS.md** — tech debt, bugs, security risks, perf bottlenecks

Use `[TODO]` for anything that cannot be determined from code. Use `[ASK USER]` where the right answer requires team intent.

### Phase 4: Validate, Repair, Verify

1. Validate each doc — for each non-trivial claim, confirm at least one evidence reference exists
2. Fix any missing or unsupported sections
3. Present a summary of all seven documents, list every `[ASK USER]` item as a numbered question, and highlight any Intent vs. Reality divergences

## Gotchas

- **Monorepos:** Check for `workspaces`, `packages/`, or `apps/` directories. Map each sub-package separately.
- **Outdated README:** Cross-reference with actual file structure before treating any README claim as fact.
- **Generated/compiled output:** Never document patterns from `dist/`, `build/`, `generated/`, `.next/`, or `__pycache__/`.
- **`devDependencies` ≠ production stack:** Only `dependencies` runs in production.
- **High-churn files = fragile areas:** Files appearing most in recent git history have the highest modification rate.

## Anti-Patterns

| ❌ Don't | ✅ Do instead |
|---------|--------------|
| "Uses Clean Architecture with Domain/Data layers." (when no such directories exist) | State only what directory structure actually shows. |
| "This is a Next.js project." (without checking `package.json`) | Check `dependencies` first. State what's actually there. |
| Guess the database from a variable name like `dbUrl` | Check manifest for `pg`, `mysql2`, `mongoose`, `prisma`, etc. |
| Document `dist/` or `build/` naming patterns as conventions | Source files only. |
