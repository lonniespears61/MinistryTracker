# MinistryTracker — Kanban: Mapping & Location

## Epic: Mapping, Location & Unscheduled Visits

Goal: Enable navigation, last-minute visits, and accurate visit history with minimal complexity.

---

## Story 1: Open Directions to Student or Visit Location
**Priority:** High  
**Phase:** 1

**Acceptance Tests**
- Directions open native maps to GPS when available
- Address fallback works when GPS missing
- Warning shown when no location exists
- No in-app navigation UI is used

---

## Story 2: Capture GPS on First In-Person Visit
**Priority:** High  
**Phase:** 1

**Acceptance Tests**
- Prompt shown only when GPS missing
- Confirming saves GPS to Student
- Declining does not modify Student
- No GPS capture for non-physical visits

---

## Story 3: Update Student Default Location
**Priority:** Medium  
**Phase:** 1

**Acceptance Tests**
- Updating GPS requires confirmation
- Clearing GPS falls back to address
- Changes affect future directions and Near Me

---

## Story 4: Students Near Me (Map View)
**Priority:** Medium  
**Phase:** 1

**Acceptance Tests**
- Map loads even if permission denied
- Only students with GPS appear
- Pin actions work (Directions, Log Visit, Profile)

---

## Story 5: Log Unscheduled Visit
**Priority:** High  
**Phase:** 1

**Acceptance Tests**
- Visit saved with DateTime = now
- Status set to Completed
- Appears in calendar and history
- No future visit created

---

## Phase 2 (Deferred): Visit Location Overrides

**Trigger Conditions**
- Repeated need for one-time visit locations
- Public-place recurring visits
- User feedback requesting override support

**Acceptance Tests**
- Visit override does not modify Student
- Directions prefer Visit override
- Removing override restores Student default
