# Ministry Tracker Beta Flow Matrix

This matrix is the release checklist for workflow behavior. Repository, rollback,
policy, route, and database-preservation rules are automated under
`Tests/MinistryTracker.Tests`. Device-only interactions remain manual acceptance
tests.

## Scheduling flows

| ID | Start | Action | Expected success destination | Cancel / Back destination | Automated coverage |
|---|---|---|---|---|---|
| S1 | Dashboard follow-up | Schedule known student for today | Dashboard | Dashboard | Route + repository |
| S2 | Dashboard overdue | Schedule known student for today | Dashboard | Dashboard | Route + repository |
| S3 | Student list swipe | Choose student, date, and save | Dashboard | Calendar | Policy + route |
| S4 | Student profile | Choose date and save | Dashboard | Calendar | Policy + route |
| S5 | Add Student prompt | Save student, choose Yes, schedule visit | Dashboard | Calendar | Route contract |
| S6 | Calendar normal mode | Choose date, choose student, save | Dashboard | Calendar | Route contract |
| S7 | Update Visit | Save & Schedule Next, choose date, save | Dashboard | Calendar | Policy + route |
| S8 | Existing future visit conflict | Keep existing | Stay at origin | N/A | Repository rule |
| S9 | Existing future visit conflict | Edit existing | Update Visit | Origin via Back | Route contract |
| S10 | Existing future visit conflict | Replace, then save | Dashboard; original canceled atomically | Calendar; original unchanged | Rollback test |

## Existing visit flows

| ID | Visit state | Allowed actions | Expected terminal behavior | Automated coverage |
|---|---|---|---|---|
| V1 | Future Scheduled | Edit, Cancel, Reschedule | Save/Cancel/Reschedule returns to Student Profile | Policy + rollback |
| V2 | Past Scheduled | Edit, mark Successful, mark Missed | Outcome remains on Update Visit for next-step choice | Policy + transaction |
| V3 | Successful | Edit, correct outcome, schedule next | Save returns to profile; next scheduling follows S7 | Policy + transaction |
| V4 | Missed | Edit, correct outcome, reschedule, schedule next | Reschedule returns to profile | Policy + rollback |
| V5 | Canceled | Edit details, schedule next | Save returns to profile | Policy |
| V6 | Rescheduled history | Read-only | Back returns to origin | Policy |

## Student status flows

| Student state | Scheduling behavior | Expected UI |
|---|---|---|
| Active | Allowed | Scheduling controls enabled |
| Paused | Blocked | Guidance says to reactivate |
| Completed | Blocked | Guidance says to reopen as Active |
| Discontinued | Blocked | Guidance says to reopen as Active |
| Deleted / missing | Blocked | Student-not-found message |

## Manual device acceptance

- Run S1–S10 and V1–V6 on a Release build.
- For every Add Visit page, test toolbar Cancel, Shell Back, and Android system Back.
- Rotate the device on Calendar, Add Visit, and Update Visit; verify pending context is unchanged.
- Background and resume the app during Add Visit; verify no duplicate save occurs.
- Change calendar months and confirm no agenda from the prior month remains selected.
- Deny location permission, retry, and confirm Add Student remains usable.
- Kill and restart the app after saving; verify students, visits, statuses, and links persist.
- Install a newer APK over a populated beta database and rerun preservation checks.

## Release gate

- `dotnet test Tests/MinistryTracker.Tests/MinistryTracker.Tests.csproj`
- Android Release build completes with zero errors and zero warnings.
- All manual device acceptance rows pass on at least one supported Samsung device.
