using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Smart_Rental___Accomodation_Management_System.Models;

namespace Smart_Rental___Accomodation_Management_System.Data
{
    public static class SeedData
    {
        public static readonly string[] Roles = { "Admin", "Landlord", "Tenant" };

        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            await context.Database.MigrateAsync();

            foreach (var role in Roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            if (await context.Properties.AnyAsync())
            {
                return;
            }

            var landlord = await CreateUserAsync(userManager, "landlord@demo.com", "Demo Landlord", "Landlord");
            var tenant1 = await CreateUserAsync(userManager, "tenant1@demo.com", "Alex Tenant", "Tenant");
            var tenant2 = await CreateUserAsync(userManager, "tenant2@demo.com", "Sam Tenant", "Tenant");
            await CreateUserAsync(userManager, "admin@demo.com", "Demo Admin", "Admin");

            var property = new Property
            {
                LandlordId = landlord.Id,
                Name = "Greenview Residence",
                Address = "12 Greenview Road, Colombo"
            };

            var unit1 = new Unit { Property = property, Name = "Room A1", UnitType = UnitType.PrivateRoom, MonthlyRent = 350m, Capacity = 1 };
            var unit2 = new Unit { Property = property, Name = "Room A2", UnitType = UnitType.SharedRoom, MonthlyRent = 220m, Capacity = 2 };
            var unit3 = new Unit { Property = property, Name = "Family Unit B1", UnitType = UnitType.FamilyUnit, BhkType = BhkType.TwoBHK, MonthlyRent = 600m, Capacity = 6 };
            property.Units.AddRange(new[] { unit1, unit2, unit3 });

            context.Properties.Add(property);
            await context.SaveChangesAsync();

            var lease1 = new Lease { UnitId = unit1.Id, TenantId = tenant1.Id, StartDate = DateTime.UtcNow.AddMonths(-4) };
            var lease2 = new Lease { UnitId = unit2.Id, TenantId = tenant2.Id, StartDate = DateTime.UtcNow.AddMonths(-2) };
            context.Leases.AddRange(lease1, lease2);
            await context.SaveChangesAsync();

            var today = DateTime.UtcNow.Date;
            var invoices = new List<RentInvoice>();

            for (int monthsAgo = 3; monthsAgo >= 0; monthsAgo--)
            {
                var due = new DateTime(today.Year, today.Month, 5).AddMonths(-monthsAgo);

                invoices.Add(new RentInvoice
                {
                    LeaseId = lease1.Id,
                    PeriodMonth = due.Month,
                    PeriodYear = due.Year,
                    Amount = unit1.MonthlyRent,
                    DueDate = due,
                    Status = monthsAgo == 0 ? InvoiceStatus.Pending : InvoiceStatus.Paid,
                    PaidDate = monthsAgo == 0 ? null : due.AddDays(2)
                });

                invoices.Add(new RentInvoice
                {
                    LeaseId = lease2.Id,
                    PeriodMonth = due.Month,
                    PeriodYear = due.Year,
                    Amount = unit2.MonthlyRent,
                    DueDate = due,
                    Status = monthsAgo == 1 ? InvoiceStatus.Overdue : (monthsAgo == 0 ? InvoiceStatus.Pending : InvoiceStatus.Paid),
                    PaidDate = monthsAgo <= 1 ? null : due.AddDays(5)
                });
            }

            context.RentInvoices.AddRange(invoices);
            await context.SaveChangesAsync();

            var utilityBill = new UtilityBill
            {
                PropertyId = property.Id,
                BillType = UtilityBillType.Electricity,
                Amount = 60m,
                PeriodMonth = today.Month,
                PeriodYear = today.Year,
                DueDate = new DateTime(today.Year, today.Month, 20),
                SplitMethod = UtilityBillSplitMethod.Equal,
                Shares = new List<UtilityBillShare>
                {
                    new() { TenantId = tenant1.Id, ShareAmount = 30m },
                    new() { TenantId = tenant2.Id, ShareAmount = 30m }
                }
            };
            context.UtilityBills.Add(utilityBill);
            await context.SaveChangesAsync();

            var menu = new List<MessMenu>
            {
                new() { PropertyId = property.Id, DayOfWeek = DayOfWeek.Monday, MealType = MealType.Breakfast, Description = "Toast, eggs, and fruit" },
                new() { PropertyId = property.Id, DayOfWeek = DayOfWeek.Monday, MealType = MealType.Lunch, Description = "Rice, chicken curry, and salad" },
                new() { PropertyId = property.Id, DayOfWeek = DayOfWeek.Monday, MealType = MealType.Dinner, Description = "Noodles and vegetable stir-fry" },
                new() { PropertyId = property.Id, DayOfWeek = DayOfWeek.Tuesday, MealType = MealType.Lunch, Description = "Rice, fish curry, and greens" },
                new() { PropertyId = property.Id, DayOfWeek = DayOfWeek.Wednesday, MealType = MealType.Dinner, Description = "Pasta with tomato sauce" }
            };
            context.MessMenus.AddRange(menu);
            await context.SaveChangesAsync();

            var feedback = new List<MessFeedback>
            {
                new() { PropertyId = property.Id, TenantId = tenant1.Id, DayOfWeek = DayOfWeek.Monday, MealType = MealType.Lunch, Rating = 4, Comment = "Good portion size" },
                new() { PropertyId = property.Id, TenantId = tenant2.Id, DayOfWeek = DayOfWeek.Monday, MealType = MealType.Lunch, Rating = 5, Comment = "Loved the curry" }
            };
            context.MessFeedbacks.AddRange(feedback);
            await context.SaveChangesAsync();
        }

        private static async Task<ApplicationUser> CreateUserAsync(UserManager<ApplicationUser> userManager, string email, string fullName, string role)
        {
            var existing = await userManager.FindByEmailAsync(email);
            if (existing != null)
            {
                return existing;
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, "Demo@12345");
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to seed user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            await userManager.AddToRoleAsync(user, role);
            return user;
        }
    }
}
