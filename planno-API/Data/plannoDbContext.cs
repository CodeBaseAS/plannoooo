using Microsoft.EntityFrameworkCore;
using Models.Models;

public class PlannoDbContext : DbContext
{
    public PlannoDbContext(DbContextOptions<PlannoDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<UserAppointment> UserAppointments => Set<UserAppointment>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<CardDetails> CardDetails => Set<CardDetails>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Invoice> Invoices => Set<Invoice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("Id");
            e.Property(x => x.Username).HasColumnName("username").HasMaxLength(50).IsRequired();
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(200).IsRequired();
            e.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(250).IsRequired();
            e.Property(x => x.ActiveSubscriptionId).HasColumnName("active_subscription_id");
            e.Property(x => x.SubscriptionExpiresAt).HasColumnName("subscription_expires_at").HasColumnType("datetime");

            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.Username).IsUnique();

            e.HasOne(x => x.ActiveSubscription)
             .WithMany()
             .HasForeignKey(x => x.ActiveSubscriptionId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Appointment>(e =>
        {
            e.ToTable("appointments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
            e.Property(x => x.StartTime).HasColumnName("start_time").HasColumnType("datetime").IsRequired();
            e.Property(x => x.EndTime).HasColumnName("end_time").HasColumnType("datetime").IsRequired();
            e.Property(x => x.Location).HasColumnName("location").HasMaxLength(50);
            e.Property(x => x.IsAllDay).HasColumnName("is_all_day").IsRequired();
        });

        modelBuilder.Entity<UserAppointment>(e =>
        {
            e.ToTable("user_appointments");
            e.HasKey(x => x.Id);
            e.Property(x => x.UserId).HasColumnName("user_Id");
            e.Property(x => x.AppointmentId).HasColumnName("appointment_Id");

            e.HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Appointment)
             .WithMany()
             .HasForeignKey(x => x.AppointmentId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.UserId, x.AppointmentId }).IsUnique();
        });

        modelBuilder.Entity<Subscription>(e =>
        {
            e.ToTable("subscriptions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Description).HasColumnName("description").HasMaxLength(250).IsRequired();
            e.Property(x => x.Price).HasColumnName("price").HasColumnType("decimal(10,2)").IsRequired();
            e.Property(x => x.Interval).HasColumnName("interval").IsRequired();
            e.HasData(
                new Subscription { Id = 1, Description = "Premium Monatlich", Price = 2.99m, Interval = 1 },
                new Subscription { Id = 2, Description = "Premium Jährlich", Price = 29.99m, Interval = 12 }
            );
        });

        modelBuilder.Entity<CardDetails>(e =>
        {
            e.ToTable("card_details");
            e.HasKey(x => x.Id);
            e.Property(x => x.CardBrand).HasColumnName("card_brand").HasMaxLength(50).IsRequired();
            e.Property(x => x.CardLast4).HasColumnName("card_last4").HasMaxLength(4).IsRequired();
            e.Property(x => x.ExpiresAt).HasColumnName("expires_at").HasColumnType("datetime").IsRequired();
            e.Property(x => x.UserId).HasColumnName("user_Id");

            e.HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Payment>(e =>
        {
            e.ToTable("payments");
            e.HasKey(x => x.Id);
            e.Property(x => x.CardId).HasColumnName("card_Id");
            e.Property(x => x.SubscriptionId).HasColumnName("subscription_Id");
            e.Property(x => x.UserId).HasColumnName("user_Id");
            e.Property(x => x.PaidAt).HasColumnName("paid_at").HasColumnType("datetime").IsRequired();
            e.Property(x => x.Amount).HasColumnName("amount").HasColumnType("decimal(10,2)").IsRequired();

            e.HasOne(x => x.Card)
             .WithMany()
             .HasForeignKey(x => x.CardId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Subscription)
             .WithMany()
             .HasForeignKey(x => x.SubscriptionId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Invoice>(e =>
        {
            e.ToTable("invoices");
            e.HasKey(x => x.Id);
            e.Property(x => x.PaymentId).HasColumnName("payment_Id");
            e.Property(x => x.InvoiceNumber).HasColumnName("invoice_number").HasMaxLength(200).IsRequired();
            e.Property(x => x.IssuedAt).HasColumnName("issued_at").HasColumnType("datetime").IsRequired();
            e.Property(x => x.PdfUrl).HasColumnName("pdf_url").HasMaxLength(250).IsRequired();

            e.HasIndex(x => x.InvoiceNumber).IsUnique();

            e.HasOne(x => x.Payment)
             .WithMany()
             .HasForeignKey(x => x.PaymentId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
