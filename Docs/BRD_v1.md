# MinistryTracker Business Requirements Document (BRD v1)

## Purpose
MinistryTracker is a mobile app for Jehovah’s Witnesses to track Bible students and their visits.  
This BRD defines the scope, requirements, and key features.

## Core Modules
1. **Dashboard**
   - Show number of active students
   - Show visits scheduled this week
   - Show next scheduled visit

2. **Students**
   - List students (search, filter, swipe for actions)
   - Add new student (required + optional fields)
   - Edit existing student
   - View student profile with visits, status, household, tags
   - Change student status (active, discontinued, etc.)

3. **Visits**
   - Add new visit linked to a student
   - Edit existing visit
   - View all visits for a student
   - Calendar view: monthly/agenda

4. **Settings**
   - Adjust preferences (theme, defaults)
   - Export/import data

5. **About**
   - Show app name, version, and info

6. **Navigation**
   - Bottom Tabbed Notifications (Dashboard, Students, Calendar, Settings, About)
# MinistryTracker — Mapping, Location, and Unscheduled Visits

## 7. Mapping, Location, and Unscheduled Visits

### 7.1 Design Principles

- Student location represents the **default / usual** meeting location.
- A Visit may optionally override the location **for that visit only**.
- GPS is captured naturally during real ministry activity, not forced at data entry.
- No saved location is overwritten without explicit user confirmation.
- Navigation is delegated to the device’s native mapping application.

---

### 7.2 Directions to Student or Visit Location

**Requirement**  
The system shall allow the user to open turn-by-turn directions using the device’s default mapping app.

**Resolution Order**
1. Visit override location (if present)
2. Student GPS coordinates
3. Student address
4. No location → display warning

**Available From**
- Student Profile
- Visit Detail
- Calendar Agenda Item

**Non-Goals**
- In-app navigation
- Route preview or ETA
- Live tracking

---

### 7.3 Capture GPS on First In-Person Visit

**User Story**  
As a publisher, when I complete my first in-person visit at a location, I want the app to offer to save GPS coordinates so future visits and directions are accurate.

**Behavior**
- On saving a completed in-person visit:
  - If the student has no GPS:
    - Prompt: “Save this location for future visits?”
- If confirmed:
  - Save GPS to the Student record
- If declined:
  - Save the visit only

**Rules**
- No prompt if GPS already exists
- No GPS capture for phone, letter, or remote visits

---

### 7.4 Update Student Default Location

**User Story**  
As a publisher, if a student’s usual meeting location changes, I want to update the saved location for future visits.

**Capabilities**
- Update GPS from current location
- Edit address manually
- Clear GPS (fallback to address)

**Confirmation**
- Overwriting existing GPS requires explicit confirmation

---

### 7.5 One-Time Visit Location Override (Planned)

**User Story**  
As a publisher, when a specific visit is held somewhere different, I want to set a one-time location without changing the student’s usual location.

**Behavior**
- Visit may specify a one-time location override
- Directions for that visit use the override
- Student location remains unchanged

---

### 7.6 Students Near Me (Map View)

**User Story**  
As a publisher, I want a map showing nearby students so I can make last-minute visits.

**Behavior**
- Map centered on current location (permission-based)
- Pins for students with GPS coordinates
- Pin actions:
  - Directions
  - Log Visit
  - Open Student Profile

**Constraints**
- No background tracking
- No automatic address geocoding (future enhancement)

---

### 7.7 Unscheduled Visit Logging

**User Story**  
As a publisher, when a conversation happens without a scheduled visit, I want to log it immediately as a Visit.

**Behavior**
- Entry points:
  - Near Me map
  - Student Profile
  - Student List (optional)
- Creates a Visit with:
  - DateTime = now
  - Status = Completed
  - VisitType defaulted (editable)
  - Notes

**Rules**
- Does not create a future visit
- One-future-visit-per-student rule remains intact

---

### 7.8 Data Model Notes

#### Phase 1
- Student:
  - StudyAddress
  - StudyLatitude / StudyLongitude
- Visit:
  - No location fields
  - Directions resolve from Student

#### Phase 2 (Planned)
- VisitAddress
- VisitLatitude / VisitLongitude

**Resolution Order**
1. Visit override
2. Student default
3. None

---

### 7.9 Assumptions and Constraints

- Single-publisher use
- ≤ 50 students
- Local-only data storage
- Native OS handles mapping and directions
- No silent data mutation