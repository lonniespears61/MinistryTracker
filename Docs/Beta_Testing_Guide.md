# Ministry Tracker 0.9.0 Beta Testing Guide

## Important privacy rule

Use fictional test people and fictional contact details only. Do not enter real
names, addresses, phone numbers, email addresses, private notes, or congregation
information in this beta build.

The beta encrypts identifying, contact, location, demographic, and note fields
with AES-256-GCM. Its random encryption key is retained by Android secure storage.
Android cloud backup is disabled. Uninstalling the app removes the key and makes
the app database unrecoverable.

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
- Restart the app and confirm fictional students and visits remain available.
- Upgrade over a populated release build and confirm all fictional data remains.
- Confirm the app does not request background location.

## Reporting a problem

Use the in-app Feedback page or email `support@ministrytoolworks.com`. Do not
include real ministry information, database files, screenshots containing real
people, or other sensitive personal data.

Privacy policy: https://ministrytoolworks.com/privacy.html
