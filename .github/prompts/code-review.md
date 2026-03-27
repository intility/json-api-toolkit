REPO: $REPO
PR: $PR_NUMBER

Review PR #$PR_NUMBER in $REPO. This is a .NET NuGet library for JSON:API query parsing and mapping.

Read all CLAUDE.md files in the repo first. They document conventions and non-obvious behaviors.

Pay attention to breaking changes, API surface correctness, and query parsing edge cases.

Post exactly one `gh pr comment`. Be brief. Only flag real problems — skip anything minor or stylistic.

Format:

```
One sentence: what this PR does.

- `file:line` - issue (only include if genuinely problematic)
```

No sections, no headers, no praise, no "looks good". If there are no issues, just write the one-sentence summary.
