# Ministry Tracker 0.9.0-beta.1 Release Record

## Automated release gates

- Repository workflow and rollback tests
- Encryption round-trip, tamper, wrong-key, and plaintext-removal tests
- Legacy database migration and restart preservation tests
- NuGet vulnerability scan
- Android Debug build with zero warnings and errors
- Signed Android Release publish with zero warnings and errors

## Device acceptance

Device-only results are recorded against `Docs/Beta_Flow_Matrix.md`.

| Check | Result |
|---|---|
| Samsung device detected | Pass — SM-S916U1, Android 16 |
| Production-signed replacement install | Pass — 0.9.0-beta.1, version code 2 |
| Same-signature reinstall/upgrade | Pass — first-install time preserved; secure-key startup passed |
| S1-S10 scheduling contracts | Pass — automated repository, route, conflict, and rollback coverage |
| V1-V6 visit contracts | Pass — automated policy, lifecycle, and transaction coverage |
| Cold start, rotation, background/resume | Pass — process remained healthy; no crash-buffer entries |
| Permission denial and restart | Pass — app cold-started with coarse/fine location denied |
| Backup and background location containment | Pass — no backup flag and no background-location request |
| Human exploratory UI pass | Required during beta using fictional data |

The pre-beta app on the Samsung used an unrelated signing certificate. Android
correctly rejected an in-place update. Its private data could not be exported
because the release was non-debuggable and backup was disabled. With user
authorization, that build was uninstalled and replaced by the durable
Ministry Toolworks-signed beta.

## Security design

- Sensitive fields are encrypted individually with AES-256-GCM.
- A fresh random 256-bit key is stored through the platform secure-storage API.
- Existing plaintext rows migrate automatically to schema version 2.
- Migration clears legacy columns, truncates WAL content, and vacuums free pages.
- A protected verifier rejects missing, changed, or incorrect device keys.
- Android backup and background-location access are disabled.
