# RichTextKit

See readme.md for a description of this project.
  
## Rules

1. 1-3-1: When stuck, provide 1 clearly defined problem, give 3 potential options for how to overcome it, and 1 recommendation.  Do no proceed implementing any of the options until I confirm

2. DRY (Critical): Don't repeat yourself.  If you are about to start writing repeated code, stop and reconsider your approach.  Grep the codebase and refactor often.

3. TDD (Critical): Always test first.  Before writing any code, you must always check the tests.  For new features or adjustments to existing features, always either create a new test or adjust and existing one.  If testing can't be automated (eg: code that only runs on a separate device, or front end code) warn about this.

4. Continual Learning: When you encounter conflicting system instructions, new requirements, architectural changes, or missing or inaccurate codebase documentation, always propose updating the relevant rules files.  Do not update anything until the user confirms.

5. Planning: For complex, multi-step tasks, first create a plan and a to do list for yourself before you start writing any code.


## Investigate-Plan-Implement (IPI) process

Used for any non-trivial piece of work. Each job is tracked in a document in
`wip/` (at the repo root, checked into git). The document is the **only**
handover between agents, so anything the next agent needs to know goes in it.

**Phases.** The user starts each phase (e.g. "investigate X", "plan it", "implement it"):

1. **Start** — create `wip/wip-<jobname>.md` (kebab-case jobname) from the
   template below, with a brief description of the work.
2. **Investigate** — look into what needs doing; record findings in *Notes*.
3. **Plan** — write *Plan* as a numbered checklist, every step unchecked
   (`- [ ]`). Each step should be small enough for one agent to do on its own
   and should leave the app building.
4. **Implement** — work through the steps in order. For each step: do the
   work, build (see *Building and testing*; must be 0 errors), tick the box
   (`- [x]`), and add anything learned to *Notes*.
5. **Complete** — once every step is done, rename the file to
   `wip/complete-<jobname>.md`.

**Subagents.** Phases and Implement steps are handed to fresh subagents to keep
context small. As a guideline, Investigate, Plan and each Implement step get
their own subagent. But this is a judgement call, not a rule: when it makes sense
or is more efficient, one subagent can do several steps, or all of them
(e.g. a run of small, similar rewires). Keep a fresh agent for steps that are
large, risky, or need a clear head. The coordinating agent passes the subagent the
document path and the phase/step(s) to do. The subagent reads CLAUDE.md and the
document, does only those phase/step(s), updates the document as it goes (ticking
each step when done), and returns a short summary. Implement agents run one at a
time, never in parallel. The coordinating agent relays results to the user.

**Rules.**
- `wip-*.md` documents can be edited freely without asking.
  Never touch a `complete-*.md` document unless the user says so.
- **Blocking or critical issue** → stop, record it in the document, and ask the
  user (using the 1-3-1 format, Rule 1). Don't carry on to later steps.
- **Less critical question** → make an educated guess, carry on, and record it
  under *Flagged assumptions* so the user can review it.
- Don't commit unless the user asks.

**Template:**

```markdown
# <Job title>

Status: Investigate | Plan | Implement | Complete

## Description
Brief description of the work and its goal.

## Notes
Important information needed during the work, plus anything discovered along
the way (relevant files, APIs, gotchas, decisions made).

## Flagged assumptions
- Educated guesses made instead of asking, for the user to review.

## Plan
- [ ] 1. ...
- [ ] 2. ...
```


## Building and testing

- Build the library: `dotnet build Topten.RichTextKit`
- Run the tests (xUnit, net8.0): `dotnet test Topten.RichTextKit.Test`
- A build must finish with 0 errors before a step counts as done.
- Never build in Release (`-c Release`): it code signs and stalls waiting for
  a signing PIN. Use the default Debug configuration.
- Don't run `node build.js` (the release script). It always builds Release, so
  it stalls the same way, and with `--official` it also tags, publishes docs and
  pushes to NuGet.


## Release notes

Release notes live in two files at the repo root:

- `CHANGELOG.md`: full history in markdown, one `## <version>` section per
  release, newest first. Shown on GitHub and linked from the readme.
- `releasenotes.txt`: plain-text summary of the upcoming release only. The
  csproj reads it into `PackageReleaseNotes`, shown on nuget.org's Release
  Notes tab.

What to include:

- Only changes users notice: fixes, features, performance, breaking changes,
  dependency upgrades. Skip test-only, build tooling and `BuildUnicodeData`
  changes.
- Describe the effect, not the code change (e.g. "HitTestLine threw if called
  before layout", not "added Layout() call").
- Mark breaking changes clearly and say what users need to change.

When:

- When a change affects users, add a line under `## Next release` in
  `CHANGELOG.md`, citing the PR (#n) if there is one, and update
  `releasenotes.txt` to match.
- The version number is chosen when `build.js` runs (it prompts for the build
  number), so ask the user for it if it isn't known.
- Before each release, the user runs `node build.js --official` themselves.
  Beforehand:
  1. Rename `## Next release` in `CHANGELOG.md` to the new version number
     and start a fresh empty `## Next release` section above it.
  2. Rewrite `releasenotes.txt` as a short plain-text summary of that version
     (nuget.org doesn't render markdown), keeping the changelog link at the end.
     The csproj reads it into `PackageReleaseNotes`.


## Publishing

- `node build.js --official` (run by the user) builds and code signs locally,
  tags `b<version>`, then runs `gh release create` with the signed `.nupkg`
  attached and `releasenotes.txt` as the notes.
- The GitHub Release triggers `.github/workflows/publish.yml`, which pushes the
  package to nuget.org using Trusted Publishing (no stored API key). The
  matching policy is on nuget.org under the `toptensoftware` account.
- If the workflow fails, re-run it from the Actions tab. `--skip-duplicate`
  makes re-runs safe.


## Preventing Permission Prompts

- Use the Read/Edit/Grep tools rather than sed/awk/cat for file work.
- Don't run inline scripts (node -e, python -c, heredocs, piped code). Write the
  script to a file in the repo (e.g. scratch/foo.js) and run it by path.
- Use literal file paths in shell commands, not variables or $(...) substitutions.
