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

            if (!await context.BlogPosts.AnyAsync())
            {
                await SeedBlogPostsAsync(context);
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

        private static async Task SeedBlogPostsAsync(ApplicationDbContext context)
        {
            var now = DateTime.UtcNow;

            var posts = new List<BlogPost>
            {
                new()
                {
                    Title = "5 Ways to Find Reliable Tenants as a First-Time Landlord",
                    Slug = "finding-reliable-tenants-first-time-landlord",
                    Excerpt = "Screening well before you sign a lease saves you months of stress later. Here's what actually works.",
                    Content = """
                    Most landlord horror stories don't start with a bad tenant — they start with skipping the screening step because a vacant unit feels like it's losing money every day it sits empty. It's tempting to take the first applicant who seems reasonable. Resist it. A few extra days of vacancy is cheaper than months of chasing rent or repairing damage.

                    Start by asking for proof of income, not just a number they tell you. A recent salary slip or bank statement showing regular deposits tells you far more than a verbal promise. As a rule of thumb, monthly rent shouldn't be more than a third of the tenant's take-home income — if it is, they're more likely to fall behind eventually.

                    Call their current or previous landlord if you can. People are usually honest about someone else's tenant when they're not trying to get rid of them. Ask directly: did they pay on time, did they take care of the place, would you rent to them again.

                    Meet in person before handing over keys, even if everything else has been over the phone. It's a simple step that filters out a surprising number of problem situations early, and it lets you set expectations face to face about rent due dates, house rules, and how maintenance requests get reported.

                    Finally, put everything in writing. A verbal agreement about move-in date, deposit amount, or what's included isn't an agreement at all once there's a disagreement. RehLe keeps every lease, invoice, and message tied to the unit, so neither side has to rely on memory.
                    """,
                    IsPublished = true,
                    PublishedAt = now.AddDays(-21),
                    CreatedAt = now.AddDays(-21)
                },
                new()
                {
                    Title = "Understanding Your Rent Agreement: What to Check Before Signing",
                    Slug = "understanding-rent-agreement-before-signing",
                    Excerpt = "A rent agreement protects both sides — but only if you actually read it first. Here's what to look for.",
                    Content = """
                    It's easy to skim a rent agreement, sign it, and move in the same day. Most of the time that's fine. The problem shows up later, when something goes wrong and neither side remembers what was actually agreed.

                    Check the rent amount and due date first, and make sure they match what you were verbally told. Then look for how the deposit is described — how much it is, and more importantly, the conditions under which it gets returned. Vague language like "subject to condition of the property" is worth clarifying before you sign, not after you move out.

                    Look for who's responsible for what: routine maintenance, utility bills, and any shared spaces. If the unit has its own electricity or water meter, that should be reflected in how bills are split, so you're not paying a share of a neighbor's usage.

                    Notice periods matter more than people think. Know how much notice you need to give before moving out, and how much notice a landlord owes you before ending the lease. These numbers are often different, and it's worth knowing both before you need them.

                    None of this is about distrust — it's about making sure two people's memory of a conversation months ago doesn't become the basis for a dispute. Read it, ask questions about anything unclear, and keep a copy.
                    """,
                    IsPublished = true,
                    PublishedAt = now.AddDays(-15),
                    CreatedAt = now.AddDays(-15)
                },
                new()
                {
                    Title = "Splitting Shared Utility Bills Fairly in a Shared Home",
                    Slug = "splitting-shared-utility-bills-fairly",
                    Excerpt = "Equal split, percentage split, or per-meter — which one is actually fair for your household?",
                    Content = """
                    Utility bills are the single most common source of friction in a shared house, and it's usually not about the money — it's about the split feeling unfair. Getting the method right upfront avoids most of the arguments later.

                    An equal split is the simplest: divide the bill evenly across everyone sharing it. It works well when usage is genuinely similar, like a small shared flat where everyone's around the same amount during the day.

                    A percentage split makes sense when rooms differ in size or occupancy — a family unit with four people shouldn't necessarily pay the same electricity share as a single occupant in a private room next door. Agreeing on the percentages once, upfront, avoids re-negotiating every month.

                    Per-meter consumption is the fairest option when it's available, since it charges people for what they actually used rather than an estimate. It takes a bit more setup, but it removes the "why am I paying for their air conditioner" argument entirely.

                    One detail that's easy to miss: if a unit has its own individual meter, it shouldn't be included in the shared building bill at all. RehLe checks for this automatically when a bill is split, so a tenant with their own meter isn't double-charged.
                    """,
                    IsPublished = true,
                    PublishedAt = now.AddDays(-10),
                    CreatedAt = now.AddDays(-10)
                },
                new()
                {
                    Title = "Getting Your Security Deposit Back: A Tenant's Checklist",
                    Slug = "getting-security-deposit-back-checklist",
                    Excerpt = "Most deposit disputes come down to documentation. Do these things on move-in day, not move-out day.",
                    Content = """
                    Deposit disputes almost always come down to the same question: was this damage already there, or did the tenant cause it? Whoever has proof wins that argument, and proof only exists if you collected it before it mattered.

                    On move-in day, before you unpack a single box, walk through the unit and take photos or a short video of everything — walls, fixtures, appliances, flooring. Date-stamped photos are worth more than a memory nine months later. Send a copy to your landlord at the time, even a simple message, so there's a shared record from day one.

                    Report anything that's already broken immediately, in writing, rather than mentioning it verbally and assuming it's noted. A landlord can't be expected to remember a passing comment months later, and neither can you.

                    Before you move out, do the same walkthrough again. Fix anything genuinely caused by normal wear versus what you're responsible for — a scuffed wall from years of living there is different from a hole punched in it.

                    Finally, agree on how the deposit will be returned and by when, and keep that conversation in writing. A platform like RehLe keeps the lease, photos, and messages tied to one unit and one tenant, so there's a clear record for both sides instead of a disagreement about who remembers what.
                    """,
                    IsPublished = true,
                    PublishedAt = now.AddDays(-6),
                    CreatedAt = now.AddDays(-6)
                },
                new()
                {
                    Title = "Why Digital Rent Collection Beats Cash and Bank Transfers",
                    Slug = "digital-rent-collection-beats-cash",
                    Excerpt = "Cash gets lost, bank transfers get \"forgotten.\" Here's why a paper trail matters for both sides.",
                    Content = """
                    Cash is still how a lot of rent gets paid, and it works fine right up until there's a disagreement about whether it was paid at all. No receipt, no record, and it comes down to one person's word against another's.

                    Bank transfers are better, but they're easy to lose track of too. A landlord managing several tenants across a few properties can't be expected to reconcile every incoming transfer by hand every month, and a missed one often gets noticed weeks late — awkward for everyone.

                    A digital system fixes the actual problem, which isn't the payment method itself but the lack of a shared, timestamped record both people can see. When a tenant marks an invoice paid, the landlord gets notified immediately and can confirm it — or flag that it wasn't received, which opens a dispute instead of an argument. Either way, there's a record, not a memory.

                    It also removes the awkwardness of chasing rent in person. Automatic due-soon and overdue reminders mean a landlord doesn't have to be the one sending an uncomfortable message every month, and a tenant gets a heads-up before a payment is actually late rather than after.

                    None of this requires trusting a stranger with your money — it's simply keeping the same conversation landlords and tenants already have, but with a record neither side has to take on faith.
                    """,
                    IsPublished = true,
                    PublishedAt = now.AddDays(-3),
                    CreatedAt = now.AddDays(-3)
                },
                new()
                {
                    Title = "Choosing the Right Area When You're Renting in a New City",
                    Slug = "choosing-the-right-area-renting-new-city",
                    Excerpt = "Rent isn't the only cost of a location. Here's what to actually weigh before you commit.",
                    Content = """
                    The cheapest listing in a city is rarely the cheapest place to actually live, once you factor in everything around it. Before committing to a unit, it's worth stepping back and looking at the area, not just the price.

                    Commute time is the big one people underestimate. A unit that's a little more expensive but ten minutes from work or university often works out ahead once you account for daily transport cost and the time itself, which you don't get back.

                    Look at what's actually nearby, not just what's advertised as nearby. Groceries, a pharmacy, and reliable transport links matter more day-to-day than a landmark a few kilometers away. If you can, visit at a couple of different times of day — an area can feel completely different in the evening than it does at noon.

                    Talk to people already living there if you get the chance, or read what previous tenants have said about the property itself. This is exactly why reviews matter: someone who's actually lived in a unit can tell you things a listing never will, from water pressure to how responsive the landlord actually is.

                    Once you've narrowed it down to a city, searching by area rather than scrolling through every listing in town makes this whole process faster — which is why RehLe lets you filter down to the specific neighborhood you're actually considering.
                    """,
                    IsPublished = true,
                    PublishedAt = now.AddDays(-1),
                    CreatedAt = now.AddDays(-1)
                }
            };

            context.BlogPosts.AddRange(posts);
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
