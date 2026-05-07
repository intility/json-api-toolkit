# Changelog

## [2.1.0](https://github.com/intility/Intility.JsonApiToolkit/compare/v2.0.1...v2.1.0) (2026-05-07)


### Features

* **docs:** migrate to mkdocs material with generated API reference ([#129](https://github.com/intility/Intility.JsonApiToolkit/issues/129)) ([765d46a](https://github.com/intility/Intility.JsonApiToolkit/commit/765d46a155b7fbd08d3b981766478b884243f0f7))
* **pagination:** return 404 when page exceeds total pages in strict mode ([#126](https://github.com/intility/Intility.JsonApiToolkit/issues/126)) ([b639fdd](https://github.com/intility/Intility.JsonApiToolkit/commit/b639fdd9ece2c7510bd3a558f05876288ad36b67))


### Bug Fixes

* correct casing for Intility.DotnetApiDocs in dotnet-tools configuration ([01ed721](https://github.com/intility/Intility.JsonApiToolkit/commit/01ed721092f687b126b0e6edf1190e198166cc0a))
* follow dotnet api docs rename ([abe8436](https://github.com/intility/Intility.JsonApiToolkit/commit/abe8436f56b97cff8f99c32049e5184736ee5f5f))


### Code Refactoring

* **controller:** extract EnforceStrictPagination from JsonApiQueryAsync ([#128](https://github.com/intility/Intility.JsonApiToolkit/issues/128)) ([e501631](https://github.com/intility/Intility.JsonApiToolkit/commit/e5016314063c4bf083c7fbafedd1a2dafe5deddc))

## [2.0.1](https://github.com/intility/Intility.JsonApiToolkit/compare/v2.0.0...v2.0.1) (2026-05-06)


### Bug Fixes

* **build:** reference root README.md in package ([ecbdb2a](https://github.com/intility/Intility.JsonApiToolkit/commit/ecbdb2a1ad73475350d1bcbad0f96656670075cd))
* **lockfile:** restore linux-musl-x64 RID section ([0c523f1](https://github.com/intility/Intility.JsonApiToolkit/commit/0c523f1183d9662872d0cd10354cdca3139a4153))

## [2.0.0](https://github.com/intility/Intility.JsonApiToolkit/compare/v1.8.1...v2.0.0) (2026-05-05)


### ⚠ BREAKING CHANGES

* upgrade to .NET 10 ([#112](https://github.com/intility/Intility.JsonApiToolkit/issues/112))

### Features

* upgrade to .NET 10 ([#112](https://github.com/intility/Intility.JsonApiToolkit/issues/112)) ([7e9bae9](https://github.com/intility/Intility.JsonApiToolkit/commit/7e9bae991b94971d9c7860bd2c68ca32214e8bf3))


### Bug Fixes

* code quality and fixes ([#116](https://github.com/intility/Intility.JsonApiToolkit/issues/116)) ([0f7af6b](https://github.com/intility/Intility.JsonApiToolkit/commit/0f7af6b5b01f743a2e23da73f63e72cd06120b7a))
* use LINQ `Select` instead of foreach loop in `EfIncludePathHelper` ([#115](https://github.com/intility/Intility.JsonApiToolkit/issues/115)) ([381d1b1](https://github.com/intility/Intility.JsonApiToolkit/commit/381d1b1e8c0219b813e76ccda7b8d514b875c0f5))


### Code Refactoring

* adopt C# 14 language features ([#114](https://github.com/intility/Intility.JsonApiToolkit/issues/114)) ([2b0169e](https://github.com/intility/Intility.JsonApiToolkit/commit/2b0169eaefc49395754bad34698276c28a250046))


### Documentation

* auto-generated docs for intility/Intility.JsonApiToolkit[#116](https://github.com/intility/Intility.JsonApiToolkit/issues/116) ([#117](https://github.com/intility/Intility.JsonApiToolkit/issues/117)) ([89f5cdf](https://github.com/intility/Intility.JsonApiToolkit/commit/89f5cdf540734b7cdae93ef4dfe2e6959da69fca))


### Build System

* **nuget:** Bump coverlet.collector from 8.0.1 to 10.0.0 ([#120](https://github.com/intility/Intility.JsonApiToolkit/issues/120)) ([8d489bd](https://github.com/intility/Intility.JsonApiToolkit/commit/8d489bdd2f11611cfde34ad0805c8480c7ae281f))
* **nuget:** Bump the microsoft group with 4 updates ([#119](https://github.com/intility/Intility.JsonApiToolkit/issues/119)) ([5c8f97e](https://github.com/intility/Intility.JsonApiToolkit/commit/5c8f97e1c1414f74d8c0c055c0215fb1b61df800))
* **nuget:** Bump the microsoft group with 5 updates ([#124](https://github.com/intility/Intility.JsonApiToolkit/issues/124)) ([f616e77](https://github.com/intility/Intility.JsonApiToolkit/commit/f616e77f50a7d42d49ccec13fe55c820840346d2))


### CI

* **actions:** bump github/codeql-action from 4.35.1 to 4.35.2 ([#118](https://github.com/intility/Intility.JsonApiToolkit/issues/118)) ([600e53b](https://github.com/intility/Intility.JsonApiToolkit/commit/600e53b902d75af633853fb318b49751813bd6ed))
* **actions:** Bump github/codeql-action from 4.35.2 to 4.35.3 ([#123](https://github.com/intility/Intility.JsonApiToolkit/issues/123)) ([ba623a5](https://github.com/intility/Intility.JsonApiToolkit/commit/ba623a517133f14719e6df33ecf555f920e38c64))
* **actions:** Bump googleapis/release-please-action from 4.4.1 to 5.0.0 ([#121](https://github.com/intility/Intility.JsonApiToolkit/issues/121)) ([973d5ad](https://github.com/intility/Intility.JsonApiToolkit/commit/973d5ade87eec86135f3a03f92f7744ec49c0d60))

## [1.8.1](https://github.com/intility/Intility.JsonApiToolkit/compare/v1.8.0...v1.8.1) (2026-04-14)


### Bug Fixes

* **lockfile:** regenerate lock file with linux-musl-x64 RID section ([715745b](https://github.com/intility/Intility.JsonApiToolkit/commit/715745b9f90175e7425a6f03a960a8612140665f))


### Build System

* add NuGet lock file support for supply chain security ([6a2cd14](https://github.com/intility/Intility.JsonApiToolkit/commit/6a2cd14b15b87f7838031485e45da64bb340acc6))

## [1.8.0](https://github.com/intility/Intility.JsonApiToolkit/compare/v1.7.3...v1.8.0) (2026-04-14)


### Features

* **projection:** add database-level column filtering ([#101](https://github.com/intility/Intility.JsonApiToolkit/issues/101)) ([3234779](https://github.com/intility/Intility.JsonApiToolkit/commit/3234779912a4b9d359c4f943b3ce03df2c519e37))

## [1.7.3](https://github.com/intility/Intility.JsonApiToolkit/compare/v1.7.2...v1.7.3) (2026-04-07)


### Bug Fixes

* resolve all 36 open CodeQL code scanning alerts ([#102](https://github.com/intility/Intility.JsonApiToolkit/issues/102)) ([25cf2ef](https://github.com/intility/Intility.JsonApiToolkit/commit/25cf2ef8841e84341658c055fd911593e14e2636))


### Documentation

* add OpenAPI integration guide ([#99](https://github.com/intility/Intility.JsonApiToolkit/issues/99)) ([72c0ee1](https://github.com/intility/Intility.JsonApiToolkit/commit/72c0ee18c55088427d4fb54c5aa73ac6a65d9185))


### CI

* add Claude code review and auto-docs workflows ([3c9eba1](https://github.com/intility/Intility.JsonApiToolkit/commit/3c9eba1b757c59f23f0032ae53e73ab78962df6f))

## [1.7.2](https://github.com/intility/Intility.JsonApiToolkit/compare/v1.7.1...v1.7.2) (2026-03-27)


### Documentation

* add MIT license ([#96](https://github.com/intility/Intility.JsonApiToolkit/issues/96)) ([20b7544](https://github.com/intility/Intility.JsonApiToolkit/commit/20b7544dbc68eb63e4d4ed21a1e311dfa0e4a2e9))

## [1.7.1](https://github.com/intility/Intility.JsonApiToolkit/compare/v1.7.0...v1.7.1) (2026-03-24)


### Dependencies

* **actions:** bump actions/create-github-app-token from 2.2.1 to 3.0.0 ([#86](https://github.com/intility/Intility.JsonApiToolkit/issues/86)) ([e7bd739](https://github.com/intility/Intility.JsonApiToolkit/commit/e7bd7392b655e281373b5002af7ed047e361f9a3))
* **nuget:** Bump coverlet.collector from 6.0.2 to 8.0.1 ([#90](https://github.com/intility/Intility.JsonApiToolkit/issues/90)) ([c8f3243](https://github.com/intility/Intility.JsonApiToolkit/commit/c8f3243a922e2d647217584227c0119f52ef0874))
* **nuget:** Bump Intility.Logging.AspNetCore from 3.0.3 to 3.1.4 ([#89](https://github.com/intility/Intility.JsonApiToolkit/issues/89)) ([7634ef7](https://github.com/intility/Intility.JsonApiToolkit/commit/7634ef7d0539ab156176694f98188158de366082))
* **nuget:** Bump Microsoft.AspNetCore.JsonPatch from 9.0.2 to 10.0.5 ([#91](https://github.com/intility/Intility.JsonApiToolkit/issues/91)) ([fd94546](https://github.com/intility/Intility.JsonApiToolkit/commit/fd945469b75c9d5dd7da7a9ecaeaf658ade6274a))
* **nuget:** Bump Microsoft.Extensions.DependencyInjection.Abstractions from 9.0.13 to 10.0.5 ([#92](https://github.com/intility/Intility.JsonApiToolkit/issues/92)) ([a2f2d73](https://github.com/intility/Intility.JsonApiToolkit/commit/a2f2d7388d388e76c855939f966c19b0105a260b))
* **nuget:** Bump Microsoft.NET.Test.Sdk from 17.11.1 to 18.3.0 ([#93](https://github.com/intility/Intility.JsonApiToolkit/issues/93)) ([d4bf186](https://github.com/intility/Intility.JsonApiToolkit/commit/d4bf1864fda790367cc5026a39f361416d46568d))
* **nuget:** Bump the microsoft group with 8 updates ([#87](https://github.com/intility/Intility.JsonApiToolkit/issues/87)) ([e6ecca2](https://github.com/intility/Intility.JsonApiToolkit/commit/e6ecca2539b47375331225a3d43fc4585a147cea))
* **nuget:** Bump the testing group with 2 updates ([#88](https://github.com/intility/Intility.JsonApiToolkit/issues/88)) ([06329e6](https://github.com/intility/Intility.JsonApiToolkit/commit/06329e620dff7f70feb4a7d4d40dad8710983376))
* **nuget:** Bump xunit.runner.visualstudio from 2.8.2 to 3.1.5 ([#94](https://github.com/intility/Intility.JsonApiToolkit/issues/94)) ([5d7b81a](https://github.com/intility/Intility.JsonApiToolkit/commit/5d7b81adbc59aeb68f6befd2df873a061810fe06))

## [1.7.0](https://github.com/intility/Intility.JsonApiToolkit/compare/v1.6.0...v1.7.0) (2026-02-10)


### Features

* implement sparse fieldsets (`fields[type]`) per JSON:API spec ([#82](https://github.com/intility/Intility.JsonApiToolkit/issues/82)) ([d03136b](https://github.com/intility/Intility.JsonApiToolkit/commit/d03136bb307ee3c1998bc5ac57e756c724e9eb44))


### Bug Fixes

* update editorconfig ([#84](https://github.com/intility/Intility.JsonApiToolkit/issues/84)) ([1931137](https://github.com/intility/Intility.JsonApiToolkit/commit/1931137bcd7f5d14ee77ffb3993d7087fbf5deeb))


### Dependencies

* **nuget:** Bump csharpier from 1.2.5 to 1.2.6 ([#81](https://github.com/intility/Intility.JsonApiToolkit/issues/81)) ([5f63735](https://github.com/intility/Intility.JsonApiToolkit/commit/5f6373589722810fb3d85e6ff8b7ca204766617d))

## [1.6.0](https://github.com/intility/Intility.JsonApiToolkit/compare/v1.5.1...v1.6.0) (2026-01-25)


### Features

* add `BuildJsonApiQueryAsync` for custom query execution ([#79](https://github.com/intility/Intility.JsonApiToolkit/issues/79)) ([66a8a00](https://github.com/intility/Intility.JsonApiToolkit/commit/66a8a00b8580a789464b2d782b4a60c8ce7dbd71))

## [1.5.1](https://github.com/intility/Intility.JsonApiToolkit/compare/v1.5.0...v1.5.1) (2026-01-25)


### Bug Fixes

* **pagination:** eliminate redundant sync COUNT query ([#77](https://github.com/intility/Intility.JsonApiToolkit/issues/77)) ([cd4d84f](https://github.com/intility/Intility.JsonApiToolkit/commit/cd4d84f6d4683c8a21c41300755ba9e978910c5e))

## [1.5.0](https://github.com/intility/Intility.JsonApiToolkit/compare/v1.4.0...v1.5.0) (2026-01-24)


### Features

* complete Phase 3 with circular reference tests and documentation ([#75](https://github.com/intility/Intility.JsonApiToolkit/issues/75)) ([c09a0e1](https://github.com/intility/Intility.JsonApiToolkit/commit/c09a0e191ec0c74749c6d6164a09d7bc6122ed68))

## [1.4.0](https://github.com/intility/Intility.JsonApiToolkit/compare/v1.3.1...v1.4.0) (2026-01-24)


### Features

* add `JsonApiOptions` for configurable query limits ([#63](https://github.com/intility/Intility.JsonApiToolkit/issues/63)) ([eb6e886](https://github.com/intility/Intility.JsonApiToolkit/commit/eb6e8869901cf10266f96c47587041ba679dc593))
* add recursion depth guard for nested collection filters ([#66](https://github.com/intility/Intility.JsonApiToolkit/issues/66)) ([36e2c5a](https://github.com/intility/Intility.JsonApiToolkit/commit/36e2c5a75f99d57375ec121f7133ab98ac87b878))
* validate filter paths against `AllowedIncludes` ([#65](https://github.com/intility/Intility.JsonApiToolkit/issues/65)) ([df411fe](https://github.com/intility/Intility.JsonApiToolkit/commit/df411fe6b0da25b7dffae36303b7adf1d3b4b485))


### Documentation

* update documentation for v1.4.0 security features ([4d3df54](https://github.com/intility/Intility.JsonApiToolkit/commit/4d3df542630030e14ca27d6b48826e72cfa3e485))

## [1.3.1](https://github.com/intility/Intility.JsonApiToolkit/compare/Intility.JsonApiToolkit-v1.3.0...Intility.JsonApiToolkit-v1.3.1) (2026-01-24)


### Bug Fixes

* **ci:** read version from csproj instead of parsing tag ([a881cba](https://github.com/intility/Intility.JsonApiToolkit/commit/a881cba744df51cc6a419e9f06643aa054e0391b))

## [1.3.0](https://github.com/intility/Intility.JsonApiToolkit/compare/Intility.JsonApiToolkit-v1.2.5...Intility.JsonApiToolkit-v1.3.0) (2026-01-24)


### Features

* ✨ `AllowedIncludesAttribute` to whitelist allowed include paths ([6a26c29](https://github.com/intility/Intility.JsonApiToolkit/commit/6a26c29640c84374f5a85c4307934495d2b88ed7))
* ✨ `JsonApiOkAsync` ([d22466d](https://github.com/intility/Intility.JsonApiToolkit/commit/d22466d6f24cefcc32bd8800d0777791bbf66a6d))
* ✨ `JsonApiOkAsync` ([bc26940](https://github.com/intility/Intility.JsonApiToolkit/commit/bc2694061e4f29a7fccddfd3671b29ccc931da14))
* ✨ add filtering support for included resources ([4e81c99](https://github.com/intility/Intility.JsonApiToolkit/commit/4e81c99c31f75988dbf4469481d53b2d350ea914))
* ✨ add support for filtering in primary resource with included r… ([0b0bb87](https://github.com/intility/Intility.JsonApiToolkit/commit/0b0bb8724e73295c952563ebfd07d22eb534f47f))
* ✨ add support for filtering in primary resource with included relationships ([5c90d41](https://github.com/intility/Intility.JsonApiToolkit/commit/5c90d41c24192e9b22c64e8a159aab6ddc20cd41))
* ✨ add too many reqyests exeption ([94a810d](https://github.com/intility/Intility.JsonApiToolkit/commit/94a810d91d510fb61cdef896e437c433c1a3934e))
* ✨ Allow collections and json columns to be mapped ([6a096bc](https://github.com/intility/Intility.JsonApiToolkit/commit/6a096bc3bebf12e4c07d9721566b522af4761541))
* ✨ Code cleanup and standardization of error handling ([fec75f5](https://github.com/intility/Intility.JsonApiToolkit/commit/fec75f5f384592be4af0c58481c33a9ff59e7309))
* ✨ Enhance QueryHelpers with enum support and additional types ([4949c26](https://github.com/intility/Intility.JsonApiToolkit/commit/4949c26c53ef896ce818ded9bcea0b737bb69608))
* ✨ general-purpose exception class ([0cdf9a5](https://github.com/intility/Intility.JsonApiToolkit/commit/0cdf9a59379e7f13bc02e2028e0ad61db1b79d51))
* ✨ general-purpose exception class ([e222bb4](https://github.com/intility/Intility.JsonApiToolkit/commit/e222bb48909af8ca38e9059872bc4889c0ace502))
* ✨ Overall project cleanup ([c8c10f4](https://github.com/intility/Intility.JsonApiToolkit/commit/c8c10f4d2d6b58d6e88b3ae6ef6cb5a5769ff7b2))
* ✨ Remove IncludeAsAttribute and related logic ([a1593be](https://github.com/intility/Intility.JsonApiToolkit/commit/a1593be67867d2a8331e05529406c4b634a2ca48))
* ✨ Support complex JsonCols ([b744b8e](https://github.com/intility/Intility.JsonApiToolkit/commit/b744b8ea3848601ff5be79eaf0243b713917a1bc))
* 📚 add comprehensive debugging guide and enhance logging for better troubleshooting ([0ecc0cf](https://github.com/intility/Intility.JsonApiToolkit/commit/0ecc0cfe4b669b1eff7ed17311cbaab050a91673))
* 🚀 add ApplyFiltersOnly method for pre-aggregation filtering and add documentation on statistics and aggregations ([b9c6546](https://github.com/intility/Intility.JsonApiToolkit/commit/b9c65466fef8fb25f4c16ae283247043f9bc16d9))
* 🚀 enhance query processing with AsSingleQuery for pagination and add detailed logging for inclusion processing ([7824da9](https://github.com/intility/Intility.JsonApiToolkit/commit/7824da922c905bee096a759ff2813b1437392845))
* **errors:** add JsonApiErrorCodes and JsonApiErrors factory methods ([#60](https://github.com/intility/Intility.JsonApiToolkit/issues/60)) ([8531ad3](https://github.com/intility/Intility.JsonApiToolkit/commit/8531ad39acd3d7b1b64f5e7d4d79e63731752d01))
* **errors:** complete refactor Phase 1 with exception filter tests a… ([#61](https://github.com/intility/Intility.JsonApiToolkit/issues/61)) ([e5b50be](https://github.com/intility/Intility.JsonApiToolkit/commit/e5b50be0482b8565c5fc477efaf5bece82d3ec00))


### Bug Fixes

* :bug: single included resources are no longer ignored ([1e2e4b6](https://github.com/intility/Intility.JsonApiToolkit/commit/1e2e4b65227bb60599f816e1ccd0e320f1faf8b2))
* 🚑️ `[JsonIgnore]` not being respected ([af12b0b](https://github.com/intility/Intility.JsonApiToolkit/commit/af12b0b83edade9e033b67a9515b435e87a6d66e))
* 🚑️ adds support for filtering on included collection fields ([ee2eb19](https://github.com/intility/Intility.JsonApiToolkit/commit/ee2eb19489aa362c9290acd8206843b0ba20bf6d))
* 🚑️ adds support for filtering on included collection fields ([1194fd6](https://github.com/intility/Intility.JsonApiToolkit/commit/1194fd672578b22242a43e8dd3f16aefa74b11ed))
* 🚑️ adjust query processing order for filtered and regular includes to enhance EF Core compatibility ([8d21509](https://github.com/intility/Intility.JsonApiToolkit/commit/8d21509960ebb11aedd525e970caec12da1f9e4c))
* 🚑️ bracket nested filtering without the nessesary includes breaking main filtering ([e1e5785](https://github.com/intility/Intility.JsonApiToolkit/commit/e1e5785eebf4936a5ff8afe1243f85279059225a))
* 🚑️ correct version number in project file to match release version ([a0a51dd](https://github.com/intility/Intility.JsonApiToolkit/commit/a0a51ddaf865e8411b9cb1be6deddf4645aa82e8))
* 🚑️ error responses for forbidden includes did not include meta information ([9610878](https://github.com/intility/Intility.JsonApiToolkit/commit/961087876e339a2e8febf9ef3f88f1ef652c1894))
* 🚑️ filtering on includes not working on 2-level ([c658327](https://github.com/intility/Intility.JsonApiToolkit/commit/c658327d4ce1d875ea60e2621390851b634efbd7))
* 🚑️ Fixed the filtering issue for included resources. ([86cab81](https://github.com/intility/Intility.JsonApiToolkit/commit/86cab81f175892ecf75938d3a3fc8894c6c5400c))
* 🚑️ improve error messages for forbidden includes to clarify not found status ([07ea15e](https://github.com/intility/Intility.JsonApiToolkit/commit/07ea15ec70004eb8b37f847e6459bb24983d18f5))
* 🚑️ Initial working fix. Needs further testing and validation. ([0fa5628](https://github.com/intility/Intility.JsonApiToolkit/commit/0fa5628e71e1c9712550ab2f4cf714136ca8146d))
* 🚑️ JsonApiOk and JsonApiCreated methods not adding includes ([903eda3](https://github.com/intility/Intility.JsonApiToolkit/commit/903eda3de162e31dd4510bdcc7f0879629f59a52))
* 🚑️ refactor querying files and fix single resource relationship issues ([962d4d4](https://github.com/intility/Intility.JsonApiToolkit/commit/962d4d4a8a808ff46635456a926e4898e469bb15))
* 🚑️ reorder query processing to apply sorting before includes for better EF Core compatibility ([20bf0d9](https://github.com/intility/Intility.JsonApiToolkit/commit/20bf0d96a44b8b5e96129318fb5b6d8b07100abf))
* 🚑️ three level nested values and collection include filters ([7f9a336](https://github.com/intility/Intility.JsonApiToolkit/commit/7f9a336e2ebd8fbd4c539cb2c4293960dd172716))
* 🚑️ three level nested values and collection include filters ([044aaf0](https://github.com/intility/Intility.JsonApiToolkit/commit/044aaf0422cec41d56e7eade640903d2ab60e99d))
* 🚑️ use single query mode to prevent EF Core split query correlation issues with filtered includes ([ff48615](https://github.com/intility/Intility.JsonApiToolkit/commit/ff4861555414510011f0b04fd831ae353193cdbe))
* add defensive reflection checks with ReflectionMethodCache ([#57](https://github.com/intility/Intility.JsonApiToolkit/issues/57)) ([75eb978](https://github.com/intility/Intility.JsonApiToolkit/commit/75eb9784087d191baad5e03bcd165489eb7d7b5a))
* **mapping:** remove dead AddIncludedResourcesRecursive method ([#55](https://github.com/intility/Intility.JsonApiToolkit/issues/55)) ([bbc8c17](https://github.com/intility/Intility.JsonApiToolkit/commit/bbc8c17323fd2b4fff8dc42f37281d6eae760db1))
* **pagination:** guard against division by zero when Size is 0 ([#59](https://github.com/intility/Intility.JsonApiToolkit/issues/59)) ([0863dee](https://github.com/intility/Intility.JsonApiToolkit/commit/0863dee46fd0505d5431b816634e88f23706c5b5))
* **parsing:** guard unsafe string parsing in filter parsers ([#58](https://github.com/intility/Intility.JsonApiToolkit/issues/58)) ([9fb463d](https://github.com/intility/Intility.JsonApiToolkit/commit/9fb463d252bec6df7a31e43135ad3be0c2fe86cf))
* **security:** prevent log forging and add workflow permissions ([#51](https://github.com/intility/Intility.JsonApiToolkit/issues/51)) ([5fbbaba](https://github.com/intility/Intility.JsonApiToolkit/commit/5fbbaba6001a18577a4583ecd1c286879f8e4199))
* **security:** prevent log forging and update tooling ([#52](https://github.com/intility/Intility.JsonApiToolkit/issues/52)) ([52d73ce](https://github.com/intility/Intility.JsonApiToolkit/commit/52d73ce2efffd380fe2f2677ce0e79eb1188ac92))
* support JsonPropertyName attribute and fix many-to-many collecti… ([634abff](https://github.com/intility/Intility.JsonApiToolkit/commit/634abffd7c51878039e1ff696d6994f00d75a709))
* support JsonPropertyName attribute and fix many-to-many collection filtering ([6f1d961](https://github.com/intility/Intility.JsonApiToolkit/commit/6f1d961daadbf1bf74e9128ba3972bc3d6567bfe))


### Refactoring

* 🔨 follow ts-package renaming ([4cd1e7e](https://github.com/intility/Intility.JsonApiToolkit/commit/4cd1e7e6f29e394350077e21fcef4ecffba7a650))
* 🔨 optimize logging and add XML documentation ([8c14bc0](https://github.com/intility/Intility.JsonApiToolkit/commit/8c14bc0f97b42e0488e9768fe9a5b4bd06bf8a5f))
* 🔨 remove Microsoft.Identity.Abstractions package reference ([55933b7](https://github.com/intility/Intility.JsonApiToolkit/commit/55933b7e0cc6d3c7f8911acf2e2471e078bad819))
* 🔨 remove the OR max count ([65107d5](https://github.com/intility/Intility.JsonApiToolkit/commit/65107d570c2c83f2fb20dcd51b0b73c83b87c600))
* 🔨 remove the OR max count ([5a3aa87](https://github.com/intility/Intility.JsonApiToolkit/commit/5a3aa874b0dca67d80f5eb5c2452aeb7e8e4da2b))
* 🔨 Update JsonApiOk function and docs to align with what it actually does ([bfe7635](https://github.com/intility/Intility.JsonApiToolkit/commit/bfe76350efd945a5048b558a47d8f9082fdb1105))


### Documentation

* :memo: update stats docs ([549743c](https://github.com/intility/Intility.JsonApiToolkit/commit/549743c6210e04d45d1c6f21abb7bea203c4c333))
* 📜 add too many request exeption to docs ([872ae2a](https://github.com/intility/Intility.JsonApiToolkit/commit/872ae2a8af819d0c6d1665fda96ea3034ea76a7e))
* 📜 Clarify that filtering is only on main entity ([5ee3568](https://github.com/intility/Intility.JsonApiToolkit/commit/5ee3568ba331d7ce336fc99c98a97b0461eaa64b))
* 📜 Update Claude.md ([88502bb](https://github.com/intility/Intility.JsonApiToolkit/commit/88502bb00aeaa337afacfab6db59b2de01172225))
* 📜 update error message for forbidden includes to clarify not found status ([95ab6ce](https://github.com/intility/Intility.JsonApiToolkit/commit/95ab6ce161247701717e690e5a1d9f74242fc0c7))


### Dependencies

* **actions:** bump actions/checkout from 4 to 6 ([#47](https://github.com/intility/Intility.JsonApiToolkit/issues/47)) ([a16ab53](https://github.com/intility/Intility.JsonApiToolkit/commit/a16ab53f72fffc98cd9317681bc099e05e433f34))
* **actions:** bump actions/setup-dotnet from 4 to 5 ([#45](https://github.com/intility/Intility.JsonApiToolkit/issues/45)) ([db8c0d1](https://github.com/intility/Intility.JsonApiToolkit/commit/db8c0d1308a9abf84e37dcc35dd68205ddfafd54))
* **actions:** bump actions/upload-pages-artifact from 3 to 4 ([#44](https://github.com/intility/Intility.JsonApiToolkit/issues/44)) ([c5e35fb](https://github.com/intility/Intility.JsonApiToolkit/commit/c5e35fbfdf4e85669493f6e235c5615adf5dadc0))
* **actions:** bump github/codeql-action from 3 to 4 ([#46](https://github.com/intility/Intility.JsonApiToolkit/issues/46)) ([4bad70c](https://github.com/intility/Intility.JsonApiToolkit/commit/4bad70c94d835754974e95bf90c188bf6da5d046))
