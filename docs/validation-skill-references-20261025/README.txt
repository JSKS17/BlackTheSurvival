Skill reference audit release helpers / game 1.0.26

This directory contains local release and QA helpers adapted from the preserved
1.0.25 release. Helper preparation performs no build, server, network request,
upload, publication, cleanup, version change, runtime edit or old proof edit.
Root owns the source freeze, build settings/export, native Sites calls,
credentials, upload and all production actions.

Observed local baseline (corroborated by preserved completion/publish-state)
  Game: 1.0.25 / build ID 0f171eb9c2bb8ec4c369e905
  Site: 51 / environment revision 53
  Commit: 24f431c9fdbb006f28a476aff6302ba41e89054d
  Canonical manifest SHA256:
    4f2bdd240435265a6ad38e49e67469f478664109463f91535c0603de1d147373
  baseline-provenance.json records all eight observed canonical hashes and the
  untouched old helper/source-proof inputs. Root verifies live Site identity.

Paths
  Proofs: docs/validation-skill-references-20261025
  Raw export: Build/Web126
  Clone: Build/UnityWebBuild
  Native Sites checkout: Temp/SiteSkillReferences20261025
  Isolated package/uploader: Temp/SkillReferencesRelease20261025

Run commands from C:\Users\azxar\BlackTheSurvival.

Preparation / offline checks
  node docs/validation-skill-references-20261025/ValidateHelpers.mjs
  It checks module syntax, parses Cleanup.ps1 with explicit pwsh 7, checks
  embedded QA script syntax and confirms local baseline/fixture provenance.
  It executes none of the release, snapshot, server or cleanup helpers.
  CreateHelpers.mjs is the one-time adapter. It refuses to overwrite a
  different existing helper; ordinary work uses the generated files directly.

Source freeze schema (root produces this after all intended edits)
  source-validation.json:
    result: "PASS"
    version: "1.0.26"
    previousVersion: "1.0.25"
    runtimeSources: N
    changedRuntimeFiles: ["Changed.cs", "Added.cs", "Removed.cs", ...]
    files: {"Assets/Scripts/Lumia/File.cs":"64-lowercase-hex-SHA256", ...,
            "ProjectSettings/ProjectSettings.asset":"SHA256"}
    testSourceHashes: {"Tools/Test.cs":"SHA256", "Tools/RunCoreTests.ps1":"SHA256", ...}
  Paths are workspace-relative with forward slashes. Include every current
  runtime C# file directly in Assets/Scripts/Lumia. N must equal that actual
  subset of files. Root/Tools counts are derived from the actual maps; there
  are no fixed runtime, changed-file, root-file or Tools-file counts.
  changedRuntimeFiles contains the sorted basename delta over the union of
  old and new runtime paths, including additions and removals, compared with
  docs/validation-player-feedback-20261023/source-validation.json.files.
  Optional touchScrollUnchanged/mobileTypographyUnchanged fields, if given,
  must match those actual comparisons. They are reported without assuming
  the old task's result. SourceSchema.mjs implements this shared contract.

Snapshot check (root runs before cleanup, once the isolated clone exists)
  node docs/validation-skill-references-20261025/ValidateBuildSnapshot.mjs
  All frozen root/Tools hashes and all actual runtime filenames/hashes must
  match; clone runtime matches the same map. PlayerSettings must equal root
  with only the existing standard isolated WebGL overrides:
    WebGL batching 0/0; hashed filenames; PROJECT:BlackTheSurvival template;
    Gzip; decompression fallback; initial memory 256 MiB; WebGL IL2CPP backend.
  Root Android 1 / Standalone 0 values remain in the comparison. The exact
  temporary preload fileID -944628639613478452, guid
  2bcd2660ca9b64942af0de543d8d7100, type 3 is also accepted during export.
  No other settings difference is accepted. The helper writes only
  build-source-validation.json, with dynamic counts/maps and changed list.

Local browser QA (root starts the server and performs interactions)
  research.json and yuki-combat.json were copied byte-exact from the prior
  valid research profile and Yuki W/E/Aya E combat fixture. Their provenance
  appears in baseline-provenance.json. No old fixture/proof was changed.
  Add representative raw-run fixture JSONs to this directory's fixtures/.
  Safe names match [a-z][a-z0-9-]{0,63}.json and appear without code changes.
  node docs/validation-skill-references-20261025/Preview.mjs
  Open http://127.0.0.1:8247/qa?fixture=yuki-combat
  Other fixtures use ?fixture=NAME. Default is yuki-combat when present.
  Add &mobile=1 for the local detection model. Add --baseline to the node
  command for exact canonical 1.0.25 comparison. Baseline mode may stream
  only listed immutable public R2 assets, with no account requests or cache.
  Ordinary preview reads Build/Web126. It imports a new local QA bundle per
  scene reload through the game bridge. Mobile modeling is not phone hardware.

