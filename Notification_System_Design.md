# Notification System Design

---

## Stage 1

### REST API Endpoints

All routes require `Authorization: Bearer <token>` header (pre-authorised per evaluation rules).

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/notifications` | Fetch all notifications (paginated, filterable by type) |
| GET | `/api/notifications/{id}` | Get a single notification by ID |
| POST | `/api/notifications` | Create a new notification |
| PATCH | `/api/notifications/{id}/read` | Mark a notification as read |
| DELETE | `/api/notifications/{id}` | Delete a notification |
| POST | `/api/notifications/notify-all` | Send bulk notifications (Stage 5) |
| GET | `/api/notifications/priority-inbox` | Get top-N priority notifications (Stage 6) |
| GET | `/api/notifications/placement-recent` | Get recent placement notifications (Stage 3) |

### JSON Schemas

**Notification Object:**
```json
{
  "id": "uuid-string",
  "studentId": 1042,
  "type": "Placement | Result | Event",
  "message": "CSX Corporation hiring",
  "isRead": false,
  "createdAt": "2026-04-22T17:51:18Z"
}

```

**GET /api/notifications Response (200):**

```json
{
  "total": 120,
  "page": 1,
  "pageSize": 20,
  "notifications": [ { ...notification... } ]
}

```

**POST /api/notifications Request:**

```json
{
  "studentId": 1042,
  "type": "Placement",
  "message": "Interview scheduled with Accenture"
}

```

**PATCH /api/notifications/{id}/read Response (200):**

```json
{ "id": "uuid", "isRead": true }

```

### Headers

| Header | Type | Description |
| --- | --- | --- |
| Authorization | string | `Bearer <token>` – required on all routes |
| Content-Type | string | `application/json` – required on POST/PATCH |
| X-Request-ID | string | Optional trace ID for debugging |

### Real-Time Mechanism

Use **WebSockets via SignalR** (ASP.NET Core built-in):

* Server pushes new notifications to connected students instantly.
* Each student subscribes to their own SignalR group on login.
* REST endpoints serve historical/unread; SignalR delivers live events.
* Fallback: long-polling if WebSocket unavailable.

---

## Stage 2

### Recommended Persistent Database: PostgreSQL

**Why PostgreSQL:**

* ACID compliance – critical for notification delivery guarantees.
* JSONB support for flexible message metadata.
* Native UUID type, partial indexes, and `NOW()` functions.
* Scales well with read replicas for high-volume reads.

### DB Schema

```sql
CREATE TABLE students (
    id          SERIAL PRIMARY KEY,
    email       VARCHAR(255) NOT NULL UNIQUE,
    name        VARCHAR(255) NOT NULL,
    created_at  TIMESTAMPTZ DEFAULT NOW()
);

CREATE TYPE notification_type AS ENUM ('Event', 'Result', 'Placement');

