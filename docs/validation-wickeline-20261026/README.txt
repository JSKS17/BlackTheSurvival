Wickeline 1.0.27 release helpers

Baseline: game1.0.26 / Site53 / environment55
Site source: 3618d496050cce352223ca1098b4ea43b1f0dc4d
Build ID: 75e2024a587fc1546e8b2861
Canonical manifest SHA256:
69d87aa28379352f3ba29eefada716159fd299ac3c9ff59d050f4414519aee0f
baseline-provenance.json records eight canonical hashes and old proof/helper
inputs. Seven protected web source/dependency files remain unchanged.

Paths (commands run from C:\Users\azxar\BlackTheSurvival)
  Proofs: docs/validation-wickeline-20261026
  Raw: Build/Web127
  Web clone: Build/UnityWebBuild
  Site stage: Temp/SiteWickeline20261026
  Isolated package/uploader: Temp/WickelineRelease20261026
  Preview: http://127.0.0.1:8248/qa?fixture=NAME
  Explicit pwsh7:
    C:\Users\azxar\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\powershell\pwsh.exe

Preparation performed only new-docs writes, local reads, syntax validation and
read-only public GitHub inspection. No root source/version/git edits, build,
server, release preparation, production write, upload or cleanup ran.

Root execution status (latest root update, 2026-10-26)
  Final web Site55 deployment Succeeded; 21 live checks PASS, canonical sync
  PASS and actual-game browser QA PASS with physical-hardware limitations.
  Focused source tests:369; full source tests:255768 assertions PASS.
  Root started Windows via StartBuild -Target Windows at 17:20:48 UTC,
  PID9208, reusing the frozen shared clone. Windows/GitHub completion is
  recorded separately after the production build, ZIP validation and upload.

