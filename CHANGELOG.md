# Changelog

All notable changes to Blazor Memoire are documented here.

## [2.0.4] - 2026-09-14

### Changed

- Optimized deep key comparisons by moving the deep-mode branch outside the per-key loop.
- Clarified the package description and README introduction.

## [2.0.3] - 2026-09-13

### Added

- Added fast comparison paths for more common scalar and nullable scalar types in arrays, lists, and sets.
- Added optimized dictionary comparison with comparer-aware equality.
- Added coverage for dictionary comparer differences, nullable values, and allocation-free sequence comparisons.

### Changed

- Improved deep collection comparison performance and correctness.
- Clarified the behavior of in-place collection mutation in the documentation.

## [2.0.2] - 2026-09-11

### Added

- Enabled generation of NuGet symbol packages (`.snupkg`).

## [2.0.1] - 2026-09-02

### Added

- Added .NET 8.0 to the target frameworks.

### Changed

- Replaced sync-over-async code in the benchmark tooling.
- Standardized project names and added a local .NET tool manifest.
- Updated benchmark and package configuration.

## [2.0.0] - 2026-09-01

### Added

- Added the `Deep` comparison option to `Memo`.
- Added benchmark projects and benchmark documentation.
- Added tests covering shallow comparison and additional collection key scenarios.

### Changed

- Made shallow key comparison the default, using per-element `Equals` semantics.
- Expanded README guidance and documented performance characteristics.

## [1.0.0] - 2026-08-30

### Changed

- Promoted the package to the 1.0.0 release.
- Refined comparer documentation and equality behavior guidance.

## [0.1.0] - 2026-08-30

### Added

- Initial public release of the Blazor `Memo` component.
- Added value-based comparison support for primitive values and common collections.
- Added the README, MIT license, and GitHub Actions workflow for publishing to NuGet.

[2.0.4]: https://github.com/matthetherington/Blazor-Memoire/releases/tag/v2.0.4
[2.0.3]: https://github.com/matthetherington/Blazor-Memoire/releases/tag/v2.0.3
[2.0.2]: https://github.com/matthetherington/Blazor-Memoire/releases/tag/v2.0.2
[2.0.1]: https://github.com/matthetherington/Blazor-Memoire/releases/tag/v2.0.1
[2.0.0]: https://github.com/matthetherington/Blazor-Memoire/releases/tag/v2.0.0
[1.0.0]: https://github.com/matthetherington/Blazor-Memoire/releases/tag/v1.0.0
[0.1.0]: https://github.com/matthetherington/Blazor-Memoire/releases/tag/v0.1.0
