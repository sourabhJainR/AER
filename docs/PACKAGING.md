# Package distribution

AER provides release-ready metadata for the primary developer ecosystems.

| Ecosystem | Package | Distribution |
|---|---|---|
| .NET | `AER.Format.Core` | NuGet |
| Node.js | `@aer-format/core` | npm |
| Python | `aer-format` | PyPI |
| Go | `github.com/sourabhJainR/AER/implementations/go` | Go modules |

Release tags use semantic versioning: `vMAJOR.MINOR.PATCH`. The release workflow publishes NuGet, npm and PyPI artifacts and creates a GitHub release. Go consumers resolve the same tag through the module proxy.

Publishing is intentionally credential-free from source: registry tokens are GitHub Actions secrets (`NUGET_API_KEY`, `NPM_TOKEN`, `PYPI_TOKEN`). Version numbers must match the release tag before publishing.