Standard two-stage release order (root owns native Sites and upload actions)
  1. Root verifies live baseline, checks out the official Site, freezes sources,
     builds the isolated Unity export, and saves unity-build-manifest.json.
     Run ValidateBuildSnapshot before cleanup; finish core/browser evidence.
  2. node docs/validation-skill-references-20261025/PrepareRelease.mjs prepare
     Packages Web126 separately. Stage serves byte-exact canonical 1.0.25
     public game/metadata while its manifest adds only new immutable R2 records.
  3. Root builds/packages/saves/publishes the bridge and saves the native
     package/saved/deployment objects together as bridge-state.json.
  4. node docs/validation-skill-references-20261025/VerifyBridge.mjs
  5. Root uploads with the unchanged isolated uploader, using credentials only
     through hidden stdin, saves upload-validation.json, and closes the upload
     gate for final deployment. Root handles all secrets outside these proofs.
  6. node docs/validation-skill-references-20261025/PrepareRelease.mjs finalize
  7. Root builds/packages/saves/publishes final stage and writes publish-state.json.
  8. node docs/validation-skill-references-20261025/VerifyLive.mjs
     All fixed public/account-isolation/R2 checks and current client asset
     checks must pass. Compiled chunk counts are not assumed in Complete.
  9. node docs/validation-skill-references-20261025/PrepareRelease.mjs sync
     Requires final publication/live PASS and synchronizes canonical game and
     manifest by checked renames with rollback. Existing web source is protected.
 10. Root stops owned processes, verifies source/live/browser/sync PASS, then:
     & 'C:\Users\azxar\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\powershell\pwsh.exe' -NoProfile -File docs/validation-skill-references-20261025/Cleanup.ps1 -VerifiedServersStopped
     Cleanup explicitly requires PowerShell 7. Its mandatory CIM read needs a
     context that permits Win32_Process inspection. It never stops processes.
 11. node docs/validation-skill-references-20261025/Complete.mjs

Additional final proof schemas
  core-tests.json:
    result:"PASS", assertions:positive integer, sourceHashes:frozen.files,
    testSourceHashes:frozen.testSourceHashes; version may be recorded as 1.0.26.
  unity-build-manifest.json:
    exact saved raw export report, result:"Succeeded", productVersion:"1.0.26",
    errors:0 (the raw report must remain byte-identical for cleanup ownership).
  browser-validation.json:
    result:"PASS", gameVersion:"1.0.26", physicalHardwareTested:false,
    screenshots:["relative-screenshot.jpg", ...] (nonempty, inside this proof dir).
  bridge-state.json and publish-state.json:
    {package:{project_id,checkout_path,commit_sha,...},
     saved:{id,project_id,version_number,source:{commit_sha},...},
     deployment:{id,project_id,version_id,status:"succeeded",env_set_revision,...}}
    Native project ID remains appgprj_6ad077cf90708191a3e75367b4b3b78c.
    Final version must follow baseline Site51. Version/source/deployment IDs
    must agree across native objects and the generated live/sync proofs.
  VerifyLive writes live-validation.json; PrepareRelease writes
  release-prepare/finalize/sync.json. Complete consumes the source snapshot,
  core, build, browser, live, publication, sync, helper baseline and cleanup
  proofs; it writes completion.json only when the final local audit passes.

Cleanup scope
  Build is disposable only with direct children within the exact whitelist
  UnityWebBuild, Web126, LumiaWebBuild.log. Web126 must match the exact saved
  successful raw manifest. Unexpected future test outputs abort cleanup until
  root inspects and explicitly adds their ownership proof; no broad deletion.
  Stage node_modules is unlinked only as a Junction to canonical node_modules;
  other links abort. Canonical source, art, dependencies and all prior proofs stay.
  The five generated cleanup targets are Build, owned stage, owned release,
  this proof directory's site.tar.gz and bridge.tar.gz. Complete requires all
  five recorded removals, absentTargets:[], and all five still absent afterward.
  Root must package both archives at those exact paths before final cleanup.
