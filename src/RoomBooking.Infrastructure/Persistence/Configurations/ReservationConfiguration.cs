using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RoomReservations.Domain.Entities;

namespace RoomReservations.Infrastructure.Persistence.Configurations;

public sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    private static readonly ValueConverter<DateTimeOffset, long> DateTimeOffsetConverter =
        new(value => value.UtcTicks, ticks => new DateTimeOffset(ticks, TimeSpan.Zero));

    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("Reservations");
        builder.HasKey(reservation => reservation.Id);
        builder.Property(reservation => reservation.RoomId).IsRequired();
        builder.Property(reservation => reservation.Title).HasMaxLength(500).IsRequired();
        builder.Property(reservation => reservation.CreatedAt)
            .HasConversion(DateTimeOffsetConverter)
            .IsRequired();
        builder.Ignore(reservation => reservation.Range);
        builder.Property(reservation => reservation.Start)
            .HasConversion(DateTimeOffsetConverter)
            .IsRequired();
        builder.Property(reservation => reservation.End)
            .HasConversion(DateTimeOffsetConverter)
            .IsRequired();

        builder.HasOne<Room>()
            .WithMany()
            .HasForeignKey(reservation => reservation.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(reservation => new { reservation.RoomId, reservation.Start, reservation.End });
    }
}
