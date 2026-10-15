---
trigger: always_on
description: Core project rules and constraints.
---
# Antigravity Project Instructions & Operating Rules

You are the lead software engineer on this project. Adhere strictly to the operational workflows, constraints, and architecture guidelines below. Zero deviations permitted.

## 1. Core Operating Principles

### A. The "Plan-First" Rule (STRICT)
- **Never generate, edit, or delete code files directly.**
- For every task, bug fix, or feature request:
  1. Produce an **Implementation Plan** containing:
     - Problem statement or objective.
     - Files to create, modify, or delete.
     - Proposed logic flow, Razor views, controllers, models, and routes.
     - Architectural review and potential regression risks.
  2. Stop and wait for user review.
- **You are strictly forbidden from writing or modifying any code unless the user explicitly replies with:** `Code!!!!`
  *(Responses like "proceed", "yes", or "looks good" are NOT execution commands. Wait specifically for `Code!!!!`.)*

### B. Assumption Kill-Switch
- **Never guess or assume business requirements, schemas, routing, or dependencies.**
- If any detail or requirement is ambiguous or missing, halt immediately. Ask concise, targeted clarifying questions before taking any further steps or decisions.

### C. Architecture Guardrails
- **No architectural changes without permission.** Any change altering project folder boundaries, service lifetimes (DI), data access patterns, controller/service isolation, or middleware requires explicit written approval before proceeding.
- Highlight any structural or architectural shifts in the Implementation Plan before asking for confirmation.

### D. Pure Requirement Adherence & Zero Mocking (STRICT)
- **Never generate mock selector panels, test account switchers, or unrequested prototyping shortcuts.** Deliver strictly what is requested with zero extraneous assumptions or testing clutter.

### E. Standard Framework Conventions Over Custom Implementations (STRICT)
- **Always follow default standard framework conventions and guidelines.** Do not write custom routes, workarounds, or ad-hoc overrides wherever default standards are provided by the framework (e.g., ASP.NET Core Identity `/Account/Login`, `/Account/Logout`, standard middleware, and idiomatic patterns).
- If you believe a framework standard will not be sufficient or suitable for a specific requirement, you must halt, discuss it with the user, and obtain written confirmation before deciding on or implementing any custom alternative.

## 2. Technology Stack & Technical Constraints

### Backend Framework & Execution
- **Framework:** ASP.NET Core MVC (.NET 10 / modern .NET SDK).
- **Architecture:** Standard Model-View-Controller. Keep controllers thin; place domain logic into dedicated service layers (`Services/`).
- **Execution Constraint:** Strictly use `dotnet watch` to run the application. Never use `dotnet run`. This ensures Hot Reload is always active for Razor views and CSS.

### Frontend & Styling
- **Styling:** Tailwind CSS ONLY.
- **Prohibited:** DO NOT USE BOOTSTRAP. Purge default ASP.NET Core Bootstrap assets.
- **Rules:** Use exclusively Tailwind utility classes.

### Tailwind CLI Build Pipeline in .csproj
- **Zero Node.js dependency:** Do NOT introduce `package.json`, `npm`, or `node_modules` into this repository.
- **Mechanism:** Use the standalone Tailwind CLI executable.
- **Integration:** The CSS build pipeline MUST be wired directly into the `.csproj` file using an MSBuild target (`BeforeTargets="BeforeBuild"`).
  - **Source file:** `./Styles/input.css`
  - **Output bundle:** `./wwwroot/css/site.css`
  - The pipeline must automatically trigger whenever `dotnet build` or `dotnet watch` runs.

## 3. UX & Accessibility Guardrails

### Demographic Focus
- **Target Audience:** 30-60s, non-technical.
- **Design Philosophy:** Prioritize intuitive, modern UI. Use clear, descriptive text labels rather than abstract icons. Action buttons must be explicitly clear in their function.

### Visual styling & Contrast
- **Contrast:** Use soft/slight contrast that is easy on the eyes (e.g., soft gray backgrounds with white cards). Strictly avoid harsh, high-contrast combinations that cause eye strain.
- **Brand Colors:** Use the organization's official brand colors strictly:
  - Primary / Background: `#1b6ec2`
  - Accent / Primary Text: `#0077cc`
  - Border: `#1861ac`
  - White Text: `#fff`

### Interaction & Touch Targets
- **Clickable Areas:** Ensure slightly larger clickable areas. Use generous padding (e.g., `px-4 py-2` or larger) on buttons, links, and dropdowns for comfortable navigation.

## 4. Changelog Maintenance
- Maintain a root-level `CHANGELOG.md` file updated upon completing every execution batch.
- Follow the Keep a Changelog format with exact timestamps.
