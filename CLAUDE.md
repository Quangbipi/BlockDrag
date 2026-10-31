@AGENTS.md

Project skills live in `.agents/skills/` (exposed to Claude Code via `.claude/skills/`).
If `.claude/skills` is missing, create it: `mkdir -p .claude && ln -s ../.agents/skills .claude/skills`
