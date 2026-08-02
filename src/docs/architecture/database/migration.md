# Migration Workplan Specification

## Purpose

This document defines how a module's business requirements become an
implementation-ready migration workplan.

Database work must follow this sequence:

```text
docs/modules/<module>/
    -> docs/workplan/<module>/
        -> migration implementation
```

The applicable documentation under `docs/modules/<module>/` is the source of the
business requirements. It must exist before the migration workplan is created,
and the agent must read it completely before proposing a relational structure.

The migration workplan must be stored under `docs/workplan/<module>/`. In
general, a module should have a single migration workplan file that describes
all tables, columns, keys, relationships, constraints, indexes, compatibility
requirements, and implementation order required by that module. The workplan
may be split only when the migration is too large to remain clear as one
executable plan or when independently deployable database stages are explicitly
required.

The workplan is an analysis and implementation-planning artifact. The agent
creating it must not create migration SQL. Migration implementation may begin
only after the workplan is complete and its status is `Ready for
implementation`.

The analysis agent must critically evaluate the module requirements and the
user's proposal from both business and technical perspectives. It must not
merely translate the proposed entities into tables.

The agent is allowed to define or revise:

* Database schemas
* Table and column names
* SQL Server data types
* Nullability
* Primary and foreign keys
* Cardinalities
* Linking tables
* Constraints
* Relevant indexes
* Structured values consumed by the backend

The resulting workplan must describe a model that is normalized,
understandable, enforceable where appropriate, and suitable for future
application behavior. It must give the implementation agent enough precise
information to create the migration without inventing schema decisions.

The migration workplan must be refined iteratively until its status is `Ready
for implementation`.

---

## Workflow Responsibilities

### Module stage

The module documentation defines the requested business capability, relevant
behavior, business rules, and user decisions. The migration workplan must not
silently broaden, narrow, or contradict those requirements. Missing or
contradictory decisions must be reported before the workplan is marked ready.

### Workplan stage

The analysis agent must convert the module requirements into a concrete
relational plan. The workplan must:

1. Reference the applicable module documentation.
2. Describe the current schema structures that affect the change.
3. Define every table, column, relationship, constraint, and relevant index to
   be created or modified.
4. State ownership, cardinality, nullability, uniqueness, deletion behavior,
   compatibility requirements, and implementation order explicitly.
5. Identify existing migrations, procedures, or Domain structures that the
   implementation agent must inspect.
6. Record open decisions and block implementation when any unresolved decision
   would require the implementation agent to invent database behavior.

### Implementation stage

Before creating or modifying migration SQL, the implementation agent must read:

1. The complete module documentation under `docs/modules/<module>/`.
2. The applicable migration workplan under `docs/workplan/<module>/`.
3. The Data-layer architecture documentation.
4. The existing schema, migrations, procedures, and Domain structures
   referenced by the workplan.

The implementation agent must implement the workplan as written. It may report
contradictions or missing decisions, but must not silently alter cardinality,
nullability, ownership, uniqueness, deletion behavior, compatibility
requirements, or implementation order.

---

## Analysis Instructions

Before proposing the final structure, the analysis agent must:

1. Understand the business capability being requested.
2. Inspect the existing schema and related structures.
3. Identify the expected backend behavior.
4. Challenge assumptions in the user's proposal.
5. Identify missing business concepts or relationships.
6. Consider realistic future use cases that materially affect the current schema.
7. Distinguish necessary extensibility from speculative overengineering.
8. Prefer the smallest structure that supports the confirmed requirements without creating obvious structural limitations.

The agent must be explicit when the proposal:

* Mixes multiple concepts in one table
* Stores relational values in a single column
* Uses ambiguous or overly generic names
* Has incorrect or undefined cardinality
* Uses nullability without clear business meaning
* Cannot safely support expected backend behavior
* Would require difficult or destructive changes for a foreseeable use case
* Places behavior in data that the backend cannot interpret reliably
* Lacks constraints required to prevent invalid persisted states

The agent may recommend a different structure from the one proposed by the user.

---

## Business Capability

**Objective:**
`<What business capability must this structure support?>`

**Main business concepts:**

* `<concept>`
* `<concept>`

**Expected backend usage:**

Describe briefly how the backend will create, read, update, validate, or evaluate this data.

---

## Proposed Relational Structure

### `<schema.TableName>`

**Purpose:**
`<What this table represents in business terms.>`

**Backend usage:**
`<How the application is expected to consume this table.>`

| Column        | SQL Server type | Nullable | Key          | Description              |
| ------------- | --------------- | -------: | ------------ | ------------------------ |
| `<Id>`        | `int`           |       No | PK, Identity | `<Meaning>`              |
| `<Column>`    | `<type>`        |       No |              | `<Meaning>`              |
| `<RelatedId>` | `int`           |   Yes/No | FK           | `<Relationship meaning>` |

