# Changelog

## 0.13.1 - 2025-01-08

### Fixed

* InvalidOperationException on some DUs. [#128](https://github.com/ionide/ionide-analyzers/issues/128)

## 0.13.0 - 2024-11-19

### Added

* Bump FSharp.Analyzers.SDK to `0.28.0`. [#129](https://github.com/ionide/ionide-analyzers/pull/129)
  * This also bumps the TFM of the library and CLI to net8.0

### Removed

* Support for .NET 6 and .NET 7

## 0.12.0 - 2024-08-18

### Changed

* Update FSharp.Analyzers.SDK to `0.27.0`. [#111](https://github.com/ionide/ionide-analyzers/pull/110)

## 0.11.1 - 2024-08-06

* Update FSharp.Analyzers.SDK to `0.26.1`. [#110](https://github.com/ionide/ionide-analyzers/pull/110)

## 0.11.0 - 2024-05-15

### Changed

* Update FSharp.Analyzers.SDK to `0.26.0`, FSharp.Compiler.Service to `43.8.300`, and FSharp.Core to `8.0.300`. [#93](https://github.com/ionide/ionide-analyzers/pull/93)

## 0.10.0 - 2024-03-29

### Added
* HeadConsEmptyListPatternAnalyzer. [#85](https://github.com/ionide/ionide-analyzers/pull/85)
* ListEqualsEmptyListAnalyzer. [#85](https://github.com/ionide/ionide-analyzers/pull/85)
* ReturnStructPartialActivePatternAnalyzer [#85](https://github.com/ionide/ionide-analyzers/pull/85)
* CombinePipedModuleFunctionsAnalyzer [#85](https://github.com/ionide/ionide-analyzers/pull/85)
* EqualsNullAnalyzer [#85](https://github.com/ionide/ionide-analyzers/pull/85)
* StructDiscriminatedUnionAnalyzer. [#85](https://github.com/ionide/ionide-analyzers/pull/85)

## 0.9.0 - 2024-02-15

### Changed
* Update FSharp.Analyzers.SDK to `0.25.0`. [#68](https://github.com/ionide/ionide-analyzers/pull/75)

## 0.8.0 - 2024-01-30

### Changed
* Update FSharp.Analyzers.SDK to `0.24.0`. [#68](https://github.com/ionide/ionide-analyzers/pull/75)

## 0.7.0 - 2024-01-10

### Added
* Add fix-support to the CopyAndUpdateRecordChangesAllFieldsAnalyzer. [#68](https://github.com/ionide/ionide-analyzers/pull/68)

### Changed
* Update FSharp.Analyzers.SDK to `0.23.0`. [#68](https://github.com/ionide/ionide-analyzers/pull/68)

## 0.6.1 - 2024-01-01

### Added
* Add editor support to all analyzers. [#64](https://github.com/ionide/ionide-analyzers/pull/64)

## 0.6.0 - 2023-12-20

### Changed
* Update FSharp.Analyzers.SDK to `0.22.0`. [#60](https://github.com/ionide/ionide-analyzers/pull/60)

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