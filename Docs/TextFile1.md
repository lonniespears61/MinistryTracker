# MinistryTracker – Pageflow Map (v0)

## Root (Shell)
- AppShell
  - DashboardPage (root)
  - StudentsListPage
  - SettingsPage
  - AboutPage

## DashboardPage
- Tap: "Active Students" card -> StudentsListPage
- Tap: About icon -> AboutPage
- Tap: Settings icon -> SettingsPage
- Tap: "Next Weeks Calls" -> VisitsCalendar

## StudentsListPage
- Tap student row -> StudentProfilePage (studentId param)
- Swipe Left: Edit -> EditStudentPage (studentId param)
- Swipe Right: Add Visit -> AddVisitPage (studentId param)
- Tap Add -> AddStudentPage
- Tap: Home icon -> Dashboard

## StudentProfilePage
- Tap Edit button -> EditStudentPage (studentId)
- Tap Add Visit -> AddVisitPage (studentId)
- Back/Home -> StudentsListPage (or Dashboard depending on UX)

## EditStudentPage
- Save -> back to StudentProfilePage (refresh)
- Cancel -> back

## AddVisitPage
- Save -> back to StudentProfilePage (refresh)
- Cancel -> back

## UpdateVisitPage
- Save ->  VisitsCalendar
- Cancel -> back

## VisitsCalendar
- Tap CalendarEntry - add visit (if empty)
- Tap CalendarEntry - Update Visit (if not empty)
- Cancel -> backMinistryTracker – Pageflow Map (v0.1)
Root (Shell)

AppShell

DashboardPage (root)

StudentsListPage

SettingsPage

AboutPage

MyCalendarPage (placeholder – not built yet)

DashboardPage

Tap: “Active Students” card -> StudentsListPage

Tap: About icon -> AboutPage

Tap: Settings icon -> SettingsPage

Tap: “My Calendar” / “Upcoming Calls” -> MyCalendarPage (placeholder – not built yet)

StudentsListPage

Tap student row -> StudentProfilePage (studentId param via Shell route)

Swipe Left: Edit -> EditStudentPage (studentId param via Shell route)

Swipe Right: Add Visit -> AddVisitPage (studentId param via Shell route — placeholder page if not built)

Tap Add -> AddStudentPage (placeholder if not built)

Tap Home icon -> DashboardPage (or Shell nav to dashboard)

StudentProfilePage

Tap Edit -> EditStudentPage (studentId)

Tap Add Visit -> AddVisitPage (studentId — placeholder if not built)

Back/Home -> StudentsListPage (or DashboardPage depending on UX choice)

EditStudentPage

Save -> back to StudentProfilePage (refresh student)

Cancel -> back

AddStudentPage (placeholder if not built)

Save -> StudentsListPage (refresh list)

Cancel -> back

AddVisitPage (placeholder if not built)

Save -> back to StudentProfilePage (refresh visits section later)

Cancel -> back

UpdateVisitPage (placeholder – not built yet)

Save -> MyCalendarPage

Cancel -> back

MyCalendarPage (placeholder – not built yet)

Shows: scheduled visits across all students (future)

Tap visit -> UpdateVisitPage (visitId)

Tap Add -> AddVisitPage (studentId optional)

AboutPage

Home icon -> DashboardPage (Shell nav to dashboard)

SettingsPage

Home icon -> DashboardPage (Shell nav to dashboard)

## AboutPage
- cancel -> Back
## SettingsPage
