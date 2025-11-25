
# Implement JSON Templates for Site Configuration

**Date**: 2025-11-25

## Status

Accepted

## Context

RSD hosts multiple sites which all share the same basic functionality, is there a simpler way to create and host these sites?
  

## Decision

The decision has been taken to create configurable sites which run off JSON templates to aid in rapid development, reduce hosting costs and dramatically reduce the time to deployment.
  

## Reasons for the Decision

**Development Speed**: Previously, a new RSD site required the deployment of multiple environments and duplicated infrastructure. Using configurable templates, new RSD sites can be spun up and deployed in a fraction of the time.

**Reduced Hosting Costs**: As all sites will be hosted on the same infrastructure the impact of deploying a new site into the same environment will be negligible.

## Considerations

**Schemaless Design**: With the site content and responses being stored in JSON documents, any downstream reporting or data extraction will be more involved than direct SQL transfer

**Complexity of Code**: Given the JSON templates need to handle multiple events and conditional pathways, some complexity in the templates is inevitable, this will be mitigated by comprehensive documentation.
