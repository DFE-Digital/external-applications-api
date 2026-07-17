-- Ensure each tenant's ApplicationTemplates HostMappings only lists that tenant's templates.
-- Cross-tenant GUIDs in HostMappings cause catalogue leaks when tenants share an EA database.

UPDATE tenantconfig.TenantSettings
SET Settings = N'{"HostMappings":{"transfers":"9A4E9C58-9135-468C-B154-7B966F7ACFB7"}}'
WHERE Category = N'ApplicationTemplates'
  AND Target = N'Api'
  AND TenantId = (SELECT Id FROM tenantconfig.Tenants WHERE Name = N'Transfers');

UPDATE tenantconfig.TenantSettings
SET Settings = N'{"HostMappings":{"lsrp":"B2F8E7D4-2C46-4A91-8E73-9D5A1F4B6C89"}}'
WHERE Category = N'ApplicationTemplates'
  AND Target = N'Api'
  AND TenantId = (SELECT Id FROM tenantconfig.Tenants WHERE Name = N'Lsrp');

UPDATE tenantconfig.TenantSettings
SET Settings = N'{"HostMappings":{"visits":"413752f0-d67d-408f-b8eb-39ac02e7a612"}}'
WHERE Category = N'ApplicationTemplates'
  AND Target = N'Api'
  AND TenantId = (SELECT Id FROM tenantconfig.Tenants WHERE Name = N'Visits');

SELECT t.Name, s.Settings
FROM tenantconfig.Tenants t
JOIN tenantconfig.TenantSettings s ON t.Id = s.TenantId
WHERE s.Category = N'ApplicationTemplates' AND s.Target = N'Api'
ORDER BY t.Name;
