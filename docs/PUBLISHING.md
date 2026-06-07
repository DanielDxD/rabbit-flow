# Publishing RabbitFlow to NuGet.org

Complete guide to pack and publish the `RabbitFlow` package.

## Prerequisites

1. An account on [nuget.org](https://www.nuget.org)
2. [.NET 10 SDK](https://dotnet.microsoft.com/download)
3. A Git repository with history (recommended for Source Link)

## Before the first release

### 1. Verify metadata

Review and adjust if needed in [`Directory.Build.props`](../Directory.Build.props):

| Property | Current value |
|---|---|
| `Authors` | Daniel Lopes |
| `RepositoryUrl` | https://github.com/DanielDxD/rabbit-flow |
| `VersionPrefix` | 1.0.0 |

### 2. Reserve the Package ID

Confirm that `RabbitFlow` is available at [nuget.org/packages/RabbitFlow](https://www.nuget.org/packages/RabbitFlow).

If it is taken, change `PackageId` in [`RabbitFlow/RabbitFlow.csproj`](../RabbitFlow/RabbitFlow.csproj).

### 3. Create a NuGet API key

1. Go to [nuget.org/account/apikeys](https://www.nuget.org/account/apikeys)
2. Create a key with **Push** scope for the `RabbitFlow` package
3. Save the key — it is only shown once

### 4. Set up the Git repository (recommended)

```bash
git init
git add .
git commit -m "Initial commit"
git remote add origin https://github.com/DanielDxD/rabbit-flow.git
git push -u origin main
```

## Pack locally

```bash
# Runs tests and generates .nupkg + .snupkg in ./artifacts
chmod +x scripts/pack.sh
./scripts/pack.sh
```

Validate the package contents:

```bash
# Install inspection tool (one-time)
dotnet tool install -g nuget-package-explorer

# Or list contents via unzip
unzip -l artifacts/RabbitFlow.*.nupkg | head -30
```

Test local installation:

```bash
dotnet add package RabbitFlow --source ./artifacts
```

## Publish manually

```bash
export NUGET_API_KEY="your-api-key-here"
chmod +x scripts/publish.sh
./scripts/publish.sh
```

Or step by step:

```bash
./scripts/pack.sh

dotnet nuget push artifacts/RabbitFlow.1.0.0.nupkg \
  --api-key "$NUGET_API_KEY" \
  --source https://api.nuget.org/v3/index.json \
  --skip-duplicate

dotnet nuget push artifacts/RabbitFlow.1.0.0.snupkg \
  --api-key "$NUGET_API_KEY" \
  --source https://api.nuget.org/v3/index.json \
  --skip-duplicate
```

> **Never** commit the API key. Use an environment variable or a CI secret.

## Publish via GitHub Actions

The [`.github/workflows/release-nuget.yml`](../.github/workflows/release-nuget.yml) workflow runs automatically:

| Trigger | What runs |
|---|---|
| Push / merge to `main` or `master` | Test → Pack → **Publish to NuGet** |
| Pull request to `main` or `master` | Test → Pack (no publish) |
| GitHub Release published | Test → Pack → Publish to NuGet |
| Manual (`workflow_dispatch`) | Test → Pack → Publish to NuGet |

Setup:

1. Add the `NUGET_API_KEY` secret under **Settings → Secrets and variables → Actions**
2. Merge to `main` (or create a GitHub release)

Manual trigger:

```text
GitHub → Actions → Release NuGet → Run workflow
```

## Versioning

Follow [Semantic Versioning](https://semver.org/):

| Change | Example |
|---|---|
| Bug fix | `1.0.0` → `1.0.1` |
| Backward-compatible feature | `1.0.1` → `1.1.0` |
| Breaking change | `1.1.0` → `2.0.0` |

Update:

1. `VersionPrefix` in `Directory.Build.props`
2. Entry in `CHANGELOG.md`
3. Git tag `v{version}`
4. GitHub release

## Final checklist

- [ ] Tests passing (`dotnet test`)
- [ ] Coverage ≥ 80%
- [ ] `VersionPrefix` updated
- [ ] `CHANGELOG.md` updated
- [ ] `README.md` with correct installation instructions
- [ ] `RepositoryUrl` pointing to the real repository
- [ ] NuGet API key created
- [ ] Package packed and tested locally
- [ ] Published to NuGet.org
- [ ] Badge added to README (optional)

## README badge (after publishing)

```markdown
[![NuGet](https://img.shields.io/nuget/v/RabbitFlow.svg)](https://www.nuget.org/packages/RabbitFlow/)
```

## Troubleshooting

| Error | Solution |
|---|---|
| `403 Forbidden` | Invalid API key or insufficient permissions for the package |
| `409 Conflict` | Version already published — increment `VersionPrefix` |
| `Package already exists` | The `RabbitFlow` ID belongs to another author — change `PackageId` |
| Source Link warnings | Initialize Git and make at least one commit |
