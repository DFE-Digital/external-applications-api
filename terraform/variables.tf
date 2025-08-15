variable "azure_client_id" {
  description = "Service Principal Client ID"
  type        = string
}

variable "azure_client_secret" {
  description = "Service Principal Client Secret"
  type        = string
  sensitive   = true
}

variable "azure_tenant_id" {
  description = "Service Principal Tenant ID"
  type        = string
}

variable "azure_subscription_id" {
  description = "Service Principal Subscription ID"
  type        = string
}

variable "environment" {
  description = "Environment name. Will be used along with `project_name` as a prefix for all resources."
  type        = string
}

variable "key_vault_access_ipv4" {
  description = "List of IPv4 Addresses that are permitted to access the Key Vault"
  type        = list(string)
}

variable "tfvars_filename" {
  description = "tfvars filename. This file is uploaded and stored encrupted within Key Vault, to ensure that the latest tfvars are stored in a shared place."
  type        = string
}

variable "project_name" {
  description = "Project name. Will be used along with `environment` as a prefix for all resources."
  type        = string
}

variable "azure_location" {
  description = "Azure location in which to launch resources."
  type        = string
}

variable "tags" {
  description = "Tags to be applied to all resources"
  type        = map(string)
}

variable "container_apps_infra_subnet_cidr" {
  description = "Specify a subnet prefix to use for the container_apps_infra subnet"
  type        = string
  default     = "172.16.110.16/28"
}

variable "storage_subnet_cidr" {
  description = "Specify a subnet prefix to use for the storage subnet"
  type        = string
}

variable "mssql_private_endpoint_subnet_cidr" {
  description = "Specify a subnet prefix to use for the mssql private endpoint subnet"
  type        = string
}

variable "enable_container_registry" {
  description = "Set to true to create a container registry"
  type        = bool
}

variable "registry_admin_enabled" {
  description = "Do you want to enable access key based authentication for your Container Registry?"
  type        = bool
  default     = true
}

variable "registry_server" {
  description = "Container registry server (required if `enable_container_registry` is false)"
  type        = string
  default     = ""
}

variable "registry_use_managed_identity" {
  description = "Create a User-Assigned Managed Identity for the Container App. Note: If you do not have 'Microsoft.Authorization/roleAssignments/write' permission, you will need to manually assign the 'AcrPull' Role to the identity"
  type        = bool
  default     = true
}

variable "registry_managed_identity_assign_role" {
  description = "Assign the 'AcrPull' Role to the Container App User-Assigned Managed Identity. Note: If you do not have 'Microsoft.Authorization/roleAssignments/write' permission, you will need to manually assign the 'AcrPull' Role to the identity"
  type        = bool
  default     = false
}

variable "image_name" {
  description = "Image name"
  type        = string
}

variable "container_command" {
  description = "Container command"
  type        = list(any)
}

variable "container_secret_environment_variables" {
  description = "Container secret environment variables"
  type        = map(string)
  sensitive   = true
}

variable "container_scale_http_concurrency" {
  description = "When the number of concurrent HTTP requests exceeds this value, then another replica is added. Replicas continue to add to the pool up to the max-replicas amount."
  type        = number
  default     = 10
}

variable "enable_dns_zone" {
  description = "Conditionally create a DNS zone"
  type        = bool
}

variable "dns_zone_domain_name" {
  description = "DNS zone domain name. If created, records will automatically be created to point to the CDN."
  type        = string
}

variable "dns_ns_records" {
  description = "DNS NS records to add to the DNS Zone"
  type = map(
    object({
      ttl : optional(number, 300),
      records : list(string)
    })
  )
}

variable "dns_txt_records" {
  description = "DNS TXT records to add to the DNS Zone"
  type = map(
    object({
      ttl : optional(number, 300),
      records : list(string)
    })
  )
}

variable "dns_mx_records" {
  description = "DNS MX records to add to the DNS Zone"
  type = map(
    object({
      ttl : optional(number, 300),
      records : list(
        object({
          preference : number,
          exchange : string
        })
      )
    })
  )
  default = {}
}

variable "container_apps_allow_ips_inbound" {
  description = "Restricts access to the Container Apps by creating a network security group rule that only allow inbound traffic from the provided list of IPs"
  type        = list(string)
  default     = []
}

variable "enable_monitoring" {
  description = "Create an App Insights instance and notification group for the Container App"
  type        = bool
}

variable "monitor_email_receivers" {
  description = "A list of email addresses that should be notified by monitoring alerts"
  type        = list(string)
}

variable "container_health_probe_path" {
  description = "Specifies the path that is used to determine the liveness of the Container"
  type        = string
}

