
# Implement Temporal Tables for DB Auditing

**Date**: 2025-11-25

## Status

Accepted

## Context

The External Applications project requires auditing of changes to the application data.
  

## Decision

The decision has been taken to utilise SQL Server Temporal Tables to record all changes at a data level.
  

## Reasons for the Decision

**Ease of Implementation**: Activating Temporal Tables in the SQL Server instance is a common pattern in RSD applications. This then records all data level changes which can be queried back to any point in time to see who performed which changes.

**Established Pattern**: Most other applications maintained by RSD use the same pattern to audit changes to the application.
