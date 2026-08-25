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
        }
    }
}