variable "monitor_endpoint_healthcheck" {
  description = "Specify a route that should be monitored for a 200 OK status"
  type        = string
}

variable "existing_logic_app_workflow" {
  description = "Name, and Resource Group of an existing Logic App Workflow. Leave empty to create a new Resource"
  type = object({
    name : string
    resource_group_name : string
  })
  default = {
    name                = ""
    resource_group_name = ""
  }
}

variable "existing_network_watcher_name" {
  description = "Use an existing network watcher to add flow logs."
  type        = string
}

variable "existing_network_watcher_resource_group_name" {
  description = "Existing network watcher resource group."
  type        = string
}

variable "statuscake_api_token" {
  description = "API token for StatusCake"
  type        = string
  sensitive   = true
  default     = "00000000000000000000000000000"
}

variable "statuscake_contact_group_name" {
  description = "Name of the contact group in StatusCake"
  type        = string
  default     = ""
}

variable "statuscake_contact_group_integrations" {
  description = "List of Integration IDs to connect to your Contact Group"
  type        = list(string)
  default     = []
}

variable "statuscake_monitored_resource_addresses" {
  description = "The URLs to perform TLS checks on"
  type        = list(string)
  default     = []
}

variable "statuscake_contact_group_email_addresses" {
  description = "List of email address that should receive notifications from StatusCake"
  type        = list(string)
  default     = []
}

variable "custom_container_apps" {
  description = "Custom container apps, by default deployed within the container app environment managed by this module."
  type = map(object({
    container_app_environment_id = optional(string, "")
    resource_group_name          = optional(string, "")
    revision_mode                = optional(string, "Single")
    container_port               = optional(number, 0)
    ingress = optional(object({
      external_enabled = optional(bool, true)
      target_port      = optional(number, null)
      traffic_weight = object({
        percentage = optional(number, 100)
      })
      cdn_frontdoor_custom_domain                = optional(string, "")
      cdn_frontdoor_origin_fqdn_override         = optional(string, "")
      cdn_frontdoor_origin_host_header_override  = optional(string, "")
      enable_cdn_frontdoor_health_probe          = optional(bool, false)
      cdn_frontdoor_health_probe_protocol        = optional(string, "")
      cdn_frontdoor_health_probe_interval        = optional(number, 120)
      cdn_frontdoor_health_probe_request_type    = optional(string, "")
      cdn_frontdoor_health_probe_path            = optional(string, "")
      cdn_frontdoor_forwarding_protocol_override = optional(string, "")
    }), null)
    identity = optional(list(object({
      type         = string
      identity_ids = list(string)
    })), [])
    secrets = optional(list(object({
      name  = string
      value = string
    })), [])
    registry = optional(object({
      server               = optional(string, "")
      username             = optional(string, "")
      password_secret_name = optional(string, "")
      identity             = optional(string, "")
    }), null),
    image   = string
    cpu     = number
    memory  = number
    command = list(string)
    liveness_probes = optional(list(object({
      interval_seconds = number
      transport        = string
      port             = number
      path             = optional(string, null)
    })), [])
    env = optional(list(object({
      name      = string
      value     = optional(string, null)
      secretRef = optional(string, null)
    })), [])
    min_replicas = number
    max_replicas = number
  }))
  default = {}
}

variable "container_min_replicas" {
  description = "Container min replicas"
  type        = number
  default     = 1
}

variable "enable_health_insights_api" {
  description = "Deploys a Function App that exposes the last 3 HTTP Web Tests via an API endpoint. 'enable_app_insights_integration' and 'enable_monitoring' must be set to 'true'."
  type        = bool
  default     = false
}

variable "health_insights_api_cors_origins" {
  description = "List of hostnames that are permitted to contact the Health insights API"
  type        = list(string)
  default     = ["*"]
}

variable "health_insights_api_ipv4_allow_list" {
  description = "List of IPv4 addresses that are permitted to contact the Health insights API"
  type        = list(string)
  default     = []
}

variable "container_port" {
  description = "Container port"
  type        = number
  default     = 8080
}

variable "enable_init_container" {
  description = "Deploy an Init Container. Init containers run before the primary app container and are used to perform initialization tasks such as downloading data or preparing the environment"
  type        = bool
  default     = false
}

variable "init_container_image" {
  description = "Image name for the Init Container. Leave blank to use the same Container image from the primary app"
  type        = string
  default     = ""
}

variable "init_container_command" {
  description = "Container command for the Init Container"
  type        = list(any)
  default     = []
}

