# Contributing to NetCid

Thanks for helping improve `NetCid`. This guide captures contribution patterns used by successful open source projects: small focused changes, reproducible verification, and clear review context.

## Project Priorities

Keep these priorities in mind when proposing changes:

- Spec correctness first (`multiformats/cid` behavior and compatibility).
- Stable, predictable public APIs.
- Security-conscious input handling and validation.
- High signal changes with minimal unrelated churn.

## Ways to Contribute

- Fix bugs or edge-case parsing/encoding issues.
- Add or improve tests (unit and integration).
- Improve docs and examples in `README.md` and `examples/README.md`.
- Improve developer tooling, CI reliability, or release quality.

## Before You Start

1. Check existing issues and PRs to avoid duplicated work.
2. For larger changes (API shape, behavioral shifts, architecture), open an issue first and align on direction.
3. Prefer one logical change per PR. Small PRs get reviewed and merged faster.

## Local Setup

Prerequisite: .NET SDK compatible with the repo (`net10.0`).

```bash
dotnet restore NetCid.sln
dotnet build NetCid.sln -c Release
dotnet test NetCid.Tests/NetCid.Tests.csproj -c Release
dotnet test NetCid.IntegrationTests/NetCid.IntegrationTests.csproj -c Release
```

If your change affects examples, also run:

```bash
dotnet run --project examples/cid-interface/CidInterfaceExample.csproj -c Release
dotnet run --project examples/multicodec-interface/MulticodecInterfaceExample.csproj -c Release
dotnet run --project examples/multihash-interface/MultihashInterfaceExample.csproj -c Release
dotnet run --project examples/block-interface/BlockInterfaceExample.csproj -c Release
```

## Bumping NuGet package versions

All package versions live in `Directory.Packages.props` (NuGet Central Package Management). Do not add `Version="..."` back onto `<PackageReference>` entries — that's an error under CPM.

`RestoreLockedMode=true` is enabled globally, so a plain `dotnet restore` will FAIL with `NU1004` if any `Directory.Packages.props` version no longer matches the resolved entries in `packages.lock.json`. This is intentional: it forces lock-file regeneration to be an explicit, reviewable step.

To bump a package version:

1. Edit `Directory.Packages.props` and change the `Version` attribute on the relevant `<PackageVersion>` entry.
2. Regenerate every project's lock file in one pass:
   ```bash
   dotnet restore NetCid.sln --force-evaluate
   ```
3. Inspect the diff across all 10 `packages.lock.json` files (library + 2 test projects + 7 examples). Surprising transitive additions or version flips deserve scrutiny.
4. Commit `Directory.Packages.props` and all changed lock files together.

If CI fails with `NU1004`, the lock files drifted from the props file — re-run `--force-evaluate` and commit the result.

### Dependabot

`.github/dependabot.yml` is configured to open weekly PRs against the NuGet ecosystem (and GitHub Actions versions). Test-tooling bumps (`xunit*`, `Microsoft.NET.Test.Sdk`, `coverlet.*`) are grouped into a single PR. Dependabot regenerates the affected lock files automatically.

### Known review gap

`actions/dependency-review-action@v4` does not parse `Directory.Packages.props` or `packages.lock.json` — its NuGet manifest list is limited to `.csproj`, `.nuspec`, and `packages.config`. Transitive dependency changes that surface only in `packages.lock.json` will land without dependency-review findings. Reviewers should diff lock files by eye on any version bump until GitHub's dependency graph adds CPM support.

## Engineering Standards

- Follow existing style and naming conventions in `NetCid/`.
- Preserve or improve test coverage for changed behavior.
- Keep parsing and decode paths defensive against malformed input.
- Avoid silent behavioral changes; document intentional changes clearly in the PR.
- Keep commits and diffs focused. Do not mix refactors with unrelated fixes.

## Pull Request Checklist

Before opening or requesting review, confirm:

- [ ] The change is scoped to one clear problem.
- [ ] Relevant tests were added/updated and pass locally.
- [ ] Existing tests still pass in Release mode.
- [ ] Documentation/examples were updated if public behavior changed.
- [ ] Security-sensitive changes were reviewed for abuse cases and input limits.
- [ ] The PR description explains: problem, approach, risks, and verification steps.

## Commit and PR Quality Tips

Use commit messages that are short, specific, and imperative, for example:

- `Fix CIDv1 base32 parsing for uppercase prefix`
- `Add integration coverage for CID binary round-trip`
- `Document multibase behavior for CIDv0 vs CIDv1`

Strong PRs usually include:

- Linked issue or clear problem statement.
- Why this approach was chosen (especially for tradeoffs).
- Exact verification commands and results.

## Security Reporting

For suspected vulnerabilities, follow `SECURITY.md`. Do not post exploit details in public issues.

## License

By contributing, you agree that your contributions are licensed under the repository license (`LICENSE`).