**Structural rules:**

* `<Required uniqueness, valid ranges or persisted-state restrictions>`
* `<Constraint required to prevent an invalid state>`

Repeat this section for every proposed table.

---

## Relationships

| Source           | Target           | Cardinality | Required | Delete behavior | Meaning              |
| ---------------- | ---------------- | ----------- | -------: | --------------- | -------------------- |
| `<Table.Column>` | `<Table.Column>` | One-to-many |      Yes | Restrict        | `<Business meaning>` |

The specification must explicitly define:

* Which side owns the relationship
* Whether the association is optional
* Whether duplicate associations are allowed
* Whether historical associations must be preserved
* Whether a linking table is required
* What happens when a referenced record is deleted or archived

Many-to-many relationships must use explicit linking tables.

---

## Backend-Consumed Values

Use this section when a column stores a structure that the backend must interpret rather than merely display.

### `<schema.Table.Column>`

**Purpose:**
`<Behavior represented by this value.>`

**Storage format:**
`<Exact format, including versioning when necessary.>`

**Backend interpretation:**
`<How the backend evaluates or applies the value.>`

**Validation:**
`<What makes the value structurally valid.>`

The persisted format must be deterministic and versioned when its structure may evolve.

For example, a form rule condition may use a JSON expression tree because conditions may be nested and have different operators:

```json
{
  "version": 1,
  "operator": "equals",
  "fieldId": 42,
  "value": "Internal"
}
```

The specification must define relevant behavior, such as:

* Supported operators
* Supported value types
* How referenced fields are identified
* The result when a referenced value is absent
* Whether hidden, disabled, or read-only fields may submit values
* Whether unexpected submitted values are rejected, ignored, or removed
* How expression versions are handled

JSON may represent variable expression trees, configuration payloads, or external documents.

JSON must not replace ordinary relational relationships.

---

## Critical Review

The analysis agent must explicitly record relevant weaknesses and decisions.

### `<Finding title>`

* **Type:** Defect, business ambiguity, architectural decision, implementation risk, or improvement
* **Observation:** `<What is wrong, missing, or uncertain>`
* **Impact:** `<How it affects the schema or backend>`
* **Recommendation:** `<Preferred resolution>`
* **Status:** Open or resolved

The review must not use vague statements such as “consider scalability” or “review normalization.” Every finding must describe a concrete consequence.

---

## Relevant Future Scenarios

The analysis agent must consider plausible future scenarios that could materially affect the proposed structure.

Examples include:

* One relationship becoming many-to-many
* A mutable configuration requiring version history
* Rules needing compound or nested conditions
* Records requiring archival rather than deletion
* Configuration changing after existing transactions were created
* Audit requirements requiring preservation of the original value
* Multiple application components consuming the same structure
* Stored values requiring versioned interpretation
* A unique relationship becoming tenant-, organization-, or scope-specific
* A backend operation requiring efficient filtering by a currently unindexed field

For each relevant scenario, state:

| Scenario     | Current model support             | Required change | Decision                             |
| ------------ | --------------------------------- | --------------- | ------------------------------------ |
| `<scenario>` | Supported / Partial / Unsupported | `<impact>`      | Support now / Defer / Not applicable |

The agent must not add structures only because they may theoretically be useful.

A future scenario should affect the current model only when:

* It is a likely extension of the business capability
* Ignoring it would create an expensive or destructive migration
* Supporting it now has low structural cost
* It changes ownership, cardinality, history, or data interpretation

---

## Final Assessment

* **Status:** Draft, Under review, Blocked, or Ready for implementation
* **Main strengths:** `<brief summary>`
* **Main risks:** `<brief summary>`
* **Open decisions:** `<list or none>`
* **Recommended model:** `<brief conclusion>`

The specification may be marked `Ready for implementation` only when:

* Tables and columns are explicitly defined
* Data types and nullability have business meaning
* Relationships and cardinalities are explicit
* Backend-consumed formats are defined
* Critical structural problems have been resolved
* Relevant future scenarios were evaluated
* No unresolved decision prevents reliable implementation

## Documentation Language

The entire Migration Workplan Specification must be written in English.

This requirement applies to:

* Table and column descriptions
* Relationship semantics
* Critical findings
* Business and technical analysis
* Future scenarios
* Recommendations
* Decisions and final assessments

Database object names, including schemas, tables, columns, constraints, indexes, and stored procedures, must also use clear and consistent English terminology.

Do not mix Portuguese and English within the specification, except when quoting an original business term whose translation could change its meaning. In that case, provide the English term and preserve the original term in parentheses only when necessary.
