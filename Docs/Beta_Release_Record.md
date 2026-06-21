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
| Samsung device detected | Pending |
| Release upgrade/install | Pending |
| S1-S10 scheduling flows | Pending |
| V1-V6 visit flows | Pending |
| Back, cancel, rotation, background/resume | Pending |
| Permission denial and restart preservation | Pending |

## Security design

- Sensitive fields are encrypted individually with AES-256-GCM.
- A fresh random 256-bit key is stored through the platform secure-storage API.
- Existing plaintext rows migrate automatically to schema version 2.
- Migration clears legacy columns, truncates WAL content, and vacuums free pages.
- A protected verifier rejects missing, changed, or incorrect device keys.
- Android backup and background-location access are disabled.
