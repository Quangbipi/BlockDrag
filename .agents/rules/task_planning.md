# Task Planning & Design Workflow

When assigned a task, ALWAYS strictly follow this two-phase workflow before implementing or modifying code:

## Phase 1: Planning & Design (with Visual Diagram)
1. **Analyze Requirements & Intent**: Clarify the scope, assumptions, and target behavior.
2. **Architecture & Component Design**: Define data structures, interfaces, component interactions, and dependencies following repository architecture rules (`.agents/rules/architecture.md`).
3. **Mandatory Visual Diagram**: Always include a detailed visual diagram (using Mermaid syntax: flowchart, sequence, state, or class diagram) to give the user the clearest and most intuitive view of the workflow and component interactions.
4. **Save Design Document**: Always write the design specification to its own markdown file at `Docs/designs/YYYY-MM-DD-<task-name>-design.md`.

## Phase 2: Implementation Plan
1. **Step-by-Step Plan**: Break down the implementation into clear, structured, and manageable steps.
2. **Affected Files & Boundaries**: Explicitly list all files, classes, `.asmdef` modules, and Unity assets/prefabs to create or modify.
3. **Testing & Verification**: Define the verification criteria (automated tests or Unity Editor validation steps).
4. **Save Implementation Plan Document**: Always write the implementation plan to its own separate markdown file at `Docs/plans/YYYY-MM-DD-<task-name>-implementation-plan.md`.
5. **Alignment**: Present the design and plan links for user review before proceeding with implementation.
