
# Use ClamAV for Virus Scanning

**Date**: 2025-11-25

## Status

Accepted

## Context

Within the External Applications project, there is a requirement for users to be able to upload files. These files will need virus scanning on upload, various options exist for virus scanning within the Azure ecosystem.
  

## Decision

The decision has been taken to use ClamAV to virus scan uploaded files.
  

## Reasons for the Decision

**Control of Scanning**: Using ClamAV allows us more fine-grained control to initiate the scan and handle the responses.

**Compatibility**: Defender for Cloud does not support virus scanning on Azure File Share.

**Pricing**: ClamAV does not operate per-scan pricing, making costs more predictable.
