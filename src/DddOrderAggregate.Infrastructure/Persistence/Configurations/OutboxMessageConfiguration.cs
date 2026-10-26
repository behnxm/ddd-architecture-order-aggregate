using DddOrderAggregate.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DddOrderAggregate.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core mapping for the OutboxMessage entity.
/// </summary>
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.ContractName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(m => m.Payload)
            .IsRequired();

        builder.Property(m => m.OccurredOn)
            .IsRequired();

        builder.Property(m => m.ProcessedOn);

        builder.Property(m => m.Attempts)
            .IsRequired();

        builder.Property(m => m.Error)
            .HasMaxLength(2000);

        builder.HasIndex(m => new { m.ProcessedOn, m.Attempts })
            .HasDatabaseName("ix_outbox_messages_processing");
    }
}
