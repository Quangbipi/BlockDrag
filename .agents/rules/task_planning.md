# Task Planning & Design Workflow

When assigned a task, ALWAYS strictly follow this two-phase workflow before implementing or modifying code:

## Phase 1: Planning & Design (with Visual Diagram)
1. **Analyze Requirements & Intent**: Clarify the scope, assumptions, and target behavior.
2. **Architecture & Component Design**: Define data structures, interfaces, component interactions, and dependencies following repository architecture rules (`.agents/rules/architecture.md`).
3. **Mandatory Visual Diagram**: Always include a detailed visual diagram (using Mermaid syntax: flowchart, sequence, state, or class diagram) to give the user the clearest and most intuitive view of the workflow and component interactions.
4. **Save Design Document**: Always write the design specification to its own markdown file at `Docs/designs/YYYY-MM-DD-<task-name>-design.md`.
5. **GATE 1 — STOP for design approval**: Send the design link to the user and end the turn. Do not write the implementation plan until the user explicitly approves the design. If they ask for changes, revise the design and wait for approval again.

## Phase 2: Implementation Plan
1. **Step-by-Step Plan**: Break down the implementation into clear, structured, and manageable steps.
2. **Affected Files & Boundaries**: Explicitly list all files, classes, `.asmdef` modules, and Unity assets/prefabs to create or modify.
3. **Testing & Verification**: Define the verification criteria (automated tests or Unity Editor validation steps).
4. **Save Implementation Plan Document**: Always write the implementation plan to its own separate markdown file at `Docs/plans/YYYY-MM-DD-<task-name>-implementation-plan.md`.
5. **GATE 2 — STOP for plan approval**: Send the plan link to the user and end the turn. Do not modify any code, prefab, scene, asset, or package until the user explicitly approves the plan.

## Phase 3: Implementation
Start only after both gates are approved. Implement the approved plan, run its verification steps, and report the results. If the scope changes during implementation, stop, update the design or plan, and get approval again.

These gates apply to every task, including small fixes and direct instructions such as "fix X" or "change Y". Skip them only when the user explicitly says to skip them for that task.
