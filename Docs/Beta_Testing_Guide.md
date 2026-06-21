# Ministry Tracker 0.9.0 Beta Testing Guide

## Real-world beta use

Approved beta testers should use the app during normal, real-life ministry so
the workflow is tested honestly. Enter only the information reasonably needed
to remember and arrange your own return visits. Avoid unnecessary sensitive
details, congregation records, confidential health or financial information,
and information unrelated to that purpose.

The beta encrypts identifying, contact, location, demographic, and note fields
with AES-256-GCM. Its random encryption key is retained by Android secure storage.
Android cloud backup is disabled. Uninstalling the app, clearing app storage,
losing the device, or losing the secure encryption key makes the app database
unrecoverable. Keep essential information separately.

## Install and upgrade

1. Keep the supplied APK private to the approved beta group.
2. Install it over an earlier Ministry Toolworks release build to test data
   preservation. If Android reports a signing conflict with a Visual Studio
   debug build, uninstall that debug build first; Android cannot upgrade between
   unrelated signing certificates.
3. Open the app and accept the beta agreement.
4. Complete the flows in `Docs/Beta_Flow_Matrix.md`.

## Required acceptance checks

- Run scheduling flows S1-S10 and visit flows V1-V6.
- Test toolbar Cancel, page Back, and Android system Back.
- Rotate Calendar, Add Visit, and Update Visit while work is pending.
- Background and resume during Add Visit; confirm one visit is saved.
- Deny location permission and confirm Add Student remains usable.
- Restart the app and confirm students and visits remain available.
- Upgrade over a populated release build and confirm the data remains.
- Confirm the app does not request background location.

## Reporting a problem

Use the in-app Feedback page or email `support@ministrytoolworks.com`. Describe
problems with redacted or invented examples. Never send names, addresses, phone
numbers, email addresses, private notes, database files, or identifiable
screenshots.

Privacy policy: https://ministrytoolworks.com/privacy.html
