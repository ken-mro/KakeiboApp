---
name: ship-pr
description: Review, push, open and merge a KakeiboApp pull request into master. Use only when the user asks to push, open a PR or merge.
---

# Ship a branch to master

Pushing, opening and merging are done only when the user asks. Network git commands need `-c http.sslBackend=schannel` on this machine.

Branches are named after the GitHub issue they fix, `<issue number>-<issue-title-slug>` (for example `38-the-app-wont-open`), and merged with a merge commit.

1. **Open PRs first:** `gh pr list --state open --json number,title,headRefName,baseRefName,mergeable`.
   If the current branch is built on another open PR's branch (`git merge-base --is-ancestor origin/<that-branch> HEAD`), merge that PR first with `gh pr merge <n> --merge`. The repo has no CI, so an empty `gh pr checks <n>` is normal.
2. **Review when asked** (for example "performance and security"): run independent reviews of `git diff origin/master...HEAD` in parallel, fix what is real, one commit per concern.
3. **Nothing secret goes out:**
   - `git diff --name-only origin/master...HEAD | grep -iE "Constants.cs|^Package/|settings.local.json"` must print nothing. `Repository/Constants.cs` holds the real Syncfusion key in the working tree; never stage it.
   - The guard hook also blocks a push whose commits contain key-like strings.
4. **Build and test:** `dotnet build KakeiboApp.csproj -f net10.0-android`, then `dotnet test --project KakeiboApp.Tests/KakeiboApp.Tests.csproj` (xUnit v3 on Microsoft.Testing.Platform, enabled in `global.json`). All tests must pass.
5. **Conflicts:** `git -c http.sslBackend=schannel fetch origin` then `git merge-tree --write-tree origin/master HEAD`.
6. **Push:** `git -c http.sslBackend=schannel push -u origin <branch>`.
7. **PR:** `gh pr create --base master --head <branch> --title "..." --body-file <scratch>/pr_body.md`. Body sections:
   - `Closes #<issue>` when the branch is for an issue
   - what changed, grouped by area
   - security and performance changes, if any
   - testing (what was checked on the emulator or device, what was not, e.g. iOS)
   - last line `🤖 Generated with [Claude Code](https://claude.com/claude-code)`
8. **Merge:** `gh pr view <n> --json mergeable,mergeStateStatus` should be `MERGEABLE CLEAN`. Then `gh pr merge <n> --merge`.
9. **Report:** the PR URL, what was merged, review findings that were fixed and any that were left (with the reason).
