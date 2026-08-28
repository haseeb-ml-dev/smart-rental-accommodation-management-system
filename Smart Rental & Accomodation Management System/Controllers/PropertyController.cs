using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Rental___Accomodation_Management_System.Data;
using Smart_Rental___Accomodation_Management_System.Models;
using Smart_Rental___Accomodation_Management_System.ViewModels;

namespace Smart_Rental___Accomodation_Management_System.Controllers
{
    [Authorize(Roles = "Landlord")]
    public class PropertyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PropertyController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var landlordId = _userManager.GetUserId(User)!;

            var properties = await _context.Properties
                .Include(p => p.Units)
                .Where(p => p.LandlordId == landlordId)
                .ToListAsync();

            return View(properties);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new PropertyFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PropertyFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var property = new Property
            {
                LandlordId = _userManager.GetUserId(User)!,
                Name = model.Name,
                Address = model.Address
            };

            _context.Properties.Add(property);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> AddUnit(int propertyId)
        {
            var property = await GetOwnedPropertyAsync(propertyId);
            if (property == null)
            {
                return NotFound();
            }

            return View(new UnitFormViewModel { PropertyId = property.Id, PropertyName = property.Name });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUnit(UnitFormViewModel model)
        {
            var property = await GetOwnedPropertyAsync(model.PropertyId);
            if (property == null)
            {
                return NotFound();
            }

            if (model.UnitType == UnitType.FamilyUnit && model.BhkType == null)
            {
                ModelState.AddModelError(nameof(model.BhkType), "Select a BHK configuration for a Family Unit / apartment listing.");
            }

            if (!ModelState.IsValid)
            {
                model.PropertyName = property.Name;
                return View(model);
            }

            var unit = new Unit
            {
                PropertyId = property.Id,
                Name = model.Name,
                UnitType = model.UnitType,
                BhkType = model.UnitType == UnitType.FamilyUnit ? model.BhkType : null,
                MonthlyRent = model.MonthlyRent,
                Capacity = model.Capacity,
                HasIndividualElectricityMeter = model.HasIndividualElectricityMeter,
                HasIndividualWaterMeter = model.HasIndividualWaterMeter
            };

            _context.Units.Add(unit);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> EditProperty(int id)
        {
            var property = await GetOwnedPropertyAsync(id);
            if (property == null)
            {
                return NotFound();
            }

            return View(new PropertyFormViewModel { Id = property.Id, Name = property.Name, Address = property.Address });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProperty(PropertyFormViewModel model)
        {
            var property = await GetOwnedPropertyAsync(model.Id);
            if (property == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            property.Name = model.Name;
            property.Address = model.Address;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> EditUnit(int id)
        {
            var unit = await GetOwnedUnitAsync(id);
            if (unit == null)
            {
                return NotFound();
            }

            return View(new UnitFormViewModel
            {
                Id = unit.Id,
                PropertyId = unit.PropertyId,
                PropertyName = unit.Property!.Name,
                Name = unit.Name,
                UnitType = unit.UnitType,
                BhkType = unit.BhkType,
                MonthlyRent = unit.MonthlyRent,
                Capacity = unit.Capacity,
                HasIndividualElectricityMeter = unit.HasIndividualElectricityMeter,
                HasIndividualWaterMeter = unit.HasIndividualWaterMeter
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUnit(UnitFormViewModel model)
        {
            var unit = await GetOwnedUnitAsync(model.Id);
            if (unit == null)
            {
                return NotFound();
            }

            if (model.UnitType == UnitType.FamilyUnit && model.BhkType == null)
            {
                ModelState.AddModelError(nameof(model.BhkType), "Select a BHK configuration for a Family Unit / apartment listing.");
            }

            var activeLeaseCount = unit.Leases.Count(l => l.EndDate == null);
            var newBookableSlots = model.UnitType == UnitType.SharedRoom ? model.Capacity : 1;
            if (newBookableSlots < activeLeaseCount)
            {
                ModelState.AddModelError(string.Empty, $"This unit has {activeLeaseCount} active tenant(s); end enough leases before reducing capacity/type below that.");
            }

            if (!ModelState.IsValid)
            {
                model.PropertyName = unit.Property!.Name;
                return View(model);
            }

            unit.Name = model.Name;
            unit.UnitType = model.UnitType;
            unit.BhkType = model.UnitType == UnitType.FamilyUnit ? model.BhkType : null;
            unit.MonthlyRent = model.MonthlyRent;
            unit.Capacity = model.Capacity;
            unit.HasIndividualElectricityMeter = model.HasIndividualElectricityMeter;
            unit.HasIndividualWaterMeter = model.HasIndividualWaterMeter;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ManageTenants(int unitId)
        {
            var unit = await GetOwnedUnitAsync(unitId);
            if (unit == null)
            {
                return NotFound();
            }

            var activeLeases = unit.Leases.Where(l => l.EndDate == null).OrderBy(l => l.StartDate).ToList();
            var activeTenantIds = activeLeases.Select(l => l.TenantId).ToHashSet();

            var tenantUsers = await _userManager.GetUsersInRoleAsync("Tenant");

            var vm = new UnitTenantsViewModel
            {
                UnitId = unit.Id,
                UnitName = unit.Name,
                PropertyId = unit.PropertyId,
                PropertyName = unit.Property!.Name,
                BookableSlots = unit.BookableSlots,
                ActiveLeaseCount = activeLeases.Count,
                ActiveLeases = activeLeases.Select(l => new ActiveLeaseRowViewModel
                {
                    LeaseId = l.Id,
                    TenantName = l.Tenant!.FullName,
                    StartDate = l.StartDate
                }).ToList(),
                AvailableTenants = tenantUsers
                    .Where(t => !activeTenantIds.Contains(t.Id))
                    .OrderBy(t => t.FullName)
                    .Select(t => new TenantOptionViewModel { Id = t.Id, Label = $"{t.FullName} ({t.Email})" })
                    .ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignTenant(int unitId, string tenantId, DateTime startDate)
        {
            var unit = await GetOwnedUnitAsync(unitId);
            if (unit == null)
            {
                return NotFound();
            }

            var activeLeaseCount = unit.Leases.Count(l => l.EndDate == null);
            var alreadyActive = unit.Leases.Any(l => l.EndDate == null && l.TenantId == tenantId);

            if (activeLeaseCount < unit.BookableSlots && !alreadyActive)
            {
                _context.Leases.Add(new Lease
                {
                    UnitId = unit.Id,
                    TenantId = tenantId,
                    StartDate = startDate == default ? DateTime.UtcNow.Date : startDate.Date
                });
                await _context.SaveChangesAsync();
                TempData["Message"] = "Tenant assigned.";
            }
            else
            {
                TempData["Message"] = alreadyActive ? "That tenant already has an active lease on this unit." : "This unit is full.";
            }

            return RedirectToAction(nameof(ManageTenants), new { unitId = unit.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EndLease(int leaseId)
        {
            var landlordId = _userManager.GetUserId(User)!;

            var lease = await _context.Leases
                .Include(l => l.Unit)
                    .ThenInclude(u => u!.Property)
                .FirstOrDefaultAsync(l => l.Id == leaseId && l.Unit!.Property!.LandlordId == landlordId);

            if (lease == null)
            {
                return NotFound();
            }

            if (lease.EndDate == null)
            {
                lease.EndDate = DateTime.UtcNow.Date;
                await _context.SaveChangesAsync();
                TempData["Message"] = "Tenant removed from unit.";
            }

            return RedirectToAction(nameof(ManageTenants), new { unitId = lease.UnitId });
        }

        private async Task<Property?> GetOwnedPropertyAsync(int propertyId)
        {
            var landlordId = _userManager.GetUserId(User)!;
            return await _context.Properties
                .FirstOrDefaultAsync(p => p.Id == propertyId && p.LandlordId == landlordId);
        }

        private async Task<Unit?> GetOwnedUnitAsync(int unitId)
        {
            var landlordId = _userManager.GetUserId(User)!;
            return await _context.Units
                .Include(u => u.Property)
                .Include(u => u.Leases)
                    .ThenInclude(l => l.Tenant)
                .FirstOrDefaultAsync(u => u.Id == unitId && u.Property!.LandlordId == landlordId);
        }
    }
}
