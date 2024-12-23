# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [unreleased]

### Changed

## [0.3.1] - 2023-11-01

- Update dependencies

## [0.3.0] - 2023-12-02

### Changed

- Update ImageSharp to v3.1.0
- Remove custom QOI decoder and use ImageSharp directly

## [0.2.3] - 2023-12-02

### Fixed

- Properly rewind stream after converting QOI to PNG

## [0.2.2] - 2023-12-02

### Fixed

- Remove debug code that writes Meatpack blocks to file

## [0.2.1] - 2023-12-01

### Fixed

- Fixed nuget package adding a reference to our own library

## [0.2.0] - 2023-12-01

### Added

- Support for [QOI] thumbnails since PrusaSlicer 2.7.0 switches to these instead of PNG.
- Support for parsing binary GCode files

### Changed

- `GcodeThumbnail` renamed to `GcodePngThumbnail` and implements `IGcodeThumbnail` instead to handle both formats.

## [0.1.0] - 2023-11-07

### Added

- Initial release

[unreleased]: https://github.com/PerfectlyNormal/SliceyDicey/compare/v0.3.1...HEAD
[0.3.0]: https://github.com/PerfectlyNormal/SliceyDicey/compare/v0.3.1...v0.3.0
[0.3.0]: https://github.com/PerfectlyNormal/SliceyDicey/compare/v0.2.3...v0.3.0
[0.2.3]: https://github.com/PerfectlyNormal/SliceyDicey/compare/v0.2.2...v0.2.3
[0.2.2]: https://github.com/PerfectlyNormal/SliceyDicey/compare/v0.2.1...v0.2.2
[0.2.1]: https://github.com/PerfectlyNormal/SliceyDicey/compare/v0.2.0...v0.2.1
[0.2.0]: https://github.com/PerfectlyNormal/SliceyDicey/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/PerfectlyNormal/SliceyDicey/tree/release/v0.1.0

[QOI]: https://qoiformat.org/
