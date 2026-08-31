# Async Note Classification Architecture

## Purpose

This document defines the architecture for asynchronously classifying notes against the hierarchical knowledge nodes already created by the user.

The classification process must never block the request that creates or updates a note.

The backend remains the owner of application state and persistence. The Python classifier is only responsible for inference.

---

# Architecture Overview

```text
┌──────────────┐
│   Frontend   │
└──────┬───────┘
       │ POST /notes
       ▼
┌─────────────────────┐
│   ASP.NET Backend   │
│                     │
│  1. Save Note       │
│  2. Create Job      │
└─────────┬───────────┘
          │
          │ Same transaction
          ▼
┌────────────────────────────┐
│        SQL Server          │
│                            │
│ Notes                      │
│ ClassificationJobs         │
│ ClassificationRuns         │
│ NoteClassifications        │
└───────────┬────────────────┘
            │
            │ Consume pending job
            ▼
┌─────────────────────┐
│ Classification      │
│ Worker (.NET)       │
└──────────┬──────────┘
           │ Internal HTTP
           ▼
┌─────────────────────┐
│ Python Classifier   │
│                     │
│ GLiClass            │
└──────────┬──────────┘
           │ Scores
           ▼
┌─────────────────────┐
│ Classification      │
│ Worker              │
│                     │
│ Persist result      │
└──────────┬──────────┘
           │
           ▼
       SQL Server
           │
           ▼
        SignalR
           │
           ▼
       Frontend
```

---

# Core Principles

## 1. Classification is asynchronous

Creating or updating a note must not wait for ML inference.

The request flow is:

```text
POST /notes
    ↓
Save note
    ↓
Create classification job
    ↓
Commit
    ↓
Return response
```

The API can immediately return something equivalent to:

```json
{
  "id": 391,
  "classificationStatus": "pending"
}
```

Classification happens independently afterward.

The frontend must not remain blocked waiting for the classifier.

---

# 2. Use SQL Server as the initial durable queue

Do not introduce RabbitMQ, Kafka, Redis Streams, or Azure Service Bus unless the system scale or architecture later requires them.

Initially, use a durable SQL-backed job queue.

Example:

```text
ClassificationJobs
------------------------------
Id
NoteId
NoteVersion
TaxonomyVersion
Status
Attempts
CreatedAt
AvailableAt
StartedAt
CompletedAt
LockedUntil
LastError
```

Possible states:

```text
Pending
Processing
RetryScheduled
Completed
Failed
```

This guarantees that classification work survives:

* API restarts;
* worker crashes;
* machine restarts;
* temporary classifier failures.

Never implement classification as an in-memory fire-and-forget operation such as:

```csharp
Task.Run(() => ClassifyNote(note));
```

A process restart would lose the work.

---

# 3. Note and ClassificationJob must be transactional

The note and its classification job must be created in the same database transaction.

Correct:

```text
BEGIN TRANSACTION

INSERT Note

INSERT ClassificationJob

COMMIT
```

Do not:

```text
INSERT Note
COMMIT

INSERT ClassificationJob
COMMIT
```

Otherwise the application could save a note and fail before creating its classification job.

The invariant is:

```text
If a note requiring classification is committed,
a corresponding classification job must also exist.
```

---

# 4. Backend owns the domain

The Python classifier must not directly modify the application's domain tables.

Avoid:

```text
Python
  ↓
SQL Server
  ↓
UPDATE Notes / Nodes / relations
```

The classifier should behave like a stateless computational service:

```text
Classify(text, taxonomy)
    ↓
ClassificationResult
```

It should not know:

* how Notes are persisted;
* application business rules;
* user authorization;
* database schema;
* how Node relations are created;
* how the frontend is notified.

The backend owns these decisions.

---

# 5. Python classification service

Expose the classifier through a small internal service, preferably using FastAPI.

Example endpoint:

```http
POST /classify
```

Input:

```json
{
  "text": "O uso de subagentes no Codex.",
  "nodes": [
    {
      "id": 10,
      "name": "Inteligência Artificial",
      "description": "LLMs, machine learning, agentes de IA, transformers e inferência."
    },
    {
      "id": 20,
      "name": "Banco de Dados",
      "description": "SQL, modelagem, índices, consultas e armazenamento de dados."
    }
  ]
}
```

Example output:

```json
{
  "classifications": [
    {
      "nodeId": 10,
      "score": 0.91
    },
    {
      "nodeId": 20,
      "score": 0.04
    }
  ],
  "model": "gliclass-multilang-mini"
}
```

The Python process should preferably load the ML model once during startup and reuse it for all requests.

Do not reload the model for each classification.

---

# 6. Classification Worker

Use a separate .NET worker process.

Suggested implementation:

```text
BackgroundService
```

The worker continuously performs:

```text
Claim pending job
    ↓
Load note
    ↓
Load user's current taxonomy
    ↓
Call Python classifier
    ↓
Receive scores
    ↓
Validate versions
    ↓
Persist classification run
    ↓
Mark job completed
    ↓
Notify frontend
```

The worker should preferably run separately from the Web API process.

Example deployment:

```text
Machine / VM / Container Host
│
├── knowledge-tracker-api
├── knowledge-tracker-worker
└── classification-service-python
```

Logical separation does not require separate physical machines.

---

# 7. Waiting inside the worker is acceptable

The requirement that classification be asynchronous means the user's HTTP request must not wait.

It does not mean that the worker cannot await the classifier.

This is correct:

```csharp
var result = await classifierClient.ClassifyAsync(...);
```

because it happens inside the background worker.

Incorrect architecture:

```text
User request
    ↓
Save note
    ↓
Wait for Python classifier
    ↓
Persist result
    ↓
Return response
```

Correct architecture:

```text
User request
    ↓
Save note + enqueue
    ↓
Return immediately
```

while independently:

```text
Worker
    ↓
Wait for classifier
    ↓
Persist result
```

---

# 8. Jobs must be atomically claimed

Do not simply execute:

```sql
SELECT TOP 1 *
FROM ClassificationJobs
WHERE Status = 'Pending';
```

If multiple workers exist, two could consume the same job.

The worker must atomically claim work.

Conceptually:

```text
Pending
   ↓
atomic claim
   ↓
Processing
WorkerId = X
LockedUntil = ...
```

For SQL Server, the implementation can use concurrency mechanisms such as:

```text
UPDLOCK
READPAST
```

combined with an atomic update.

The design must allow multiple workers in the future.

---

# 9. Use leases

A job must not remain permanently stuck in `Processing` if a worker crashes.

Use:

```text
LockedUntil
```

Example:

```text
Status = Processing
LockedUntil = 2026-08-20T16:15:00
```

If the worker disappears and the lease expires, another worker can recover the job.

Conceptually:

```text
Processing
+
expired LockedUntil
    ↓
eligible for recovery
```

---

# 10. Assume at-least-once processing

Do not attempt to depend on perfect exactly-once execution.

Design classification as:

```text
at-least-once delivery
+
idempotent persistence
```

The same job may theoretically execute twice.

Persist results so duplicate execution does not create duplicated domain state.

Possible mechanism:

```text
ClassificationRun
-------------------------
Id
ClassificationJobId UNIQUE
...
```

or another uniqueness constraint tied to the job/version.

---

# 11. Persist classification scores

Do not store only the selected Node.

Store the scores produced by the model.

Example:

```text
ClassificationRun
-------------------------
Id
NoteId
NoteVersion
TaxonomyVersion
Model
ModelVersion
CreatedAt
```

and:

```text
NoteClassification
-------------------------
ClassificationRunId
NodeId
Score
```

Example result:

```text
Artificial Intelligence     0.93
AI Agents                   0.89
Software Development        0.61
Backend                     0.22
Database                    0.03
```

Keeping these values allows future:

* threshold changes;
* model comparison;
* debugging;
* classification quality analysis;
* user feedback;
* retraining;
* taxonomy experimentation.

