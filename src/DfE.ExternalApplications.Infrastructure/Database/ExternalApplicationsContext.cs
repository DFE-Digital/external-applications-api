using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.ExternalApplications.Domain.Common;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Infrastructure.Database.Interceptors;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Infrastructure.Database;

public class ExternalApplicationsContext : DbContext
{
    private readonly IConfiguration? _configuration;
    const string DefaultSchema = "ea";
    private readonly IServiceProvider _serviceProvider = null!;

    public ExternalApplicationsContext()
    {
    }

    public ExternalApplicationsContext(DbContextOptions<ExternalApplicationsContext> options, IConfiguration configuration, IServiceProvider serviceProvider)
        : base(options)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }

    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Template> Templates { get; set; } = null!;
    public DbSet<TemplateVersion> TemplateVersions { get; set; } = null!;
    public DbSet<Domain.Entities.Application> Applications { get; set; } = null!;
    public DbSet<ApplicationResponse> ApplicationResponses { get; set; } = null!;
    public DbSet<Permission> Permissions { get; set; } = null!;
    public DbSet<TaskAssignmentLabel> TaskAssignmentLabels { get; set; } = null!;
    public DbSet<TemplatePermission> TemplatePermissions { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var connectionString = _configuration!.GetConnectionString("DefaultConnection");
            optionsBuilder.UseSqlServer(connectionString);
        }

        var mediator = _serviceProvider.GetRequiredService<IMediator>();
        optionsBuilder.AddInterceptors(new DomainEventDispatcherInterceptor(mediator));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(ConfigureRole);
        modelBuilder.Entity<User>(ConfigureUser);
        modelBuilder.Entity<Template>(ConfigureTemplate);
        modelBuilder.Entity<TemplateVersion>(ConfigureTemplateVersion);
        modelBuilder.Entity<Domain.Entities.Application>(ConfigureApplication);
        modelBuilder.Entity<ApplicationResponse>(ConfigureApplicationResponse);
        modelBuilder.Entity<Permission>(ConfigurePermission);
        modelBuilder.Entity<TemplatePermission>(ConfigureTemplatePermission);
        modelBuilder.Entity<TaskAssignmentLabel>(ConfigureTaskAssignmentLabel);

        base.OnModelCreating(modelBuilder);
    }

    private static void ConfigureRole(EntityTypeBuilder<Role> b)
    {
        b.ToTable("Roles", DefaultSchema);
        b.HasKey(e => e.Id);
        b.Property(e => e.Id)
            .HasColumnName("RoleId")
            .ValueGeneratedOnAdd()
            .HasConversion(v => v.Value, v => new RoleId(v))
            .IsRequired();
        b.Property(e => e.Name)
            .HasColumnName("Name")
            .HasMaxLength(50)
            .IsRequired();
        b.HasIndex(e => e.Name).IsUnique();
    }

    private static void ConfigureUser(EntityTypeBuilder<User> b)
    {
        b.ToTable("Users", DefaultSchema);
        b.HasKey(e => e.Id);
        b.Property(e => e.Id)
            .HasColumnName("UserId")
            .ValueGeneratedOnAdd()
            .HasConversion(v => v.Value, v => new UserId(v))
            .IsRequired();
        b.Property(e => e.RoleId)
            .HasColumnName("RoleId")
            .HasConversion(v => v.Value, v => new RoleId(v))
            .IsRequired();
        b.Property(e => e.Name)
            .HasColumnName("Name")
            .HasMaxLength(100)
            .IsRequired();
        b.Property(e => e.Email)
            .HasColumnName("Email")
            .HasMaxLength(256)
            .IsRequired();
        b.Property(e => e.CreatedOn)
            .HasColumnName("CreatedOn")
            .HasDefaultValueSql("GETDATE()")
            .IsRequired();
        b.Property(e => e.CreatedBy)
            .HasColumnName("CreatedBy")
            .HasConversion(v => v!.Value, v => new UserId(v))
            .IsRequired(false);
        b.Property(e => e.LastModifiedOn)
            .HasColumnName("LastModifiedOn")
            .IsRequired(false);
        b.Property(e => e.LastModifiedBy)
            .HasColumnName("LastModifiedBy")
            .HasConversion(v => v!.Value, v => new UserId(v))
            .IsRequired(false);
        b.Property(u => u.ExternalProviderId)
            .HasMaxLength(100)
            .IsUnicode(false);
        b.HasIndex(u => u.ExternalProviderId).IsUnique();
        b.HasIndex(e => e.Email).IsUnique();
        b.HasOne(e => e.Role)
            .WithMany()
            .HasForeignKey(e => e.RoleId);
        b.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.LastModifiedByUser)
            .WithMany()
            .HasForeignKey(e => e.LastModifiedBy)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasMany(u => u.Permissions)
            .WithOne(p => p.User)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasMany(u => u.TemplatePermissions)
            .WithOne(p => p.User)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureTemplate(EntityTypeBuilder<Template> b)
    {
        b.ToTable("Templates", DefaultSchema);
        b.HasKey(e => e.Id);
        b.Property(e => e.Id)
            .HasColumnName("TemplateId")
            .ValueGeneratedOnAdd()
            .HasConversion(v => v.Value, v => new TemplateId(v))
            .IsRequired();
        b.Property(e => e.Name)
            .HasColumnName("Name")
            .HasMaxLength(100)
            .IsRequired();
        b.Property(e => e.CreatedOn)
            .HasColumnName("CreatedOn")
            .HasDefaultValueSql("GETDATE()")
            .IsRequired();
        b.Property(e => e.CreatedBy)
            .HasColumnName("CreatedBy")
            .HasConversion(v => v.Value, v => new UserId(v))
            .IsRequired();

        b.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureTemplateVersion(EntityTypeBuilder<TemplateVersion> b)
    {
        b.ToTable("TemplateVersions", DefaultSchema);
        b.HasKey(e => e.Id);
        b.Property(e => e.Id)
            .HasColumnName("TemplateVersionId")
            .ValueGeneratedNever()
            .HasConversion(v => v.Value, v => new TemplateVersionId(v))
            .IsRequired();
        b.Property(e => e.TemplateId)
            .HasColumnName("TemplateId")
            .HasConversion(v => v.Value, v => new TemplateId(v))
            .IsRequired();
        b.Property(e => e.VersionNumber)
            .HasColumnName("VersionNumber")
            .HasMaxLength(50)
            .IsRequired();
        b.Property(e => e.JsonSchema)
            .HasColumnName("JsonSchema")
            .IsRequired();
        b.Property(e => e.CreatedOn)
            .HasColumnName("CreatedOn")
            .HasDefaultValueSql("GETDATE()")
            .IsRequired();
        b.Property(e => e.CreatedBy)
            .HasColumnName("CreatedBy")
            .HasConversion(v => v.Value, v => new UserId(v))
            .IsRequired();
        b.Property(e => e.LastModifiedOn)
            .HasColumnName("LastModifiedOn")
            .IsRequired(false);
        b.Property(e => e.LastModifiedBy)
            .HasColumnName("LastModifiedBy")
            .HasConversion(v => v!.Value, v => new UserId(v))
            .IsRequired(false);

        b.HasOne(e => e.Template)
            .WithMany(a => a.TemplateVersions)
            .HasForeignKey(e => e.TemplateId)
            .OnDelete(DeleteBehavior.NoAction);
        b.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.LastModifiedByUser)
            .WithMany()
            .HasForeignKey(e => e.LastModifiedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureApplication(EntityTypeBuilder<Domain.Entities.Application> b)
    {
        b.ToTable("Applications", DefaultSchema);
        b.HasKey(e => e.Id);
        b.Property(e => e.Id)
            .HasColumnName("ApplicationId")
            .ValueGeneratedOnAdd()
            .HasConversion(v => v.Value, v => new ApplicationId(v))
            .IsRequired();
        b.Property(e => e.ApplicationReference)
            .HasColumnName("ApplicationReference")
            .HasMaxLength(20)
            .IsRequired();
        b.Property(e => e.TemplateVersionId)
            .HasColumnName("TemplateVersionId")
            .HasConversion(v => v.Value, v => new TemplateVersionId(v))
            .IsRequired();
        b.Property(e => e.CreatedOn)
            .HasColumnName("CreatedOn")
            .HasDefaultValueSql("GETDATE()")
            .IsRequired();
        b.Property(e => e.CreatedBy)
            .HasColumnName("CreatedBy")
            .HasConversion(v => v.Value, v => new UserId(v))
            .IsRequired();
        b.Property(e => e.Status)
            .HasColumnName("Status")
            .IsRequired(false);
        b.Property(e => e.LastModifiedOn)
            .HasColumnName("LastModifiedOn")
            .IsRequired(false);
        b.Property(e => e.LastModifiedBy)
            .HasColumnName("LastModifiedBy")
            .HasConversion(v => v!.Value, v => new UserId(v))
            .IsRequired(false);

        b.HasOne(e => e.TemplateVersion)
            .WithMany()
            .HasForeignKey(e => e.TemplateVersionId);
        b.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.LastModifiedByUser)
            .WithMany()
            .HasForeignKey(e => e.LastModifiedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureApplicationResponse(EntityTypeBuilder<ApplicationResponse> b)
    {
        b.ToTable("ApplicationResponses", DefaultSchema);
        b.HasKey(e => e.Id);
        b.Property(e => e.Id)
            .HasColumnName("ResponseId")
            .ValueGeneratedNever()
            .HasConversion(v => v.Value, v => new ResponseId(v))
            .IsRequired();
        b.Property(e => e.ApplicationId)
            .HasColumnName("ApplicationId")
            .HasConversion(v => v.Value, v => new ApplicationId(v))
            .IsRequired();
        b.Property(e => e.ResponseBody)
            .HasColumnName("ResponseBody")
            .IsRequired();
        b.Property(e => e.CreatedOn)
            .HasColumnName("CreatedOn")
            .HasDefaultValueSql("GETDATE()")
            .IsRequired();
        b.Property(e => e.CreatedBy)
            .HasColumnName("CreatedBy")
            .HasConversion(v => v.Value, v => new UserId(v))
            .IsRequired();
        b.Property(e => e.LastModifiedOn)
            .HasColumnName("LastModifiedOn")
            .IsRequired(false);
        b.Property(e => e.LastModifiedBy)
            .HasColumnName("LastModifiedBy")
            .HasConversion(v => v!.Value, v => new UserId(v))
            .IsRequired(false);

        b.HasOne(e => e.Application)
            .WithMany(a => a.Responses)
            .HasForeignKey(e => e.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.LastModifiedByUser)
            .WithMany()
            .HasForeignKey(e => e.LastModifiedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigurePermission(EntityTypeBuilder<Permission> b)
    {
        b.ToTable("Permissions", DefaultSchema);
        b.HasKey(e => e.Id);
        b.Property(e => e.Id)
            .HasColumnName("PermissionId")
            .ValueGeneratedNever()
            .HasConversion(v => v.Value, v => new PermissionId(v))
            .IsRequired();
        b.Property(e => e.UserId)
            .HasColumnName("UserId")
            .HasConversion(v => v.Value, v => new UserId(v))
            .IsRequired();
        b.Property(e => e.ApplicationId)
            .HasColumnName("ApplicationId")
            .HasConversion(v => v.Value, v => new ApplicationId(v));
        b.Property(e => e.ResourceType)
            .HasColumnName("ResourceType")
            .HasConversion(
                v => (byte)v,
                v => (ResourceType)v)
            .IsRequired();
        b.Property(e => e.ResourceKey)
            .HasColumnName("ResourceKey")
            .HasMaxLength(200)
            .IsRequired();
        b.Property(e => e.AccessType)
            .HasColumnName("AccessType")
            .HasConversion(
            v => (byte)v,
            v => (AccessType)v)
            .IsRequired();
        b.Property(e => e.GrantedOn)
            .HasColumnName("GrantedOn")
            .HasDefaultValueSql("GETDATE()")
            .IsRequired();
        b.Property(e => e.GrantedBy)
            .HasColumnName("GrantedBy")
            .HasConversion(v => v.Value, v => new UserId(v))
            .IsRequired();
        b.HasOne(p => p.User)
            .WithMany(u => u.Permissions)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasOne(e => e.Application)
            .WithMany()
            .HasForeignKey(e => e.ApplicationId);
        b.HasOne(e => e.GrantedByUser)
            .WithMany()
            .HasForeignKey(e => e.GrantedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureTaskAssignmentLabel(EntityTypeBuilder<TaskAssignmentLabel> b)
    {
        b.ToTable("TaskAssignmentLabels", DefaultSchema);
        b.HasKey(e => e.Id);
        b.Property(e => e.Id)
            .HasColumnName("TaskAssignmentLabelsId")
            .ValueGeneratedOnAdd()
            .HasConversion(v => v.Value, v => new TaskAssignmentLabelId(v))
            .IsRequired();
        b.Property(e => e.Value)
            .HasColumnName("Value")
            .HasMaxLength(100)
            .IsRequired();
        b.Property(e => e.TaskId)
            .HasColumnName("TaskId")
            .HasMaxLength(10)
            .IsRequired();
        b.Property(e => e.UserId)
            .HasColumnName("UserId")
            .HasConversion(v => v!.Value, v => new UserId(v))
            .IsRequired(false);
        b.Property(e => e.CreatedOn)
            .HasColumnName("CreatedOn")
            .HasDefaultValueSql("GETDATE()")
            .IsRequired();
        b.Property(e => e.CreatedBy)
            .HasColumnName("CreatedBy")
            .HasConversion(v => v.Value, v => new UserId(v))
            .IsRequired();

        b.HasOne(e => e.AssignedUser)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureTemplatePermission(EntityTypeBuilder<TemplatePermission> b)
    {
        b.ToTable("TemplatePermissions", DefaultSchema);
        b.HasKey(e => e.Id);
        b.Property(e => e.Id)
            .HasColumnName("TemplatePermissionId")
            .ValueGeneratedOnAdd()
            .HasConversion(v => v.Value, v => new TemplatePermissionId(v))
            .IsRequired();
        b.Property(e => e.UserId)
            .HasColumnName("UserId")
            .HasConversion(v => v.Value, v => new UserId(v))
            .IsRequired();
        b.Property(e => e.TemplateId)
            .HasColumnName("TemplateId")
            .HasConversion(v => v.Value, v => new TemplateId(v))
            .IsRequired();
        b.Property(e => e.AccessType)
            .HasColumnName("AccessType")
            .HasConversion(
                v => (byte)v,
                v => (AccessType)v)
            .IsRequired();
        b.Property(e => e.GrantedOn)
            .HasColumnName("GrantedOn")
            .HasDefaultValueSql("GETDATE()")
            .IsRequired();
        b.Property(e => e.GrantedBy)
            .HasColumnName("GrantedBy")
            .HasConversion(v => v.Value, v => new UserId(v))
            .IsRequired();
        b.HasOne(e => e.Template)
            .WithMany()
            .HasForeignKey(e => e.TemplateId);
        b.HasOne(e => e.GrantedByUser)
            .WithMany()
            .HasForeignKey(e => e.GrantedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }

}
