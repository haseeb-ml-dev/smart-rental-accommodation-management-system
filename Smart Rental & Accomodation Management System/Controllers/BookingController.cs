using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Rental___Accomodation_Management_System.Data;
using Smart_Rental___Accomodation_Management_System.Models;
using Smart_Rental___Accomodation_Management_System.Services;
using Smart_Rental___Accomodation_Management_System.ViewModels;

namespace Smart_Rental___Accomodation_Management_System.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private const int PageSize = 10;

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly LocationOptionsService _locationOptions;

        public BookingController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, LocationOptionsService locationOptions)
        {
            _context = context;
            _userManager = userManager;
            _locationOptions = locationOptions;
        }

        [Authorize(Roles = "Tenant")]
        public async Task<IActionResult> Browse(UnitSearchFilter filter)
        {
            var tenantId = _userManager.GetUserId(User)!;
            var today = DateTime.UtcNow.Date;

            filter ??= new UnitSearchFilter();
            if (filter.Page < 1)
            {
                filter.Page = 1;
            }

            var query = _context.Units
                .Include(u => u.Property)
                .Include(u => u.Leases)
                .Include(u => u.Bookings)
                .Include(u => u.Images)
                .Where(u => u.Property!.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.City))
            {
                query = query.Where(u => u.Property!.City == filter.City);
            }
            if (!string.IsNullOrWhiteSpace(filter.Area))
            {
                query = query.Where(u => u.Property!.Area == filter.Area);
            }
            if (filter.MinRent.HasValue)
            {
                query = query.Where(u => u.MonthlyRent >= filter.MinRent.Value);
            }
            if (filter.MaxRent.HasValue)
            {
                query = query.Where(u => u.MonthlyRent <= filter.MaxRent.Value);
            }
            if (filter.UnitType.HasValue)
            {
                query = query.Where(u => u.UnitType == filter.UnitType.Value);
            }
            if (filter.BhkType.HasValue)
            {
                query = query.Where(u => u.BhkType == filter.BhkType.Value);
            }

            var totalCount = await query.CountAsync();

            var units = await query
                .OrderBy(u => u.Property!.Name).ThenBy(u => u.Name)
                .Skip((filter.Page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var vm = new TenantBrowseViewModel
            {
                Filter = filter,
                Cities = await _locationOptions.GetActiveCitiesAsync(),
                AreasByCity = await _locationOptions.GetActiveAreasByCityAsync(),
                TotalCount = totalCount,
                PageSize = PageSize,
                Units = units.Select(u => new UnitAvailabilityViewModel
                {
                    UnitId = u.Id,
                    CoverImageFileName = (u.Images.FirstOrDefault(i => i.IsCover) ?? u.Images.FirstOrDefault())?.FileName,
                    PropertyName = u.Property?.Name ?? string.Empty,
                    Latitude = u.Property?.Latitude,
                    Longitude = u.Property?.Longitude,
                    UnitName = u.Name,
                    UnitType = u.UnitType,
                    BhkType = u.BhkType,
                    MonthlyRent = u.MonthlyRent,
                    Capacity = u.Capacity,
                    BookableSlots = u.BookableSlots,
                    ActiveLeaseCount = u.Leases.Count(l => l.EndDate == null || l.EndDate >= today),
                    HasPendingRequestFromCurrentTenant = u.Bookings.Any(b => b.TenantId == tenantId && b.Status == BookingStatus.Pending)
                }).ToList()
            };

            return View(vm);
        }

        [Authorize(Roles = "Tenant")]
        public async Task<IActionResult> Details(int unitId)
        {
            var tenantId = _userManager.GetUserId(User)!;
            var today = DateTime.UtcNow.Date;

            var unit = await _context.Units
                .Include(u => u.Property)
                .Include(u => u.Leases)
                .Include(u => u.Bookings)
                .Include(u => u.Images)
                .FirstOrDefaultAsync(u => u.Id == unitId && u.Property!.IsActive);

            if (unit == null)
            {
                return NotFound();
            }

            var vm = new UnitDetailViewModel
            {
                UnitId = unit.Id,
                PropertyName = unit.Property?.Name ?? string.Empty,
                Address = unit.Property?.Address ?? string.Empty,
                Latitude = unit.Property?.Latitude,
                Longitude = unit.Property?.Longitude,
                UnitName = unit.Name,
                UnitType = unit.UnitType,
                BhkType = unit.BhkType,
                MonthlyRent = unit.MonthlyRent,
                Capacity = unit.Capacity,
                BookableSlots = unit.BookableSlots,
                ActiveLeaseCount = unit.Leases.Count(l => l.EndDate == null || l.EndDate >= today),
                HasPendingRequestFromCurrentTenant = unit.Bookings.Any(b => b.TenantId == tenantId && b.Status == BookingStatus.Pending),
                Images = unit.Images.OrderByDescending(i => i.IsCover).ThenBy(i => i.Id).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [Authorize(Roles = "Tenant")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestBooking(int unitId, DateTime startDate, DateTime? endDate, decimal? proposedRent)
        {
            var tenantId = _userManager.GetUserId(User)!;

            var unit = await _context.Units
                .Include(u => u.Property)
                .Include(u => u.Leases)
                .Include(u => u.Bookings)
                .FirstOrDefaultAsync(u => u.Id == unitId);

            if (unit == null)
            {
                return NotFound();
            }

            var today = DateTime.UtcNow.Date;
            var activeLeaseCount = unit.Leases.Count(l => l.EndDate == null || l.EndDate >= today);
            var alreadyPending = unit.Bookings.Any(b => b.TenantId == tenantId && b.Status == BookingStatus.Pending);
            var resolvedStartDate = startDate == default ? today : startDate.Date;

            if (endDate.HasValue && endDate.Value.Date <= resolvedStartDate)
            {
                TempData["Message"] = "The move-out date must be after the move-in date.";
                return RedirectToAction(nameof(Details), new { unitId });
            }

            if (unit.Property!.IsActive && activeLeaseCount < unit.BookableSlots && !alreadyPending)
            {
                _context.Bookings.Add(new Booking
                {
                    UnitId = unitId,
                    TenantId = tenantId,
                    StartDate = resolvedStartDate,
                    RequestedEndDate = endDate?.Date,
                    ProposedRent = proposedRent is > 0 ? proposedRent : null,
                    Status = BookingStatus.Pending
                });
                await _context.SaveChangesAsync();
                TempData["Message"] = "Booking request submitted.";
            }
            else
            {
                TempData["Message"] = alreadyPending
                    ? "You already have a pending request for this unit."
                    : "This unit is no longer available.";
            }

            return RedirectToAction(nameof(Browse));
        }

        [Authorize(Roles = "Tenant")]
        public async Task<IActionResult> MyBookings()
        {
            var tenantId = _userManager.GetUserId(User)!;

            var bookings = await _context.Bookings
                .Include(b => b.Unit)
                    .ThenInclude(u => u!.Property)
                .Where(b => b.TenantId == tenantId)
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new BookingListItemViewModel
                {
                    BookingId = b.Id,
                    PropertyName = b.Unit!.Property!.Name,
                    UnitName = b.Unit.Name,
                    StartDate = b.StartDate,
                    RequestedEndDate = b.RequestedEndDate,
                    MonthlyRent = b.Unit.MonthlyRent,
                    ProposedRent = b.ProposedRent,
                    Status = b.Status,
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();

            return View(bookings);
        }

        [Authorize(Roles = "Landlord")]
        public async Task<IActionResult> Requests()
        {
            var landlordId = _userManager.GetUserId(User)!;

            var bookings = await _context.Bookings
                .Include(b => b.Unit)
                    .ThenInclude(u => u!.Property)
                .Include(b => b.Tenant)
                .Where(b => b.Status == BookingStatus.Pending && b.Unit!.Property!.LandlordId == landlordId)
                .OrderBy(b => b.CreatedAt)
                .Select(b => new BookingListItemViewModel
                {
                    BookingId = b.Id,
                    PropertyName = b.Unit!.Property!.Name,
                    UnitName = b.Unit.Name,
                    TenantName = b.Tenant!.FullName,
                    StartDate = b.StartDate,
                    RequestedEndDate = b.RequestedEndDate,
                    MonthlyRent = b.Unit.MonthlyRent,
                    ProposedRent = b.ProposedRent,
                    Status = b.Status,
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();

            return View(bookings);
        }

        [HttpPost]
        [Authorize(Roles = "Tenant")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int bookingId)
        {
            var tenantId = _userManager.GetUserId(User)!;

            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.TenantId == tenantId);

            if (booking == null)
            {
                return NotFound();
            }

            if (booking.Status == BookingStatus.Pending)
            {
                booking.Status = BookingStatus.Cancelled;
                booking.DecisionDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["Message"] = "Booking request cancelled.";
            }

            return RedirectToAction(nameof(MyBookings));
        }

        [HttpPost]
        [Authorize(Roles = "Landlord")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int bookingId)
        {
            var landlordId = _userManager.GetUserId(User)!;

            var booking = await _context.Bookings
                .Include(b => b.Unit)
                    .ThenInclude(u => u!.Property)
                .Include(b => b.Unit!.Leases)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.Unit!.Property!.LandlordId == landlordId);

            if (booking == null)
            {
                return NotFound();
            }

            if (booking.Status != BookingStatus.Pending)
            {
                TempData["Message"] = "This request has already been decided.";
                return RedirectToAction(nameof(Requests));
            }

            var today = DateTime.UtcNow.Date;
            var activeLeaseCount = booking.Unit!.Leases.Count(l => l.EndDate == null || l.EndDate >= today);
            if (activeLeaseCount >= booking.Unit.BookableSlots)
            {
                TempData["Message"] = "This unit filled up since the request was made. Reject it or free up capacity first.";
                return RedirectToAction(nameof(Requests));
            }

            _context.Leases.Add(new Lease
            {
                UnitId = booking.UnitId,
                TenantId = booking.TenantId,
                StartDate = booking.StartDate,
                EndDate = booking.RequestedEndDate
            });

            booking.Status = BookingStatus.Approved;
            booking.DecisionDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["Message"] = "Booking approved and lease created.";

            return RedirectToAction(nameof(Requests));
        }

        [HttpPost]
        [Authorize(Roles = "Landlord")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int bookingId)
        {
            var landlordId = _userManager.GetUserId(User)!;

            var booking = await _context.Bookings
                .Include(b => b.Unit)
                    .ThenInclude(u => u!.Property)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.Unit!.Property!.LandlordId == landlordId);

            if (booking == null)
            {
                return NotFound();
            }

            if (booking.Status == BookingStatus.Pending)
            {
                booking.Status = BookingStatus.Rejected;
                booking.DecisionDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            TempData["Message"] = "Booking rejected.";
            return RedirectToAction(nameof(Requests));
        }

        [Authorize(Roles = "Landlord")]
        public async Task<IActionResult> Calendar(int propertyId, int monthOffset = 0)
        {
            var property = await GetOwnedPropertyAsync(propertyId);
            if (property == null)
            {
                return NotFound();
            }

            var units = await _context.Units
                .Include(u => u.Leases)
                    .ThenInclude(l => l.Tenant)
                .Include(u => u.Bookings.Where(b => b.Status == BookingStatus.Pending))
                    .ThenInclude(b => b.Tenant)
                .Where(u => u.PropertyId == propertyId)
                .OrderBy(u => u.Name)
                .ToListAsync();

            var today = DateTime.UtcNow.Date;
            var rangeStart = new DateTime(today.Year, today.Month, 1).AddMonths(monthOffset);
            var rangeEnd = rangeStart.AddMonths(3);
            var totalDays = (decimal)(rangeEnd - rangeStart).TotalDays;

            decimal PercentOf(DateTime date) => (decimal)(date - rangeStart).TotalDays / totalDays * 100m;

            var vm = new BookingCalendarViewModel
            {
                PropertyId = property.Id,
                PropertyName = property.Name,
                RangeStart = rangeStart,
                RangeEnd = rangeEnd,
                MonthOffset = monthOffset
            };

            for (var month = rangeStart; month < rangeEnd; month = month.AddMonths(1))
            {
                vm.MonthMarkers.Add(new MonthMarkerViewModel { Label = month.ToString("MMM yyyy"), LeftPercent = PercentOf(month) });
            }

            foreach (var unit in units)
            {
                var unitVm = new UnitTimelineViewModel { UnitId = unit.Id, UnitName = unit.Name, UnitType = unit.UnitType };

                foreach (var lease in unit.Leases)
                {
                    if (lease.EndDate < rangeStart || lease.StartDate > rangeEnd)
                    {
                        continue;
                    }

                    var clampedStart = lease.StartDate < rangeStart ? rangeStart : lease.StartDate;
                    var clampedEnd = (lease.EndDate ?? rangeEnd) > rangeEnd ? rangeEnd : (lease.EndDate ?? rangeEnd);

                    var left = PercentOf(clampedStart);
                    var width = Math.Max(PercentOf(clampedEnd) - left, 2m);

                    unitVm.Bars.Add(new TimelineBarViewModel
                    {
                        TenantName = lease.Tenant?.FullName ?? "Unknown",
                        StartDate = lease.StartDate,
                        EndDate = lease.EndDate,
                        IsActive = lease.EndDate == null || lease.EndDate >= today,
                        LeftPercent = left,
                        WidthPercent = width
                    });
                }

                foreach (var booking in unit.Bookings)
                {
                    if (booking.StartDate < rangeStart || booking.StartDate > rangeEnd)
                    {
                        continue;
                    }

                    unitVm.PendingMarkers.Add(new TimelineMarkerViewModel
                    {
                        TenantName = booking.Tenant?.FullName ?? "Unknown",
                        RequestedDate = booking.StartDate,
                        LeftPercent = PercentOf(booking.StartDate)
                    });
                }

                vm.Units.Add(unitVm);
            }

            return View(vm);
        }

        private async Task<Property?> GetOwnedPropertyAsync(int propertyId)
        {
            var landlordId = _userManager.GetUserId(User)!;
            return await _context.Properties.FirstOrDefaultAsync(p => p.Id == propertyId && p.LandlordId == landlordId);
        }
    }
}
