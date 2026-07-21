# How launcher versions reach Gml.Backend admin panel

## Source

Backend lists/clones from:

- Owner: `Nik497926`
- Repo: `Gml.Launcher`
- Clone: `https://github.com/Nik497926/Gml.Launcher.git`

Configured in Api: `LauncherGitHubDefaults`.

## Auto publish

On every push to `master` in [Gml.Launcher](https://github.com/Nik497926/Gml.Launcher):

1. CI builds Windows / Linux / OSX binaries (`ci.yml`).
2. Creates git tag `vYYYY.M.D.<run>` + GitHub Release with zip.
3. Backend panel → Integrations → launcher versions picks those tags (plus `master`).

## Admin flow

1. Download tag/master (git clone on the API server).
2. Compile for selected OS.
3. Upload build via `/api/v1/launcher/upload`.
