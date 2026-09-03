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

            if (!await context.AppSettings.AnyAsync())
            {
                context.AppSettings.Add(new AppSetting());
                await context.SaveChangesAsync();
            }

            if (!await context.InfoPages.AnyAsync())
            {
                await SeedInfoPagesAsync(context);
            }

            if (!await context.SupportedCities.AnyAsync())
            {
                await SeedLocationsAsync(context);
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

        private static async Task SeedLocationsAsync(ApplicationDbContext context)
        {
            var citiesWithAreas = new Dictionary<string, string[]>
            {
                ["Karachi"] = new[] { "Clifton", "DHA", "Gulshan-e-Iqbal", "North Nazimabad", "Saddar", "Malir" },
                ["Lahore"] = new[] { "Gulberg", "DHA", "Johar Town", "Model Town", "Bahria Town", "Cantt" },
                ["Islamabad"] = new[] { "F-6", "F-7", "F-8", "G-9", "G-11", "Bahria Town" },
                ["Rawalpindi"] = new[] { "Saddar", "Bahria Town", "Satellite Town", "Cantt" },
                ["Faisalabad"] = new[] { "Madina Town", "Peoples Colony", "Susan Road", "D Ground" },
                ["Multan"] = new[] { "Cantt", "Gulgasht Colony", "Shah Rukn-e-Alam", "Model Town" },
                ["Peshawar"] = new[] { "University Town", "Hayatabad", "Cantt", "Gulbahar" },
                ["Quetta"] = new[] { "Cantt", "Jinnah Town", "Satellite Town" },
                ["Sialkot"] = new[] { "Cantt", "Model Town", "Paris Road" },
                ["Hyderabad"] = new[] { "Latifabad", "Qasimabad", "Cantt" }
            };

            foreach (var (cityName, areaNames) in citiesWithAreas)
            {
                var city = new SupportedCity { Name = cityName };
                context.SupportedCities.Add(city);
                await context.SaveChangesAsync();

                context.SupportedAreas.AddRange(areaNames.Select(a => new SupportedArea { SupportedCityId = city.Id, Name = a }));
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedInfoPagesAsync(ApplicationDbContext context)
        {
            var now = DateTime.UtcNow;

            var posts = new List<InfoPage>
            {
                new()
                {
                    Title = "For Landlords",
                    Slug = "for-landlords",
                    Category = InfoPageCategory.ForLandlords,
                    Excerpt = "List unlimited properties, collect rent without chasing it, and manage everything from one dashboard.",
                    Content = """
                    RehLe is built for the parts of being a landlord that actually take up your time. List as many properties and units as you manage, with no per-listing fee — one flat monthly subscription covers all of them.

                    Rent collection is built around a simple idea: a paper trail beats a memory. A tenant marks an invoice paid, you confirm it was received, and everything is timestamped. If a payment claim doesn't match what actually came in, you can mark it not received instead, which opens a dispute automatically rather than leaving it as an awkward conversation with no record.

                    Utility bills split automatically — equally, by percentage, or by per-meter consumption — and a tenant with their own meter is excluded from the shared bill without you having to remember to adjust it manually every time.

                    When a tenant reports a maintenance issue, it comes with a description and an optional photo, and you track it through Open, In Progress, and Resolved so nothing falls through the cracks. Booking requests, lease management, tenant reviews, and Excel/PDF reports round out the rest — everything a landlord actually needs, nothing you have to configure to get there.

                    Every new landlord account starts with a 30-day free trial, no card required. After that, a small flat monthly subscription keeps everything running.
                    """,
                    IsPublished = true,
                    PublishedAt = now.AddDays(-2),
                    CreatedAt = now.AddDays(-2)
                },
                new()
                {
                    Title = "For Tenants",
                    Slug = "for-tenants",
                    Category = InfoPageCategory.ForTenants,
                    Excerpt = "Search by city and area, see the exact location on a map, and pay rent without the awkward reminders.",
                    Content = """
                    Finding a place shouldn't mean scrolling through listings with no real filtering. RehLe lets you search by city and then narrow down to the specific area you actually want, with the exact location shown on a map before you ever contact the landlord — not just a vague neighborhood name.

                    Every listing shows real availability, monthly rent, unit type, and — where tenants have actually lived there before — a rating and reviews, so you're not going in blind.

                    Once you've moved in, rent and utility payments happen the same way: you mark a payment made, your landlord confirms it, and if something doesn't match, either side can raise it as a dispute instead of it just sitting unresolved. If you split a place with roommates, utility bills are divided fairly — equally, by percentage, or by actual per-meter usage.

                    If something breaks, you can report it with a photo and a description, and track it through to resolved instead of wondering whether the landlord even saw your message.

                    It's completely free for tenants — no account fee, no per-payment charge, nothing hidden.
                    """,
                    IsPublished = true,
                    PublishedAt = now.AddDays(-2),
                    CreatedAt = now.AddDays(-2)
                },
                new()
                {
                    Title = "How It Works",
                    Slug = "how-it-works",
                    Category = InfoPageCategory.HowItWorks,
                    Excerpt = "From listing to move-in to rent day — how a lease actually flows through RehLe.",
                    Content = """
                    A landlord starts by adding a property and its units — private rooms, shared rooms, or full family units — with rent, capacity, and photos. Addresses are geocoded automatically, and the pin can be dragged to correct it if the automatic placement is slightly off.

                    A tenant searches by city and area, filters by rent and unit type, and requests to book a unit that's available. The landlord reviews the request and approves or rejects it — approving automatically creates the lease.

                    From there, rent invoices are generated on a schedule, with due-soon and overdue reminders sent automatically. The tenant marks a payment made, the landlord confirms it, and the whole history is kept — including any disputes and how they were resolved.

                    Utility bills, when there are any, get split across active tenants using whichever method the landlord chooses. Maintenance issues get reported with photos and tracked through to resolved. And once a tenant's lease has run its course, they can leave a review that shows up for the next person considering that property.

                    Every step generates a notification — in-app always, by email if you haven't turned that off in Settings — so nothing depends on someone remembering to check.
                    """,
                    IsPublished = true,
                    PublishedAt = now.AddDays(-2),
                    CreatedAt = now.AddDays(-2)
                },
                new()
                {
                    Title = "Property Listings",
                    Slug = "property-listings",
                    Category = InfoPageCategory.ForLandlords,
                    Excerpt = "List unlimited properties and units, with photos, exact map locations, and no per-listing fee.",
                    Content = """
                    Add as many properties and units as you manage — private rooms, shared rooms, or full family units — for one flat monthly price, not a fee per listing.

                    Each unit gets its own photos, rent, capacity, and availability. Addresses are geocoded automatically, and the pin can be dragged to correct it if the automatic placement is slightly off, so tenants see the exact location before they ever contact you.

                    Delist a property when it's no longer available and bring it back later without losing its history, reviews, or past leases.
                    """,
                    IsPublished = true,
                    PublishedAt = now.AddDays(-21),
                    CreatedAt = now.AddDays(-21)
                },
                new()
                {
                    Title = "Rent Collection",
                    Slug = "rent-collection",
                    Category = InfoPageCategory.ForLandlords,
                    Excerpt = "A tenant marks a payment made, you confirm it, and every payment is timestamped — no more chasing.",
                    Content = """
                    Rent invoices are generated on a schedule, with due-soon and overdue reminders sent automatically so you don't have to send an awkward message every month.

                    A tenant marks an invoice paid, optionally attaching a payment slip, and you confirm it was received. If something doesn't match, you mark it not received instead, which opens a dispute automatically rather than leaving it as an unresolved conversation.

                    Every payment, confirmation, and dispute is kept on record, tied to the tenant and the unit.
                    """,
                    IsPublished = true,
                    PublishedAt = now.AddDays(-18),
                    CreatedAt = now.AddDays(-18)
                },
                new()
                {
                    Title = "Utility Bill Splitting",
                    Slug = "utility-bill-splitting",
                    Category = InfoPageCategory.ForLandlords,
                    Excerpt = "Equal split, percentage split, or per-meter — choose how shared utility bills are divided.",
                    Content = """
                    Split a utility bill across your tenants equally, by a custom percentage per unit, or by actual per-meter consumption — whichever fits how the property is set up.

                    A tenant with their own individual meter is automatically excluded from the shared building bill, so nobody pays for someone else's usage by mistake.

                    Like rent, each tenant's share is marked paid and confirmed the same way, with the same dispute path if something doesn't add up.
                    """,
                    IsPublished = true,
                    PublishedAt = now.AddDays(-14),
                    CreatedAt = now.AddDays(-14)
                },
                new()
                {
                    Title = "Maintenance Tracking",
                    Slug = "maintenance-tracking",
                    Category = InfoPageCategory.ForLandlords,
                    Excerpt = "Tenants report issues with a photo and description; you track them through to resolved.",
                    Content = """
                    When something breaks, a tenant reports it with a description and an optional photo — no more messages that get lost in a chat thread.

                    You track each request through Open, In Progress, and Resolved, so both sides can see where things stand without asking.

                    Nothing here requires a separate app or spreadsheet — it's tied directly to the unit and the tenant who reported it.
                    """,
                    IsPublished = true,
                    PublishedAt = now.AddDays(-10),
                    CreatedAt = now.AddDays(-10)
                },
                new()
                {
                    Title = "Search & Booking",
                    Slug = "search-and-booking",
                    Category = InfoPageCategory.ForTenants,
                    Excerpt = "Search by city, narrow down to the area you want, and see the exact location on a map.",
                    Content = """
                    Search by city, then filter down to the specific area, rent range, and unit type you actually want — not a long, unfiltered list.

                    Every listing shows the exact location on a map, real availability, and — where tenants have lived there before — a rating and reviews, so you're not going in blind.

                    Request to book a unit that's available, and the landlord reviews and approves it. Approval automatically creates your lease — no separate paperwork to chase.
                    """,
                    IsPublished = true,
                    PublishedAt = now.AddDays(-15),
                    CreatedAt = now.AddDays(-15)
                },
                new()
                {
                    Title = "Rent Payments",
                    Slug = "rent-payments",
                    Category = InfoPageCategory.ForTenants,
                    Excerpt = "Mark a payment made, get confirmed by your landlord, and keep a timestamped record.",
                    Content = """
                    Once you've moved in, rent works the same simple way every month: you mark an invoice paid, optionally attaching a payment slip, and your landlord confirms it.

                    If something doesn't match, either side can raise it as a dispute instead of it just sitting unresolved — and the full history stays on record.

                    Utility bills, when there are any, are split the same way — equally, by percentage, or by actual per-meter usage.
                    """,
                    IsPublished = true,
                    PublishedAt = now.AddDays(-6),
                    CreatedAt = now.AddDays(-6)
                },
                new()
                {
                    Title = "Reviews & Ratings",
                    Slug = "reviews-and-ratings",
                    Category = InfoPageCategory.ForTenants,
                    Excerpt = "See what previous tenants actually experienced before you commit to a place.",
                    Content = """
                    Every listing shows a rating and reviews from tenants who've actually lived there — real feedback, not marketing copy.

                    Once your own lease has run its course, you can leave a review for the property, helping the next person considering it make a more informed decision.

                    Reviews stay attached to the property, not the landlord's account, so they build an honest picture over time.
                    """,
                    IsPublished = true,
                    PublishedAt = now.AddDays(-1),
                    CreatedAt = now.AddDays(-1)
                }
            };

            context.InfoPages.AddRange(posts);
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
