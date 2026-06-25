# Campus Notifications Microservice - System Design

## Stage 1 – REST API Design
The notification platform utilizes standard RESTful conventions. All routes require `Authorization: Bearer <token>`.

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/notifications` | Fetch all notifications (paginated, filter by type) |
| GET | `/api/notifications/{id}` | Fetch a single notification by ID |
| POST | `/api/notifications` | Create a new notification |
| PATCH | `/api/notifications/{id}/read` | Mark a notification as read |
| POST | `/api/notifications/notify-all` | Bulk send notifications |
| GET | `/api/notifications/priority-inbox` | Fetch Top-N priority inbox |

**JSON Schema:**
```json
{
  "id": "uuid-string",
  "studentId": 1042,
  "type": "Placement | Result | Event",
  "message": "CSX Corporation hiring",
  "isRead": false,
  "createdAt": "2026-04-22T17:51:18Z"
}