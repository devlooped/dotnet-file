# Changelog

## [v1.7.1](https://github.com/devlooped/dotnet-file/tree/v1.7.1) (2024-10-31)

[Full Changelog](https://github.com/devlooped/dotnet-file/compare/v1.7.0...v1.7.1)

:sparkles: Implemented enhancements:

- Enable full unicode in commit change logs [\#130](https://github.com/devlooped/dotnet-file/pull/130) (@kzu)
- Render intermediate changelog to output [\#129](https://github.com/devlooped/dotnet-file/pull/129) (@kzu)
- Preserve trailing slash in file spec for directory [\#125](https://github.com/devlooped/dotnet-file/pull/125) (@kzu)

:twisted_rightwards_arrows: Merged:

- Modernize codebase [\#126](https://github.com/devlooped/dotnet-file/pull/126) (@kzu)

## [v1.7.0](https://github.com/devlooped/dotnet-file/tree/v1.7.0) (2024-10-18)

[Full Changelog](https://github.com/devlooped/dotnet-file/compare/v1.6.2...v1.7.0)

:sparkles: Implemented enhancements:

- Optimize retrieval and propagation of X-Sha [\#124](https://github.com/devlooped/dotnet-file/pull/124) (@kzu)
- Detect fetching folders/trees to avoid unnecessary auth [\#122](https://github.com/devlooped/dotnet-file/pull/122) (@kzu)
- Don't timeout while debugging [\#120](https://github.com/devlooped/dotnet-file/pull/120) (@kzu)

## [v1.6.2](https://github.com/devlooped/dotnet-file/tree/v1.6.2) (2024-07-08)

[Full Changelog](https://github.com/devlooped/dotnet-file/compare/v1.6.1...v1.6.2)

:sparkles: Implemented enhancements:

- Smarter empty directory cleanup [\#117](https://github.com/devlooped/dotnet-file/pull/117) (@kzu)
- Perform more optimal GCM API credentials retrieval if available [\#116](https://github.com/devlooped/dotnet-file/pull/116) (@kzu)

## [v1.6.1](https://github.com/devlooped/dotnet-file/tree/v1.6.1) (2024-07-08)

[Full Changelog](https://github.com/devlooped/dotnet-file/compare/v1.6.0...v1.6.1)

## [v1.6.0](https://github.com/devlooped/dotnet-file/tree/v1.6.0) (2024-07-08)

[Full Changelog](https://github.com/devlooped/dotnet-file/compare/v1.5.0...v1.6.0)

:sparkles: Implemented enhancements:

- Add a new relative target path heuristic with `/` in url [\#115](https://github.com/devlooped/dotnet-file/pull/115) (@kzu)
- Only authenticate with GH if necessary [\#114](https://github.com/devlooped/dotnet-file/pull/114) (@kzu)

## [v1.5.0](https://github.com/devlooped/dotnet-file/tree/v1.5.0) (2024-06-25)

[Full Changelog](https://github.com/devlooped/dotnet-file/compare/v1.5.1-beta...v1.5.0)

:sparkles: Implemented enhancements:

- Support "Etag" header in addition to "X-Etag" [\#86](https://github.com/devlooped/dotnet-file/issues/86)
- Bump to LTS .NET8 [\#112](https://github.com/devlooped/dotnet-file/pull/112) (@kzu)
- When updating single file, infer URL from config [\#107](https://github.com/devlooped/dotnet-file/pull/107) (@kzu)

:bug: Fixed bugs:

- How can I update a specific file? [\#85](https://github.com/devlooped/dotnet-file/issues/85)

## [v1.5.1-beta](https://github.com/devlooped/dotnet-file/tree/v1.5.1-beta) (2024-04-09)

[Full Changelog](https://github.com/devlooped/dotnet-file/compare/v1.4.1...v1.5.1-beta)

:twisted_rightwards_arrows: Merged:

- Bump to GCM v2.0.785 [\#78](https://github.com/devlooped/dotnet-file/pull/78) (@kzu)

## [v1.4.1](https://github.com/devlooped/dotnet-file/tree/v1.4.1) (2022-11-17)

[Full Changelog](https://github.com/devlooped/dotnet-file/compare/v1.4.0...v1.4.1)

:twisted_rightwards_arrows: Merged:

- +M▼ includes [\#65](https://github.com/devlooped/dotnet-file/pull/65) (@github-actions[bot])
- +M▼ includes [\#62](https://github.com/devlooped/dotnet-file/pull/62) (@github-actions[bot])

## [v1.4.0](https://github.com/devlooped/dotnet-file/tree/v1.4.0) (2022-06-29)

[Full Changelog](https://github.com/devlooped/dotnet-file/compare/v1.3.1...v1.4.0)

:sparkles: Implemented enhancements:

- Decrease package size by building 3.1 and using rollForward [\#56](https://github.com/devlooped/dotnet-file/issues/56)

:twisted_rightwards_arrows: Merged:

- Bump to latest GCM main [\#60](https://github.com/devlooped/dotnet-file/pull/60) (@kzu)

## [v1.3.1](https://github.com/devlooped/dotnet-file/tree/v1.3.1) (2021-10-27)

[Full Changelog](https://github.com/devlooped/dotnet-file/compare/v1.3.0...v1.3.1)

:bug: Fixed bugs:

- When same Url file, different target path, does not update multiple [\#54](https://github.com/devlooped/dotnet-file/issues/54)

## [v1.3.0](https://github.com/devlooped/dotnet-file/tree/v1.3.0) (2021-10-04)

[Full Changelog](https://github.com/devlooped/dotnet-file/compare/v1.2.1...v1.3.0)

:sparkles: Implemented enhancements:

- Allow flattening entire directory structure [\#52](https://github.com/devlooped/dotnet-file/issues/52)
- Allow specifying a relative directory for target file [\#49](https://github.com/devlooped/dotnet-file/issues/49)

## [v1.2.1](https://github.com/devlooped/dotnet-file/tree/v1.2.1) (2021-07-16)

[Full Changelog](https://github.com/devlooped/dotnet-file/compare/v1.2.0...v1.2.1)

:sparkles: Implemented enhancements:

- Include readme in package for better discoverability [\#47](https://github.com/devlooped/dotnet-file/issues/47)

## [v1.2.0](https://github.com/devlooped/dotnet-file/tree/v1.2.0) (2021-05-07)

[Full Changelog](https://github.com/devlooped/dotnet-file/compare/v1.0.1...v1.2.0)

:sparkles: Implemented enhancements:

- When no target path is provided, recreate source URI folder structure by default [\#37](https://github.com/devlooped/dotnet-file/issues/37)

:bug: Fixed bugs:

- When normalizing target path, leading dot in filename is removed [\#43](https://github.com/devlooped/dotnet-file/issues/43)
- When passing base directory for single-file download, default file path should be appended [\#41](https://github.com/devlooped/dotnet-file/issues/41)

:twisted_rightwards_arrows: Merged:

- Implement default path heuristics for easier directory structure cloning [\#38](https://github.com/devlooped/dotnet-file/pull/38) (@kzu)

## [v1.0.1](https://github.com/devlooped/dotnet-file/tree/v1.0.1) (2021-03-01)

[Full Changelog](https://github.com/devlooped/dotnet-file/compare/v1.0.0...v1.0.1)

:sparkles: Implemented enhancements:

- Checking changes now takes too long because of sha retrieval [\#32](https://github.com/devlooped/dotnet-file/issues/32)

## [v1.0.0](https://github.com/devlooped/dotnet-file/tree/v1.0.0) (2021-02-26)

[Full Changelog](https://github.com/devlooped/dotnet-file/compare/v0.9.4...v1.0.0)

:sparkles: Implemented enhancements:

- Allow generating a changelog for updated/synced files [\#30](https://github.com/devlooped/dotnet-file/issues/30)
- Persist the commit if available when updating file [\#28](https://github.com/devlooped/dotnet-file/issues/28)

:twisted_rightwards_arrows: Merged:

- Allow generating a changelog for updated/synced files [\#31](https://github.com/devlooped/dotnet-file/pull/31) (@kzu)
- Store commit sha if available when updating file [\#29](https://github.com/devlooped/dotnet-file/pull/29) (@kzu)
- 🖆 Apply kzu/oss template via dotnet-file [\#27](https://github.com/devlooped/dotnet-file/pull/27) (@kzu)

## [v0.9.4](https://github.com/devlooped/dotnet-file/tree/v0.9.4) (2021-01-25)

[Full Changelog](https://github.com/devlooped/dotnet-file/compare/v0.9.3...v0.9.4)

:hammer: Other:

- Rename Microsoft.DotNet namespace to Devlooped [\#25](https://github.com/devlooped/dotnet-file/issues/25)

:twisted_rightwards_arrows: Merged:

- Rename Microsoft.DotNet namespace to Devlooped [\#26](https://github.com/devlooped/dotnet-file/pull/26) (@kzu)
- ⭮ Switch to devlooped/oss template repo upstream [\#24](https://github.com/devlooped/dotnet-file/pull/24) (@kzu)
- ♡ Add sponsors section [\#23](https://github.com/devlooped/dotnet-file/pull/23) (@kzu)
- 🖆 Apply devlooped/oss template [\#22](https://github.com/devlooped/dotnet-file/pull/22) (@kzu)
- 🖆 Apply kzu/oss template [\#20](https://github.com/devlooped/dotnet-file/pull/20) (@kzu)

## [v0.9.3](https://github.com/devlooped/dotnet-file/tree/v0.9.3) (2020-12-15)

[Full Changelog](https://github.com/devlooped/dotnet-file/compare/v0.9.2...v0.9.3)

:sparkles: Implemented enhancements:

- Allow seeding from remote .netconfig URL\(s\) [\#18](https://github.com/devlooped/dotnet-file/issues/18)

:twisted_rightwards_arrows: Merged:

- 🌱 Create init command to seed directory from remote config  [\#19](https://github.com/devlooped/dotnet-file/pull/19) (@kzu)

## [v0.9.2](https://github.com/devlooped/dotnet-file/tree/v0.9.2) (2020-12-10)

[Full Changelog](https://github.com/devlooped/dotnet-file/compare/v0.9.1...v0.9.2)

:bug: Fixed bugs:

- When file exists locally but was deleted from remote, update stops with failure [\#16](https://github.com/devlooped/dotnet-file/issues/16)
- When updating file under .NET Core 2.1, download fails if local file exists [\#15](https://github.com/devlooped/dotnet-file/issues/15)

## [v0.9.1](https://github.com/devlooped/dotnet-file/tree/v0.9.1) (2020-12-09)

[Full Changelog](https://github.com/devlooped/dotnet-file/compare/v0.9.0...v0.9.1)

:sparkles: Implemented enhancements:

- Allow tool to run under dotnet 2.1 and 5.0 [\#14](https://github.com/devlooped/dotnet-file/issues/14)
- Provide repository information in the package [\#13](https://github.com/devlooped/dotnet-file/issues/13)

## [v0.9.0](https://github.com/devlooped/dotnet-file/tree/v0.9.0) (2020-12-08)

[Full Changelog](https://github.com/devlooped/dotnet-file/compare/6b935124cbe1c9268fb616a065582f7ff58d37a4...v0.9.0)

:sparkles: Implemented enhancements:

- Add prune command to remove files that no longer exist in the upstream [\#8](https://github.com/devlooped/dotnet-file/issues/8)
- Leverage ContentMD5 header if present [\#1](https://github.com/devlooped/dotnet-file/issues/1)

:hammer: Other:

- Allow skipping entries for update/add of GitHub repo/folder urls [\#10](https://github.com/devlooped/dotnet-file/issues/10)
- Providing no args throws exception [\#6](https://github.com/devlooped/dotnet-file/issues/6)

:twisted_rightwards_arrows: Merged:

- 🖆 Apply kzu/oss template via dotnet file [\#11](https://github.com/devlooped/dotnet-file/pull/11) (@kzu)
- Add Sync operation [\#9](https://github.com/devlooped/dotnet-file/pull/9) (@kzu)
- Download to temp then move to local file [\#5](https://github.com/devlooped/dotnet-file/pull/5) (@atifaziz)
- Support HTTP de/compression [\#4](https://github.com/devlooped/dotnet-file/pull/4) (@atifaziz)
- Don't fail if no files are configured for downloading [\#3](https://github.com/devlooped/dotnet-file/pull/3) (@kzu)



\* *This Changelog was automatically generated by [github_changelog_generator](https://github.com/github-changelog-generator/github-changelog-generator)*