variable "monitor_http_availability_fqdn" {
  description = "Specify a FQDN to monitor for HTTP Availability. Leave unset to dynamically calculate the correct FQDN"
  type        = string
  default     = ""
}

variable "dns_alias_records" {
  description = "DNS ALIAS records to add to the DNS Zone"
  type = map(
    object({
      ttl : optional(number, 300),
      target_resource_id : string
    })
  )
  default = {}
}

variable "enable_monitoring_traces" {
  description = "Monitor App Insights traces for error messages"
  type        = bool
  default     = true
}

variable "existing_container_app_environment" {
  description = "Conditionally launch resources into an existing Container App environment. Specifying this will NOT create an environment."
  type = object({
    name           = string
    resource_group = string
  })
}

variable "existing_virtual_network" {
  description = "Conditionally use an existing virtual network. The `virtual_network_address_space` must match an existing address space in the VNet. This also requires the resource group name."
  type        = string
}

variable "existing_resource_group" {
  description = "Conditionally launch resources into an existing resource group. Specifying this will NOT create a resource group."
  type        = string
}

variable "container_app_name_override" {
  type        = string
  description = "A custom name for the Container App"
  default     = ""
}

variable "enable_mssql_database" {
  description = "Set to true to create an Azure SQL server/database, with a private endpoint within the virtual network"
  type        = bool
  default     = false
}

variable "mssql_server_admin_password" {
  description = "The local administrator password for the MSSQL server"
  type        = string
  default     = ""
  sensitive   = true
}

variable "mssql_azuread_admin_username" {
  description = "Username of a User within Azure AD that you want to assign as the SQL Server Administrator"
  type        = string
  default     = ""
}

variable "mssql_azuread_admin_object_id" {
  description = "Object ID of a User within Azure AD that you want to assign as the SQL Server Administrator"
  type        = string
  default     = ""
}

variable "mssql_sku_name" {
  description = "Specifies the name of the SKU used by the database"
  type        = string
  default     = "Basic"
}

variable "mssql_database_name" {
  description = "The name of the MSSQL database to create. Must be set if `enable_mssql_database` is true"
  type        = string
  default     = ""
}

variable "mssql_firewall_ipv4_allow_list" {
  description = "A list of IPv4 Addresses that require remote access to the MSSQL Server"
  type = map(object({
    start_ip_range : string,
    end_ip_range : optional(string, "")
  }))
  default = {}
}

variable "enable_mssql_vulnerability_assessment" {
  description = "Vulnerability assessment can discover, track, and help you remediate potential database vulnerabilities"
  type        = bool
  default     = true
}

variable "mssql_managed_identity_assign_role" {
  description = "Assign the 'Storage Blob Data Contributor' Role to the SQL Server User-Assigned Managed Identity. Note: If you do not have 'Microsoft.Authorization/roleAssignments/write' permission, you will need to manually assign the 'Storage Blob Data Contributor' Role to the identity"
  type        = bool
  default     = false
}

variable "mssql_server_public_access_enabled" {
  description = "Enable public internet access to your MSSQL instance. Be sure to specify 'mssql_firewall_ipv4_allow_list' to restrict inbound connections"
  type        = bool
  default     = true
}

variable "mssql_azuread_auth_only" {
  description = "Set to true to only permit SQL logins from Azure AD users"
  type        = bool
  default     = false
}

variable "restrict_container_apps_to_cdn_inbound_only" {
  description = "Restricts access to the Container Apps by creating a network security group rule that only allows 'AzureFrontDoor.Backend' inbound, and attaches it to the subnet of the container app environment."
  type        = bool
  default     = false
}

variable "enable_container_app_file_share" {
  description = "Create an Azure Storage Account and File Share to be mounted to the Container Apps"
  type        = bool
  default     = true
}

variable "storage_account_file_share_quota_gb" {
  description = "The maximum size of the share, in gigabytes."
  type        = number
  default     = 1
}

variable "container_app_file_share_mount_path" {
  description = "A path inside your container where the File Share will be mounted to"
  type        = string
  default     = "/uploads"
}

variable "storage_account_public_access_enabled" {
  description = "Should the Azure Storage Account have Public visibility?"
  type        = bool
  default     = false
}

variable "storage_account_ipv4_allow_list" {
  description = "A list of public IPv4 address to grant access to the Storage Account"
  type        = list(string)
  default     = []
}

variable "enable_signalr" {
  description = "Enable serverless Azure SignalR service"
  type        = bool
}

variable "signalr_sku" {
  description = "SignalR SKU name"
  type        = string
  default     = "Free_F1"
}

variable "signalr_service_mode" {
  description = "SignalR service mode"
  type        = string
  default     = "Default"
}
