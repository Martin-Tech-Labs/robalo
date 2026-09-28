# AI Agent Best Practices Checklist

To maintain code quality and project stability, all AI coding assistants should follow these guidelines:

## 🚀 Development Workflow

- [ ] **Implementation Plan Required (ALWAYS FIRST)**: ALWAYS present a detailed implementation plan before making any code changes, commits, or branch operations.
    - Describe the files that will be created, modified, or deleted.
    - Outline the specific changes to be made in each file.
    - Explain the rationale and approach.
    - Wait for user feedback and approval before proceeding with implementation.
    - Only proceed with implementation after receiving explicit approval or a clear "go ahead" signal.
    - This applies to ALL requests, including those on non-main branches.
    - **Exception for trivial changes**: Simple, single-file edits (documentation updates, typo fixes, config clarifications) that don't require complex planning can proceed without a full implementation plan.
- [ ] **Branching Strategy**: ALWAYS check the current branch before starting work.
    - Create a new branch ONLY if you are currently on `main`.
    - Branch names MUST be prefixed with `feature/` or `bug/` (e.g., `feature/add-login`).
    - **Never** create a new branch or "sub-branch" if the current branch is not `main`. Continue work in the existing branch.
    - **Do NOT switch branches or checkout main to circumvent this rule.** If you are not on main, present the implementation plan first and continue work in the existing branch.
- [ ] **No Direct Commits**: Never commit directly to the `main` branch.
- [ ] **Pull Requests**: Raise a Pull Request (PR) to `main` using the GitHub CLI (`gh pr create`) only after finishing the task.
    - **Check for existing PRs**: Only create a new PR if one does not already exist for the current branch.
    - **Return PR link in result**: Always include the PR URL in the final result so the user can easily navigate to it.
    - **Update existing PR descriptions**: If a PR already exists for the current branch, do NOT create a new PR. Instead, update the PR description using `gh pr edit <pr-number> --body "<new-description>"`.
    - Follow the [GitHub flow](https://docs.github.com/en/get-started/using-git/github-flow) principles.
    - MUST follow the [pull_request_template.md](file:///Users/marty/git/snake-arena-live/.github/pull_request_template.md) when raising a PR.
- [ ] **Publish Branch**: Make sure to publish the created branch to GitHub (`git push -u origin <branch-name>`) after committing changes.
- [ ] **Run Tests Before Finalizing**: Always run the test suite before declaring a task complete.
- [ ] **Explicit Permission**: NEVER make code changes or updates to files unless specifically asked by the user. 
    - Once an **Implementation Plan** is approved, proceed with implementation immediately. Do NOT ask for additional confirmation on reviewing file changes during implementation unless a major design shift occurs.
- [ ] **Commit Regularly**: Make small, logical commits after each reasonable step rather than one giant commit at the end.
    - If a single implementation plan covers several logical components, consider making multiple separate commits where it makes sense (on a case-by-case basis).
- [ ] **Use Imperative Commits**: Write commit messages in the imperative mood (e.g., "Add sound utility" instead of "Added sound utility").
- [ ] **Verify Build**: Ensure the development server (`npm run dev`) starts without warnings or errors.

## 📁 Project Structure Awareness

- [ ] **Frontend context**: Most commands (npm, vitest, playwright) must be run from the `/frontend` directory.
- [ ] **Source vs Root**: Be careful not to mix root-level config with frontend-specific code.

## 🛠 Coding Standards

- [ ] **Synthesized Assets**: When adding simple assets (like UI sounds), prefer browser-native APIs (e.g., Web Audio API) over external binary files to keep the repo lightweight.
- [ ] **CSS Hygiene**: Keep all `@import` statements at the very top of CSS files to avoid build warnings.
- [ ] **Testing Protocol**:
    - Use `src/test/setup.ts` to add global mocks (like `localStorage`).
    - Prefer `vitest` for logic/component tests and `playwright` for E2E flows.
- [ ] **Error Handling**: Use the existing `toast` system for user-facing feedback.
- [ ] **Security (OWASP)**:
    - Never store plaintext passwords; always hash with a strong, adaptive algorithm before persistence.
    - Never commit secrets (API keys, tokens, credentials, private keys) to the repo. Use env vars and `.env` files (gitignored) or a secrets manager.
    - Remove sensitive data from logs, fixtures, and sample payloads unless explicitly required and clearly marked as non-production.

## 📋 Documentation

- [ ] **Update artifacts**: Maintain the `implementation_plan.md`, `task.md`, and `walkthrough.md` in the `.gemini/antigravity/brain` directory to keep the user informed.
- [ ] **Update Documentation**: Always update the root `README.md` or subfolder READMEs when adding new commands, tests, or setup steps.

## 🐍 Backend / Python (uv)

- [ ] **Dependency Management**: Use `uv` for backend python management.
    ```bash
    # Sync dependencies from lockfile
    uv sync

    # Add a new package
    uv add <PACKAGE-NAME>

    # Run Python files
    uv run python <PYTHON-FILE>
    ```
