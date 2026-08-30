using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AccessManagement.Domain.Authorization;

namespace AccessManagement.Infrastructure.Data.Configurations;

public class RoleGroupConfiguration : IEntityTypeConfiguration<RoleGroup>
{
    public void Configure(EntityTypeBuilder<RoleGroup> builder)
    {
        builder.ToTable("RoleGroup");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Code).HasMaxLength(64).IsRequired();
        builder.Property(g => g.Name).HasMaxLength(200).IsRequired();
        builder.Property(g => g.Status).HasConversion<int>();
        builder.HasIndex(g => g.Code).IsUnique();
        builder.HasMany(g => g.Members).WithOne(m => m.RoleGroup).HasForeignKey(m => m.RoleGroupId);
    }
}

public class RoleGroupMemberConfiguration : IEntityTypeConfiguration<RoleGroupMember>
{
    public void Configure(EntityTypeBuilder<RoleGroupMember> builder)
    {
        builder.ToTable("RoleGroupMember");
        builder.HasKey(m => m.Id);
        builder.HasIndex(m => new { m.RoleGroupId, m.RoleId }).IsUnique();
        builder.HasOne(m => m.Role).WithMany().HasForeignKey(m => m.RoleId).OnDelete(DeleteBehavior.Restrict);
    }
}
