
# Implement Azure Service Bus and Signal R for Event Handling

**Date**: 2025-11-25

## Status

Accepted

## Context

When users upload files to the External Applications project, these must be scanned for malware - as this can take an indeterminate amount of time, we require an asynchronous method for alerting the user when this scan is complete.
  

## Decision

The decision has been taken to use Azure Service Bus and Signal R to alert users to asynchronous events.
  

## Reasons for the Decision

**Asynchronous Event Handling**: To prevent the UI from either blocking or polling to return the success or failure of a virus scan, an event will be generated and placed on the Event Bus. Serverless Functions will perform the scan and Signal R will then return the result of the scan to the user asynchronously.

**Expanded Event Handling**: Many of the projects in the RSD space would benefit from being able to register and respond to events, this will put in place the infrastructure that other projects can then utilise.
