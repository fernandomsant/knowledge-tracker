# AI Agent Instructions & Repository Guidelines (`agents.md`)

This document defines the technical stack, architectural standards, and workflow rules for AI agents modifying this codebase.

## 1. Technology Stack

* **Core Framework:** React 18+ utilizing functional components and hooks (`useState`, `useMemo`, `useRef`, `useCallback`).
* **Styling Architecture:** Custom modular CSS (`styles.css`) leveraging CSS variables, Flexbox, Grid, and hardware-accelerated transform positioning (`translate3d`) for canvas nodes.
* **Iconography:** `lucide-react` for all interface icons.
* **State Management:** Local component state paired with lifted application state and derived data via `useMemo`.

---

## 2. Component Modularization & File Structure

Monolithic single-file implementations are strictly prohibited. All new code and refactors must follow a clean directory structure under `src/`:

```text
src/
├── components/
│   ├── sidebar/       # Navigation items, workspace selector, subject lists
│   ├── canvas/        # Graph canvas, interactive nodes, SVG connection lines, zoom/pan controls
│   ├── context/       # Side-drawers, inline note editors, and detail views
│   ├── modals/        # Dialog overlays (e.g., subject creation composer)
│   └── shared/        # Reusable presentation units (Stat cards, Note rows)
├── hooks/             # Custom React hooks (e.g., pan/zoom handling, pointer drag logic)
├── utils/             # Helper utilities and ID generation functions
├── styles.css         # Consolidated stylesheet definitions
└── App.jsx            # Top-level shell wiring layout and global state

```

* **Separation of Concerns:** Keep presentation logic, UI containers, and state handlers isolated. Components must focus on a single responsibility.
* **Naming Conventions:** Use PascalCase for component files and directories (e.g., `SubjectNode.jsx`), and camelCase for hooks and utility files.

---

## 3. Git Version Control Workflow (`git add` & `commit`)

Every completed task, feature addition, bug fix, or refactor must be finalized with a proper Git stage and commit sequence:

1. **Stage Modified Files:** Run `git add <file-paths>` to explicitly stage modified or newly created files. Avoid indiscriminate or untargeted staging.
2. **Commit with Conventional Messages:** Immediately execute a `git commit` with a descriptive, structured commit message following conventional commit standards:
* **Format:** `<type>(<scope>): <description>`
* **Types:** `feat`, `fix`, `refactor`, `style`, `chore`, `docs`
* **Example:**
```bash
git add src/components/canvas/Graph.jsx src/hooks/useCanvasPanZoom.js
git commit -m "refactor(canvas): extract pan and zoom logic into a dedicated custom hook"

```

3. **Enforcement Rule:** Never conclude a task implementation sequence or hand off code while leaving modified files unstaged or uncommitted.