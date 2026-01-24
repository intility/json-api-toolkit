# Changelog

## [1.4.0](https://github.com/intility/Intility.JsonApiToolkit/compare/v1.3.1...v1.4.0) (2026-01-24)


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
* add `JsonApiOptions` for configurable query limits ([#63](https://github.com/intility/Intility.JsonApiToolkit/issues/63)) ([eb6e886](https://github.com/intility/Intility.JsonApiToolkit/commit/eb6e8869901cf10266f96c47587041ba679dc593))
* add recursion depth guard for nested collection filters ([#66](https://github.com/intility/Intility.JsonApiToolkit/issues/66)) ([36e2c5a](https://github.com/intility/Intility.JsonApiToolkit/commit/36e2c5a75f99d57375ec121f7133ab98ac87b878))
* **errors:** add JsonApiErrorCodes and JsonApiErrors factory methods ([#60](https://github.com/intility/Intility.JsonApiToolkit/issues/60)) ([8531ad3](https://github.com/intility/Intility.JsonApiToolkit/commit/8531ad39acd3d7b1b64f5e7d4d79e63731752d01))
* **errors:** complete refactor Phase 1 with exception filter tests a… ([#61](https://github.com/intility/Intility.JsonApiToolkit/issues/61)) ([e5b50be](https://github.com/intility/Intility.JsonApiToolkit/commit/e5b50be0482b8565c5fc477efaf5bece82d3ec00))
* validate filter paths against `AllowedIncludes` ([#65](https://github.com/intility/Intility.JsonApiToolkit/issues/65)) ([df411fe](https://github.com/intility/Intility.JsonApiToolkit/commit/df411fe6b0da25b7dffae36303b7adf1d3b4b485))


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
* **ci:** read version from csproj instead of parsing tag ([a881cba](https://github.com/intility/Intility.JsonApiToolkit/commit/a881cba744df51cc6a419e9f06643aa054e0391b))
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