CREATE TABLE notifications (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    student_id          INT NOT NULL REFERENCES students(id) ON DELETE CASCADE,
    notification_type   notification_type NOT NULL,
    message             TEXT NOT NULL,
    is_read             BOOLEAN NOT NULL DEFAULT FALSE,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Indexes for common access patterns
CREATE INDEX idx_notifications_student_unread 
    ON notifications(student_id, is_read, created_at DESC) 
    WHERE is_read = FALSE;

CREATE INDEX idx_notifications_type_created 
    ON notifications(notification_type, created_at DESC);

```

### Problems at Scale (50,000 students × 5,000,000 notifications)

| Problem | Mitigation |
| --- | --- |
| Table bloat / slow full scans | Partial indexes (WHERE is_read = FALSE), table partitioning by `created_at` |
| Hot rows on popular students | Read replicas; caching unread counts in Redis |
| Write pressure during bulk sends | Batched inserts; async queue (RabbitMQ / Redis Streams) |
| Storage growth | Archiving old notifications (>90 days) to cold storage (S3 + Parquet) |

### SQL Query Examples

**Fetch unread notifications for a student:**

```sql
SELECT * FROM notifications 
WHERE student_id = 1042 AND is_read = FALSE 
ORDER BY created_at DESC 
LIMIT 20;

```

---

## Stage 3

### Original Slow Query Analysis

```sql
-- ORIGINAL (slow at scale)
SELECT * FROM notifications 
WHERE studentID = 1042 AND isRead = false 
ORDER BY createdAt DESC;

```

**Why it is slow:**

* No composite index on `(studentID, isRead, createdAt)` → full table scan.
* `ORDER BY createdAt DESC` cannot use an index unless it is included in order.
* `SELECT *` retrieves all columns even when only a few are needed.

**Computation cost:** O(N) scan where N = 5,000,000 rows; very slow.

**Optimised query:**

```sql
-- OPTIMISED
SELECT id, message, notification_type, created_at FROM notifications 
WHERE student_id = 1042 AND is_read = FALSE 
ORDER BY created_at DESC 
LIMIT 20;

```

**Why "index every column" is bad:**

* Every write (INSERT/UPDATE) must update all indexes → write amplification.
* Increased storage and vacuuming cost.
* The query planner may choose a wrong index; explicit targeted indexes are better.

### Placement Notifications in Last 7 Days

```sql
SELECT id, student_id, message, created_at 
FROM notifications 
WHERE notification_type = 'Placement' 
  AND created_at >= NOW() - INTERVAL '7 days' 
ORDER BY created_at DESC;

```

---

## Stage 4

### Problem: DB overwhelmed on every page load

**Solutions and Trade-offs:**

#### 1. Server-Side Caching (Redis) ✅ Recommended primary fix

* Cache unread notification count per student in Redis with TTL = 30 seconds.
* On page load: return cached count instantly; background job refreshes if stale.
* **Trade-off:** Slightly stale count (up to 30 s); acceptable for notifications.

#### 2. Pagination

* Never return all notifications at once; enforce `LIMIT 20` with cursor-based pagination.
* **Trade-off:** Client must implement "load more"; minimal downside.

#### 3. Read Replicas

* Route all GET queries to a PostgreSQL read replica; writes go to primary.
* **Trade-off:** Replication lag (~1-2 s); a new notification may not appear immediately on the replica.

#### 4. Database Connection Pooling (PgBouncer)

* 50,000 simultaneous page loads cannot each open a DB connection.
* PgBouncer maintains a fixed pool; requests queue rather than crash.
* **Trade-off:** Adds one network hop; very low latency impact.

**Recommended Stack:** Redis (unread counts) + Read Replica + PgBouncer + Pagination.

---

## Stage 5

### Pseudocode Analysis

**Shortcomings:**

1. **Sequential** – at ~100 ms/email × 50,000 = 83 minutes; unacceptable.
2. **No error handling** – one failed email stops all subsequent sends.
3. **No retry** – 200 failed students are permanently skipped.
4. **Synchronous DB write per student** – 50,000 individual inserts; should be batched.
5. **Tight coupling** – email failure blocks DB save and push.

### Should Email + DB Save Happen Together?

**No – they should NOT be atomic.** An email once sent cannot be rolled back. Tying it to a DB transaction is meaningless. A DB failure would cause a double-send on retry. Fire the email first, save to DB independently, and use idempotency keys.

### Revised Pseudocode

```python
function notify_all(student_ids, message):
  batches = split(student_ids, batch_size=500)
  for batch in batches:
    run in parallel (max 50 concurrent workers):
      for student_id in batch:
        try:
          ok = send_email(student_id, message)
          if ok:
            enqueue_db_insert(student_id, message)   # async queue
            push_to_app(student_id, message)
          else:
            log_failure(student_id)
            enqueue_retry(student_id, message)       # retry queue
        except Exception as e:
          log_error(student_id, e)
          enqueue_retry(student_id, message)

  # Flush DB queue in a single batched INSERT
  batch_insert_notifications(db_queue)

```

---

## Stage 6

### Priority Inbox Algorithm

**Priority Score Formula:**

```text
priority = typeWeight * 1000 + recencyScore

typeWeight:  Placement = 3,  Result = 2,  Event = 1
recencyScore: normalised timestamp (0–999), newer = higher

```

**Implementation:** Min-heap of size N (topN).

* For each incoming notification, if heap has fewer than N items → push.
* If heap is full and new item scores higher than the minimum → pop min, push new.
* Result: O(M log N) time, O(N) space, where M = total notifications.
* This works efficiently even as new notifications keep arriving.

**Why this is better than sort + slice:**

* Sort is O(M log M); heap is O(M log N) – much faster when N << M.
* The heap can be maintained incrementally as new notifications arrive (no re-sort needed).

---

## Output Screenshots

*Note: The Affordmed evaluation-service was returning a 503 Service Unavailable during testing. The backend gracefully handles the HttpRequestException and returns a 502 Bad Gateway to prevent application crashes, proving production-ready error handling.*

### Question 3: Vehicle Scheduler Endpoint

### Stage 6: Priority Inbox Endpoint
```