Do not treat the score as a literal percentage of how much the text belongs to a topic unless the model has been explicitly calibrated for that interpretation.

Prefer terminology such as:

```text
classification score
```

or:

```text
topic relevance score
```

---

# 12. Distinguish manual and inferred relationships

A relation explicitly created by the user is not semantically equivalent to one inferred by ML.

Persist the source.

Example:

```text
RelationSource
--------------
Manual
Classifier
Inherited
```

Possible structure:

```text
NoteNodeRelation
-------------------------
NoteId
NodeId
Source
Score NULL
ClassificationRunId NULL
```

For a manual relation:

```text
Source = Manual
Score = NULL
```

For a classification-generated relation:

```text
Source = Classifier
Score = 0.91
ClassificationRunId = ...
```

Do not silently overwrite manual relations with classifier output.

---

# 13. Handle note edits with versioning

A classification result must never be applied to a newer version of the note.

Example race condition:

```text
Note version 7:
"O uso de subagentes no Codex."

Classification starts.

User edits note.

Note version 8:
"Índices clustered no SQL Server."

Old classification finishes:
AI = 0.92
```

The old result must not be applied to version 8.

Store:

```text
NoteVersion
```

inside the job.

Before persisting:

```text
current Note.Version == ClassificationJob.NoteVersion?
```

If true:

```text
persist
```

If false:

```text
discard result
```

The new note version should have its own classification job.

A content hash may be used instead of, or in addition to, a version number.

---

# 14. Handle taxonomy changes

The user's Node hierarchy may change while classification is running.

A classification result therefore belongs to both:

```text
NoteVersion
TaxonomyVersion
```

A classification run should preserve:

```text
ClassificationRun
├── NoteVersion
├── TaxonomyVersion
├── Model
├── ModelVersion
└── Results
```

Before applying a result, validate that it still corresponds to the intended taxonomy version.

This also makes previous classification runs reproducible and auditable.

---

# 15. Hierarchical classification

The user may eventually have a large hierarchy such as:

```text
Technology
├── Software
│   ├── Backend
│   │   ├── ASP.NET
│   │   └── Spring
│   └── Frontend
│       ├── React
│       └── Angular
├── Databases
│   ├── SQL Server
│   └── PostgreSQL
└── Artificial Intelligence
    ├── Machine Learning
    ├── LLMs
    └── AI Agents
```

For small taxonomies, sending all Nodes to the classifier is acceptable.

For large taxonomies, prefer hierarchical classification:

```text
Text
 ↓
Classify root-level nodes
 ↓
Select relevant branches
 ↓
Classify their children
 ↓
Continue recursively
```

Example:

```text
"O uso de subagentes no Codex."

Root classification:

Software Development    0.58
Artificial Intelligence 0.95
Databases               0.02

↓

Artificial Intelligence:

Machine Learning         0.65
LLMs                     0.79
AI Agents                0.96
```

This reduces:

* inference cost;
* classification noise;
* unnecessary candidate labels.

---

# 16. Frontend notification

The note should initially appear as something equivalent to:

```text
Classification: Pending
```

After the worker persists the result, notify the frontend.

Preferred approach:

```text
SignalR
```

Example event:

```text
NoteClassificationCompleted
```

Payload:

```json
{
  "noteId": 391,
  "classificationRunId": 812
}
```

The frontend can then refresh the note/classification data.

Possible UI flow:

```text
User saves note

Saved
Classification pending...

↓

Worker finishes

Classification completed

AI Agents                  0.91
Artificial Intelligence    0.88
LLMs                       0.74
```

Polling may be used initially if SignalR is not yet justified:

```http
GET /notes/{id}/classification
```

But SignalR is preferable if classification completion should appear naturally in the UI.

---

# 17. Retry policy

Transient classifier failures should not immediately mark jobs permanently failed.

Example behavior:

```text
Attempt 1 fails
 ↓
RetryScheduled

Attempt 2 fails
 ↓
RetryScheduled

Attempt 3 fails
 ↓
Failed
```

Persist:

