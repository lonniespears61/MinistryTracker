# Beta release failure triage - 2026-06-26

## Summary

Internal testing release `0.9.0-beta.2` / version code `3` was published to Google Play and installed successfully, but the app crashed on real-device interaction. The failure was not caused by Play Console setup, tester configuration, Google Maps API credentials, or normal application business logic.

The evidence points to Android Release packaging/runtime settings. The stable rescue build is `0.9.0-beta.3` / version code `4`, with Release Android packaging changed to embed assemblies and disable the aggressive Release runtime optimizations for this beta.

## User-visible failure

- The app launched from the Play internal testing build.
- Any meaningful interaction, including opening screens or tapping buttons, crashed the app.
- A local Release APK built with the original-style Release behavior reproduced the same class of crash.
- A corrected Debug APK with assemblies embedded opened and allowed the Add Student flow.
- A corrected Release APK with safer packaging/runtime settings opened and no longer crashed during the same manual interaction test.

## Device/build context

- Package: `com.ministrytoolworks.tracker`
- Failed Play release: `0.9.0-beta.2`, version code `3`
- Rescue release: `0.9.0-beta.3`, version code `4`
- Test device seen over USB: `T901_US`
- Device build fingerprint seen in crash logs: `Relndoo/T901_US/T901_US:15/AP3A.240905.015.A2/20250225SMR2:user/release-keys`
- Android target SDK in generated manifest: `35`

## Crash evidence

The Play/internal Release build produced native Android crashes, not managed C# exceptions:

- `Fatal signal 11 (SIGSEGV)` on `RenderThread`
- `FORTIFY: pthread_mutex_lock called on a destroyed mutex`
- Later reproduction showed `Fatal signal 6 (SIGABRT)`

No useful managed `.NET` exception stack appeared. That strongly suggests a native/runtime/package interaction rather than a ViewModel validation or app data exception.

The first local Debug test crashed immediately with:

```text
No assemblies found in '/data/user/0/com.ministrytoolworks.tracker/files/.__override__/arm64-v8a' ... Assuming this is part of Fast Deployment. Exiting...
```

That specific Debug crash was a local deployment issue. Rebuilding Debug with embedded assemblies fixed it and proved the app itself could run and navigate on the device.

## Things ruled out

- Play Console tester setup: the internal track was active and available to testers.
- Google Maps key restrictions: the app launched and the map rendered after the rescue build.
- OAuth consent warning: irrelevant because the app uses an API key, not Google sign-in.
- Add Student form logic: Debug with embedded assemblies opened Add Student successfully.
- CommunityToolkit Expander: temporarily removing it did not fix the Release crash.
- Android hardware acceleration: disabling it did not fix the Release crash.
- Unit-test-covered app logic: 39 Release tests passed before and after; the failure lived outside those tests.

## Most likely root cause

The failure was caused by the Android Release build/runtime packaging profile used for `0.9.0-beta.2`, especially the combination of Release assembly packaging, linking/trimming, and AOT/profiled-AOT behavior on this device.

The stable rescue profile explicitly sets:

- `EmbedAssembliesIntoApk=true`
- `AndroidEnableProfiledAot=false`
- `RunAOTCompilation=false`
- `PublishTrimmed=false`
- `AndroidLinkMode=None`

This is intentionally conservative. It makes the beta artifact larger, but it removes the risky optimization path that produced native crashes during early field testing.

## Tradeoff accepted for beta

The safer build is larger than the original Release bundle. That is acceptable for an internal beta because stability is more important than bundle size while we validate core workflows.

Once beta behavior is stable across devices, optimization can be reintroduced one setting at a time with a device smoke test after each change.

## Prevention checklist

Before uploading any future Play beta:

1. Clean-build the Android Release APK and AAB.
2. Install the exact Release APK locally on a USB device.
3. Smoke test:
   - App launch
   - Accept beta/start flow
   - Dashboard buttons
   - Students tab
   - Add Student screen
   - Calendar tab
   - Settings tab
   - Map screen
4. Pull crash logs immediately after any crash.
5. Upload to Play only after the local Release APK survives the smoke test.
6. Keep version code increasing for every Play upload.
7. Do not rely on unit tests alone for Android Release readiness; they do not cover native packaging/runtime behavior.

## Follow-up items

- Keep the safer Release settings for `0.9.0-beta.3`.
- Add Android debug symbols/deobfuscation later to improve native crash visibility.
- Revisit size optimization only after the beta is stable on multiple devices.
- Investigate the map defaulting to Hawaii separately; it is not the crash root cause.
