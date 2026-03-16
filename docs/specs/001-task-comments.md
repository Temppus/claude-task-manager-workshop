# Feature Spec: Task Comments

## Problem

Users need to discuss tasks with their team. Currently there's no way to add
notes or context to a task beyond the description field.

## API Contract

All endpoints are nested under a task: `/api/tasks/{taskId}/comments`

### POST /api/tasks/{taskId}/comments

Create a new comment on a task.

**Request body:**
```json
{
  "author": "Alice",
  "body": "This is a comment"
}
```

**Response (201 Created):**
```json
{
  "id": "guid",
  "task_id": "guid",
  "author": "Alice",
  "body": "This is a comment",
  "created_at": "2026-03-16T12:00:00Z"
}
```

**Errors:**
- `400` — validation (author or body missing/empty, body exceeds 1000 characters, author exceeds 100 characters)
- `404` — task not found

### GET /api/tasks/{taskId}/comments

List all comments for a task, ordered by `created_at` ascending (oldest first).

**Response (200 OK):**
```json
[
  {
    "id": "guid",
    "task_id": "guid",
    "author": "Alice",
    "body": "First comment",
    "created_at": "2026-03-16T12:00:00Z"
  }
]
```

**Errors:**
- `404` — task not found

## Schema

New `comments` table:

| Column     | Type          | Constraints                          | Default            |
|------------|---------------|--------------------------------------|--------------------|
| id         | UUID          | PK                                   | `gen_random_uuid()`|
| task_id    | UUID          | FK → tasks(id) ON DELETE CASCADE     | —                  |
| author     | VARCHAR(100)  | NOT NULL                             | —                  |
| body       | TEXT          | NOT NULL, max 1000 chars             | —                  |
| created_at | TIMESTAMP     | NOT NULL                             | `now()`            |

Index on `task_id` for efficient lookups.

## Business Rules

1. Both `author` and `body` are required.
2. `author` and `body` are trimmed of leading/trailing whitespace before validation — whitespace-only values are rejected as empty.
3. `author` max length: 100 characters.
4. `body` max length: 1000 characters. Multiline text (newlines) is allowed.
5. Comments can be added to tasks in any status (todo, in_progress, done).
6. Deleting a task cascades to delete all its comments.
7. Comments are returned oldest-first by default.
8. Adding a comment does **not** update the task's `updated_at` timestamp.

## Out of Scope (intentional)

The following are deliberately excluded from this version and may be added in a future spec:

- **Edit comment** (PUT/PATCH) — comments are immutable once created.
- **Delete comment** (DELETE) — comments cannot be individually removed.
- **Pagination** — all comments are returned in a single response.
- **Comment count on task response** — no `comment_count` field on task endpoints.

## Agent Tasks

1. Database migration + model
2. Route handlers + validation
3. Tests (unit + integration)

## Test Plan

### Regression

All existing task tests must continue to pass — particularly delete (cascade FK change) and stats (new table/model must not interfere).

### Create comment (POST /api/tasks/{taskId}/comments)

| # | Test | Expected |
|---|------|----------|
| 1 | Valid comment with author + body | 201, correct fields returned |
| 2 | Missing author | 400 |
| 3 | Missing body | 400 |
| 4 | Empty body after trim | 400 |
| 5 | Whitespace-only author | 400 |
| 6 | Whitespace-only body | 400 |
| 7 | Body exceeds 1000 chars | 400 |
| 8 | Author exceeds 100 chars | 400 |
| 9 | Multiline body with newlines | 201, newlines preserved |
| 10 | Task not found | 404 |

### List comments (GET /api/tasks/{taskId}/comments)

| # | Test | Expected |
|---|------|----------|
| 11 | Returns comments ordered by created_at ascending | 200, oldest first |
| 12 | Task with no comments | 200, empty array |
| 13 | Task not found | 404 |

### Cascade delete

| # | Test | Expected |
|---|------|----------|
| 14 | Delete task removes its comments | GET comments returns 404 (task gone) |

### Isolation

| # | Test | Expected |
|---|------|----------|
| 15 | Comments on task A do not appear on task B | 200, only that task's comments |
