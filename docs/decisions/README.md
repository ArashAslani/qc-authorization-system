# Architecture Decision Records

This directory captures significant architectural decisions taken during
the implementation of the Qc Authorization System. Each decision is a
short Markdown file whose name follows the
`NNNN-short-slug.md` convention and whose status is one of
`Proposed`, `Accepted`, or `Superseded`.

| # | Title | Status | Phase |
|---|-------|--------|-------|
| 0001 | [Grant as dumb data](0001-grant-as-dumb-data.md) | Accepted | 02 |
| 0002 | [Engine is sole Allow/Deny owner](0002-engine-is-sole-allow-deny-owner.md) | Accepted | 03 |
| 0003 | [Asymmetric Position propagation](0003-asymmetric-position-propagation.md) | Accepted | 04 |
| 0004 | [Computed, not materialized propagation](0004-computed-not-materialized-propagation.md) | Accepted | 04 |
| 0005 | [Individual Grant isolation](0005-individual-grant-isolation.md) | Accepted | 04 |
| 0006 | [Source-aware priority model](0006-source-aware-priority-model.md) | Accepted | 03 |
| 0007 | [Decision Trace content](0007-decision-trace-content.md) | Accepted | 03 |
| 0008 | [SQLite + EF Core 10 in V1](0008-sqlite-efcore-v1.md) | Accepted | 00 |
| 0009 | [Typed constraints, no DSL](0009-typed-constraints-no-dsl.md) | Accepted | 06 |
| 0010 | [Identity Role vs Qc Authorization Role separation](0010-identity-vs-qc-role-separation.md) | Accepted | Identity audit |
| 0011 | [Personnel vs System User and RoleGroup assignment](0011-personnel-user-role-group.md) | Accepted | Business alignment |
| 0012 | [RoleGroup is role bundle only](0012-hybrid-rolegroup-permissions.md) | Superseded | US-ACCESS-01 |
| 0013 | [Remove unused Scope value object](0013-remove-unused-scope-value-object.md) | Accepted | Remediation |