```text
Attempts
AvailableAt
LastError
```

Use bounded retries with backoff.

Example:

```text
1st retry: +10 seconds
2nd retry: +1 minute
3rd retry: +5 minutes
```

Do not retry permanently invalid jobs indefinitely.

---

# 18. Classifier availability must not affect note availability

If the Python classifier is offline:

```text
POST /notes
```

must still succeed as long as the main application and database are healthy.

The result becomes:

```text
Note saved
Classification pending
```

The worker retries once the classifier becomes available.

Classification is an asynchronous enrichment process, not a prerequisite for note persistence.

---

# 19. Suggested responsibility boundaries

## Frontend

Responsible for:

* creating/editing notes;
* displaying classification status;
* displaying classification results;
* receiving completion notifications;
* allowing the user to correct classifications if supported.

Not responsible for:

* ML inference;
* classification retries;
* queue handling.

---

## ASP.NET Web API

Responsible for:

* note commands;
* authorization;
* creating classification jobs;
* exposing classification state;
* domain rules;
* serving current classification results.

Not responsible for:

* performing ML inference during HTTP requests.

---

## SQL Server

Responsible for:

* domain persistence;
* durable classification queue;
* classification runs;
* scores;
* leases and retry metadata.

---

## .NET Classification Worker

Responsible for:

* claiming jobs;
* retries;
* lease handling;
* loading note/taxonomy data;
* calling the Python classifier;
* validating note/taxonomy versions;
* persisting results;
* marking jobs completed or failed;
* triggering completion notifications.

---

## Python Classification Service

Responsible only for:

```text
text + taxonomy
    ↓
ML inference
    ↓
scores
```

It should remain stateless regarding the application's domain.

---

# 20. Initial deployment

A simple deployment is enough:

```text
Single Machine / VM
│
├── ASP.NET API
├── .NET Classification Worker
├── Python Classification Service
└── SQL Server / external SQL Server
```

The services can later be independently containerized or moved to separate infrastructure.

---

# 21. Future queue evolution

Do not introduce a message broker prematurely.

SQL Server is sufficient while:

* job volume is moderate;
* there is one main producer;
* classification is internal to the application;
* operational simplicity is preferred.

If the architecture later requires:

* many producers;
* many independent consumers;
* high throughput;
* sophisticated dead-letter handling;
* independent service events;
* distributed processing;

evolve toward:

```text
ASP.NET transaction
      ↓
Transactional Outbox
      ↓
Azure Service Bus / RabbitMQ
      ↓
Classification Workers
```

The classification domain contracts should remain largely unchanged.

---

# Final Flow

```text
USER
 │
 │ Save note
 ▼
ASP.NET API
 │
 ├─ BEGIN TRANSACTION
 │
 ├─ Save Note
 │
 ├─ Create ClassificationJob(Pending)
 │
 └─ COMMIT
 │
 ▼
201 Created
{
  "classificationStatus": "pending"
}

════════════════════════════════════════
         ASYNCHRONOUS BOUNDARY
════════════════════════════════════════

.NET Classification Worker
 │
 ├─ Atomically claim job
 ├─ Start lease
 ├─ Load Note
 ├─ Load user's taxonomy
 │
 ▼
Python Classification Service
 │
 ├─ GLiClass
 │
 └─ Return node scores
 │
 ▼
.NET Worker
 │
 ├─ Validate NoteVersion
 ├─ Validate TaxonomyVersion
 ├─ Persist ClassificationRun
 ├─ Persist Node scores
 ├─ Mark job Completed
 │
 ▼
SignalR / Notification
 │
 ▼
Frontend
 │
 └─ Refresh classification state
```

---

# Architectural Rule

The central rule is:

> **The application owns the classifier; the classifier does not own the application.**

The Python service produces inference.

The backend determines:

* whether that inference is still valid;
* how it affects domain state;
* what gets persisted;
* what is shown to the user;
* how retries and failures are handled.

Classification must remain an asynchronous, replaceable enrichment capability rather than a dependency inside the note creation request.
