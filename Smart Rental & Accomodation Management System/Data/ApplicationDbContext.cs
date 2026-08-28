using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Smart_Rental___Accomodation_Management_System.Models;

namespace Smart_Rental___Accomodation_Management_System.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Property> Properties { get; set; } = null!;
        public DbSet<Unit> Units { get; set; } = null!;
        public DbSet<Lease> Leases { get; set; } = null!;
        public DbSet<RentInvoice> RentInvoices { get; set; } = null!;
        public DbSet<Booking> Bookings { get; set; } = null!;
        public DbSet<UtilityBill> UtilityBills { get; set; } = null!;
        public DbSet<UtilityBillShare> UtilityBillShares { get; set; } = null!;
        public DbSet<MessMenu> MessMenus { get; set; } = null!;
        public DbSet<MessFeedback> MessFeedbacks { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Property>()
                .HasOne(p => p.Landlord)
                .WithMany()
                .HasForeignKey(p => p.LandlordId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Unit>()
                .HasOne(u => u.Property)
                .WithMany(p => p.Units)
                .HasForeignKey(u => u.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Lease>()
                .HasOne(l => l.Unit)
                .WithMany(u => u.Leases)
                .HasForeignKey(l => l.UnitId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Lease>()
                .HasOne(l => l.Tenant)
                .WithMany()
                .HasForeignKey(l => l.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RentInvoice>()
                .HasOne(r => r.Lease)
                .WithMany(l => l.RentInvoices)
                .HasForeignKey(r => r.LeaseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Booking>()
                .HasOne(b => b.Unit)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.UnitId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Booking>()
                .HasOne(b => b.Tenant)
                .WithMany()
                .HasForeignKey(b => b.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UtilityBill>()
                .HasOne(u => u.Property)
                .WithMany()
                .HasForeignKey(u => u.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UtilityBillShare>()
                .HasOne(s => s.UtilityBill)
                .WithMany(b => b.Shares)
                .HasForeignKey(s => s.UtilityBillId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UtilityBillShare>()
                .HasOne(s => s.Tenant)
                .WithMany()
                .HasForeignKey(s => s.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<MessMenu>()
                .HasOne(m => m.Property)
                .WithMany()
                .HasForeignKey(m => m.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<MessFeedback>()
                .HasOne(f => f.Property)
                .WithMany()
                .HasForeignKey(f => f.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<MessFeedback>()
                .HasOne(f => f.Tenant)
                .WithMany()
                .HasForeignKey(f => f.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Notification>()
                .HasOne(n => n.Recipient)
                .WithMany()
                .HasForeignKey(n => n.RecipientId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
