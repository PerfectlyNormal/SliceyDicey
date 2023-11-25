# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [unreleased]

### Added

- Support for [QOI] thumbnails since PrusaSlicer 2.7.0 switches to these instead of PNG.

### Changed

- `GcodeThumbnail` renamed to `GcodePngThumbnail` and implements `IGcodeThumbnail` instead to handle both formats.

## [0.1.0] - 2023-11-07

### Added

- Initial release

[unreleased]: https://github.com/PerfectlyNormal/SliceyDicey/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/PerfectlyNormal/SliceyDicey/tree/release/v0.1.0

[QOI]: https://qoiformat.org/