Root-owned source freeze
  FreezeSources.mjs is a template for root execution after implementation,
  tests and art paths settle. It writes source-validation.json once (wx).
  First write art-source-paths.json as a nonempty array of workspace-relative
  forward-slash paths. Include every new/changed runtime image and its meta,
  plus any art provenance/reference document root chooses to freeze. Root's
  agreed list includes all ArtSource/Wickeline20261026 files, four runtime
  PNG/meta files, and Assets/Editor/WickelineReleaseBuild.cs plus its meta.
  This map is asset/build provenance, so it may include the Editor fixture.
  The freeze creates this schema:
    result:PASS, version:1.0.27, previousVersion:1.0.26, runtimeSources:N
    files:{all runtime C# paths plus ProjectSettings and chosen docs:sha256}
    testSourceHashes:{actual Tools input paths:sha256}
    artSourceHashes:{all explicit art-source-paths.json entries:sha256}
    changedRuntimeFiles:[changed/added/removed basenames compared with1.0.26]
  Counts are dynamic. SourceSchema.mjs checks the previous proof at
  docs/validation-skill-references-20261025/source-validation.json.
  RecordCoreResults.mjs checks the exact unique tested input set: runtime C#
  plus all frozen testSourceHashes. It uses no previous task count subtraction
  or subject/skill totals. Root owns core-tests.log/source-manifest.json.

Validation and web release order
  1. node docs/validation-wickeline-20261026/ValidateHelpers.mjs
     Syntax/provenance only; never invokes server/build/release/cleanup modes.
  2. Root freezes source/art and runs tests/builds. Then:
     node docs/validation-wickeline-20261026/ValidateBuildSnapshot.mjs
     All frozen root, Tools and art hashes are checked. Runtime C# file set and
     bytes match clone. Every frozen Assets/* art/meta also matches clone.
     The prior seven exact WebGL overrides and exact optional preload asset
     are the only accepted clone PlayerSettings differences.
  3. Root saves successful raw unity-build-manifest.json, then:
     node docs/validation-wickeline-20261026/PrepareRelease.mjs prepare
     Stage keeps byte-exact old1.0.26 game while adding only new immutable R2
     records. Root packages/saves/publishes the bridge and writes bridge-state.json.
  4. node docs/validation-wickeline-20261026/VerifyBridge.mjs
  5. Root uploads with the unchanged isolated uploader, supplying credentials
     through hidden stdin only; root saves upload proof and closes upload gate.
  6. node docs/validation-wickeline-20261026/PrepareRelease.mjs finalize
     Root packages/saves/publishes final stage and writes publish-state.json.
  7. node docs/validation-wickeline-20261026/VerifyLive.mjs
     Root completes browser-validation.json (PASS, gameVersion1.0.27,
     physicalHardwareTested:false, nonempty screenshot filenames inside here).
  8. node docs/validation-wickeline-20261026/PrepareRelease.mjs sync
  9. After owned processes stop and all cleanup gates pass:
     & 'C:\Users\azxar\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\powershell\pwsh.exe' -NoProfile -File docs/validation-wickeline-20261026/Cleanup.ps1 -VerifiedServersStopped
 10. node docs/validation-wickeline-20261026/Complete.mjs
     Final local web audit consumes frozen maps, source snapshot, tests, build,
     browser, live, publication, sync, protected web hashes and cleanup proofs.
     Art map/count/clone coverage is included. It writes completion.json only
     after required proofs exist. GitHub publication has its own proof below.

Preview fixtures
  node docs/validation-wickeline-20261026/Preview.mjs
  research.json is copied byte-exact from the prior profile. Root/engine agent
  supply Wickeline raw-run fixtures after the interface is finalized. Names
  [a-z][a-z0-9-]{0,63}.json are discovered on page reload; no hardcoded list.
  Root's WickelineReleaseBuild.Build exports six actual-engine fixtures when
  passed -lumia-fixture-output <absolute proof fixtures directory>, then calls
  the normal Web builder. The Preview discovers these without edits.
  Default is wickeline-combat if supplied. &mobile=1 models browser detection;
  --baseline serves exact canonical1.0.26 and may read listed public R2 assets.
  No preview server was started during helper preparation.

Windows build review (updated to root's shared-clone decision)
  Root uses StartBuild.ps1 in this validation directory:
    & 'C:\Users\azxar\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\powershell\pwsh.exe' -NoProfile -File docs/validation-wickeline-20261026/StartBuild.ps1 -Target Windows
  It verifies frozen root/art/Tools hashes, checks no owned Unity clone is
  open, requires the Web snapshot proof and verifies cloned Assets hashes.
  It reuses Build/UnityWebBuild, launches hidden Unity with StandaloneWindows64,
  Lumia.Editor.LumiaProjectSetup.BuildWindows, --burst-disable-compilation,
  one job/import worker, BEE_BUILD_THREADS=1 and the exact source freeze path
  supplied via -lumia-source-capture. It returns the PID without waiting.
  Log: docs/validation-wickeline-20261026/unity-windows.log
  Process proof: docs/validation-wickeline-20261026/build-process-windows.json
  Shared-clone Windows output is Build/UnityWebBuild/Build/Windows and its
  manifest is Build/UnityWebBuild/Build/windows-build-manifest.json.
  Root waits for exit0/Succeeded and verifies frozen source/art matches before
  preparing the complete ZIP. No development flag is supplied. StartBuild's
  Web target uses WickelineReleaseBuild.Build and exports actual-engine QA
  fixtures; do not run a Web and Windows target concurrently on this clone.

GitHub publication helper (root alone runs mutation mode)
  PublishGitHubRelease.ps1 requires pwsh7 and an already pushed remote v1.0.27
  tag resolving to the exact -SourceCommit. It never commits, pushes or tags.
  Required paths: -ZipPath, -ChecksumsPath, -NotesPath, -WindowsManifestPath.
  SourceFreezePath defaults to this folder's source-validation.json.
  Asset names must be:
    BlackTheSurvival-v1.0.27-Windows-x64.zip
    SHA256SUMS-v1.0.27.txt
  ZIP contains the complete Windows player at its root. Checksums file is one
  lowercase SHA256, two spaces, exact ZIP filename. Windows manifest must be
  Succeeded/version1.0.27/errors0/developmentfalse and its sourceCaptureSha256
  must equal the final source freeze proof SHA256.
  -Mode Inspect validates local inputs and reads tag/release; writes nothing.
  -Mode Publish resumes/creates draft, uploads two assets, checks server SHA256
  digests, publishes and saves github-publish-state.json (public metadata only).
  Secrets come from optional in-memory SecureString -Token or captured GCM
  credential fill output. No secret, header or raw error response is logged.
  Existing mismatched assets are never deleted/replaced. Failure leaves draft
  for inspection. An already published matching release is verified idempotently.
  Root should finish both GitHub assets before removing the local Windows ZIP.

Cleanup boundaries
  Web helper accepts only root Build children UnityWebBuild/Web127/LumiaWebBuild.log.
  Five targets must exist for its exact removal proof: Build, owned Site stage,
  owned release Temp, this folder/site.tar.gz and this folder/bridge.tar.gz.
  The dependency junction is unlinked only when it targets canonical modules.
  pwsh7/CIM access is required. No process is stopped by Cleanup.ps1.
  Root's shared-clone Windows output lies inside Build and is covered by that
  whole-tree target. If the older Temp-based launcher is used instead, its
  Temp/UnityBuildVerification and Temp/LumiaBuild.log need a separate owned
  cleanup proof. Preserve Windows build/ZIP evidence and upload assets before
  deleting the shared clone or local release package.
  Preserve prior proof folders, root source, art, dependencies and git state.
