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
    public DbSet<UserTemplateAccess> UserTemplateAccesses { get; set; } = null!;
    public DbSet<TaskAssignmentLabel> TaskAssignmentLabels { get; set; } = null!;

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
        modelBuilder.Entity<UserTemplateAccess>(ConfigureUserTemplateAccess);
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
            .ValueGeneratedOnAdd()
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
            .WithMany()
            .HasForeignKey(e => e.TemplateId);
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
            .ValueGeneratedOnAdd()
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
            .WithMany()
            .HasForeignKey(e => e.ApplicationId);
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
            .ValueGeneratedOnAdd()
            .HasConversion(v => v.Value, v => new PermissionId(v))
            .IsRequired();
        b.Property(e => e.UserId)
            .HasColumnName("UserId")
            .HasConversion(v => v.Value, v => new UserId(v))
            .IsRequired();
        b.Property(e => e.ApplicationId)
            .HasColumnName("ApplicationId")
            .HasConversion(v => v.Value, v => new ApplicationId(v))
            .IsRequired();
        b.Property(e => e.ResourceKey)
            .HasColumnName("ResourceKey")
            .HasMaxLength(200)
            .IsRequired();
        b.Property(e => e.AccessType)
            .HasColumnName("AccessType")
            .IsRequired();
        b.Property(e => e.GrantedOn)
            .HasColumnName("GrantedOn")
            .HasDefaultValueSql("GETDATE()")
            .IsRequired();
        b.Property(e => e.GrantedBy)
            .HasColumnName("GrantedBy")
            .HasConversion(v => v.Value, v => new UserId(v))
            .IsRequired();

        b.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId);
        b.HasOne(e => e.Application)
            .WithMany()
            .HasForeignKey(e => e.ApplicationId);
        b.HasOne(e => e.GrantedByUser)
            .WithMany()
            .HasForeignKey(e => e.GrantedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureUserTemplateAccess(EntityTypeBuilder<UserTemplateAccess> b)
    {
        b.ToTable("UserTemplateAccess", DefaultSchema);
        b.HasKey(e => e.Id);
        b.Property(e => e.Id)
            .HasColumnName("UserTemplateAccessId")
            .ValueGeneratedOnAdd()
            .HasConversion(v => v.Value, v => new UserTemplateAccessId(v))
            .IsRequired();
        b.Property(e => e.UserId)
            .HasColumnName("UserId")
            .HasConversion(v => v.Value, v => new UserId(v))
            .IsRequired();
        b.Property(e => e.TemplateId)
            .HasColumnName("TemplateId")
            .HasConversion(v => v.Value, v => new TemplateId(v))
            .IsRequired();
        b.Property(e => e.GrantedOn)
            .HasColumnName("GrantedOn")
            .HasDefaultValueSql("GETDATE()")
            .IsRequired();
        b.Property(e => e.GrantedBy)
            .HasColumnName("GrantedBy")
            .HasConversion(v => v.Value, v => new UserId(v))
            .IsRequired();

        b.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId);
        b.HasOne(e => e.Template)
            .WithMany()
            .HasForeignKey(e => e.TemplateId);
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

    //private static void ConfigureSchool(EntityTypeBuilder<School> schoolConfiguration)
    //{
    //    schoolConfiguration.HasKey(s => s.Id);
    //    schoolConfiguration.ToTable("Schools", DefaultSchema);
    //    schoolConfiguration.Property(e => e.Id)
    //        .ValueGeneratedOnAdd()
    //        .HasConversion(
    //            v => v!.Value,
    //            v => new SchoolId(v));

    //    schoolConfiguration.Property(e => e.PrincipalId)
    //        .HasConversion(
    //            v => v.Value,
    //            v => new PrincipalId(v));

    //    schoolConfiguration.Property(e => e.SchoolName).HasColumnName("SchoolName");

    //    schoolConfiguration.OwnsOne(e => e.NameDetails, nameDetails =>
    //    {
    //        nameDetails.Property(nd => nd.NameListAs).HasColumnName("NameListAs");
    //        nameDetails.Property(nd => nd.NameDisplayAs).HasColumnName("NameDisplayAs");
    //        nameDetails.Property(nd => nd.NameFullTitle).HasColumnName("NameFullTitle");
    //    });

    //    schoolConfiguration.Property(e => e.LastRefresh).HasColumnName("LastRefresh");

    //    schoolConfiguration
    //        .HasOne(c => c.PrincipalDetails)
    //        .WithOne()
    //        .HasForeignKey<School>(c => c.PrincipalId)
    //        .HasPrincipalKey<PrincipalDetails>(m => m.Id);
    //}

}
