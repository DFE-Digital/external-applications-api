
# Use Azure File Share for Storing Uploaded Files

**Date**: 2025-11-25

## Status

Accepted

## Context

Within the External Applications project, there is a requirement for users to be able to upload files. These need to be stored within the Azure cloud and made available for users to download. Options exist within Azure, for instance Azure File Share or Azure Blob Storage.
  

## Decision

The decision has been taken to use Azure File Share to store uploaded files.
  

## Reasons for the Decision

**Ease of Access**: The External Applications project runs in Docker containers within Azure Container Apps. Using Azure File Share allows the location for the files to be accessible as a mounted drive, making file access simpler using generic file access utilities.

**Purpose**: Azure File Share is more appropriate for the small files which users will upload to the External Applications project.
