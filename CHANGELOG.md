# Changelog

## 0.5.1 - 2023-12-06

### Fixed
* Handle types without a FullName more gracefully in the EmptyStringAnalyzer. [#48](https://github.com/ionide/ionide-analyzers/pull/48)
* Handle types without a FullName more gracefully in the HandleOptionGracefullyAnalyzer. [#50](https://github.com/ionide/ionide-analyzers/pull/50)

## 0.5.0 - 2023-11-23

### Changed
* Reworks `ReplaceOptionGetWithGracefulHandlingAnalyzer` to handle ValueOption's and `.Value` member access. [#33](https://github.com/ionide/ionide-analyzers/pull/33) 
* Reworks `SquareBracketArrayAnalyzer` to handle all generic types that should be postfixed. [#39](https://github.com/ionide/ionide-analyzers/pull/39)
* Update FSharp.Analyzers.SDK to `0.21.0`. [#45](https://github.com/ionide/ionide-analyzers/pull/45)

## 0.4.0 - 2023-11-15

### Added
* ReplaceOptionGetWithGracefulHandlingAnalyzer [#32](https://github.com/ionide/ionide-analyzers/pull/32)

## 0.3.0 - 2023-11-13

### Changed
* Update FSharp.Analyzers.SDK to v0.20.0. [#26](https://github.com/ionide/ionide-analyzers/pull/26)

## 0.2.0 - 2023-11-09

### Fixed
* Fix analyzers urls. [#19](https://github.com/ionide/ionide-analyzers/pull/19)
* Fix analyzers codes. [#22](https://github.com/ionide/ionide-analyzers/pull/22)

### Added
* Support for referencing a local analyzers SDK. [#18](https://github.com/ionide/ionide-analyzers/pull/18)
* EmptyStringAnalyzer. [#20](https://github.com/ionide/ionide-analyzers/pull/20)

## 0.1.1 - 2023-11-07

### Fixed
* Update NuGet properties. [#14](https://github.com/ionide/ionide-analyzers/pull/14)

## 0.1.0 - 2023-11-07

### Added
* Initial version