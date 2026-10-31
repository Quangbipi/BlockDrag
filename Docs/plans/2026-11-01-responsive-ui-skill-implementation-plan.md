# Responsive UI skill — Implementation Plan

Design: [2026-11-01-responsive-ui-skill-design.md](../designs/2026-11-01-responsive-ui-skill-design.md)
Branch: `feature/block-sprite-palette`

## Impacted files

| File | Change |
|---|---|
| `.agents/skills/unity-responsive-ui/SKILL.md` | **new** |
| `.agents/skills/unity-responsive-ui/render-snippets.md` | **new** |
| `.claude/skills` | **new local symlink** → `../.agents/skills` (git-ignored, not committed) |
| `CLAUDE.md` | add the command to recreate the symlink |

No code, prefab, asset, or package changes.

## Steps

### 1. `SKILL.md`

- Frontmatter: `name: unity-responsive-ui`, description as in the design (starts with "Use when…", trigger conditions only).
- Sections, kept short (target < 200 lines):
  1. Overview + "works inside Gate 1 / Gate 2".
  2. Project facts table (scaler, ×3 rule, canvas sizes per device, asset folder, fonts, `SafeAreaFitter`).
  3. Workflow checklist (measure → inventory → zone → design/plan → build → render → verify) with the Mermaid flow from the design.
  4. Layout rules (zones, anchors, safe area, `AspectRatioFitter`, 9-slice, layout groups, TMP settings, banner band).
  5. Prefab-edit template (short C# block).
  6. Verification checklist.
  7. Pitfalls table (symptom → cause → fix).
- Reference `HomeCanvas.prefab` + its design doc as the worked example.

### 2. `render-snippets.md`

The tested `execute_code` scripts from the HomeCanvas work, cleaned up and parameterised:

- Measure mockup: color sampling + per-row extents.
- Generate simple sprites (tile, rounded 9-slice panel) + importer setup.
- Render a prefab at N resolutions in a preview scene → PNG in the scratchpad.
- Play mode: find canvas, click a button via `ExecuteEvents`, `ScreenCapture`.

### 3. Link skills

```sh
mkdir -p .claude && ln -s ../.agents/skills .claude/skills
```

### 4. `CLAUDE.md`

Add under the existing skills line:

```
If `.claude/skills` is missing, create it: `mkdir -p .claude && ln -s ../.agents/skills .claude/skills`
```

## Verification

1. `ls .claude/skills/` lists every folder in `.agents/skills/`, and `.claude/skills/unity-responsive-ui/SKILL.md` resolves.
2. `git status` does not show `.claude/skills` (ignored); only the skill files, `CLAUDE.md`, and the docs show up.
3. Frontmatter check: `name` and `description` present, valid YAML.
4. Skill discovery: Claude Code loads skills at session start, so the new skills are confirmed in a **new session** (list shows `unity-responsive-ui`). I will tell you this if it cannot be confirmed in the current one.
5. Snippets: re-run the render snippet from `render-snippets.md` once against `HomeCanvas.prefab` to prove it works as written.
