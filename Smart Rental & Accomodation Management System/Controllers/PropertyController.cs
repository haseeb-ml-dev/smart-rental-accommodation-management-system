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
    [Authorize(Roles = "Landlord")]
    public class PropertyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly UnitImageStorage _imageStorage;
        private readonly GeocodingService _geocodingService;
        private readonly LocationOptionsService _locationOptions;

        public PropertyController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, UnitImageStorage imageStorage, GeocodingService geocodingService, LocationOptionsService locationOptions)
        {
            _context = context;
            _userManager = userManager;
            _imageStorage = imageStorage;
            _geocodingService = geocodingService;
            _locationOptions = locationOptions;
        }

        private async Task PopulateLocationViewBagAsync()
        {
            ViewBag.Cities = await _locationOptions.GetSupportedCityNamesAsync();
            ViewBag.AreasByCity = await _locationOptions.GetSupportedAreasByCityAsync();
        }

        // Free trial or an active subscription both grant access; only adding a NEW property is
        // gated on this — existing properties, rent collection, and tenant management stay
        // available regardless, since that's all that was asked to be gated.
        private async Task<bool> IsEntitledToAddPropertyAsync()
        {
            var landlord = await _userManager.GetUserAsync(User);
            if (landlord == null)
            {
                return false;
            }

            var now = DateTime.UtcNow;
            return (landlord.TrialEndsAt.HasValue && landlord.TrialEndsAt.Value > now)
                || (landlord.SubscriptionActiveUntil.HasValue && landlord.SubscriptionActiveUntil.Value > now);
        }

        public async Task<IActionResult> Index()
        {
            var landlordId = _userManager.GetUserId(User)!;

            var properties = await _context.Properties
                .Include(p => p.Units)
                    .ThenInclude(u => u.Images)
                .Where(p => p.LandlordId == landlordId)
                .ToListAsync();

            return View(properties);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!await IsEntitledToAddPropertyAsync())
            {
                TempData["Message"] = "Your free trial has ended. Subscribe to add more properties.";
                return RedirectToAction("Index", "Subscription");
            }

            await PopulateLocationViewBagAsync();
            return View(new PropertyFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PropertyFormViewModel model)
        {
            if (!await IsEntitledToAddPropertyAsync())
            {
                TempData["Message"] = "Your free trial has ended. Subscribe to add more properties.";
                return RedirectToAction("Index", "Subscription");
            }

            if (!ModelState.IsValid)
            {
                await PopulateLocationViewBagAsync();
                return View(model);
            }

            var property = new Property
            {
                LandlordId = _userManager.GetUserId(User)!,
                Name = model.Name,
                Address = model.Address,
                City = string.IsNullOrWhiteSpace(model.City) ? null : model.City.Trim(),
                Area = string.IsNullOrWhiteSpace(model.Area) ? null : model.Area.Trim()
            };

            if (model.LocationConfirmed && model.Latitude.HasValue && model.Longitude.HasValue)
            {
                property.Latitude = model.Latitude;
                property.Longitude = model.Longitude;
            }
            else
            {
                var geocode = await _geocodingService.GeocodeAsync(model.Address);
                property.Latitude = geocode?.Latitude;
                property.Longitude = geocode?.Longitude;
            }

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

            return RedirectToAction(nameof(EditUnit), new { id = unit.Id });
        }

        [HttpGet]
        public async Task<IActionResult> EditProperty(int id)
        {
            var property = await GetOwnedPropertyAsync(id);
            if (property == null)
            {
                return NotFound();
            }

            await PopulateLocationViewBagAsync();
            return View(new PropertyFormViewModel
            {
                Id = property.Id,
                Name = property.Name,
                Address = property.Address,
                City = property.City,
                Area = property.Area,
                Latitude = property.Latitude,
                Longitude = property.Longitude
            });
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
                await PopulateLocationViewBagAsync();
                model.Latitude = property.Latitude;
                model.Longitude = property.Longitude;
                return View(model);
            }

            var addressChanged = !string.Equals(property.Address, model.Address, StringComparison.Ordinal);

            property.Name = model.Name;
            property.Address = model.Address;
            property.City = string.IsNullOrWhiteSpace(model.City) ? null : model.City.Trim();
            property.Area = string.IsNullOrWhiteSpace(model.Area) ? null : model.Area.Trim();

            if (model.LocationConfirmed && model.Latitude.HasValue && model.Longitude.HasValue)
            {
                // Coordinate came from an autocomplete pick or a manual pin drag this submission — trust it.
                property.Latitude = model.Latitude;
                property.Longitude = model.Longitude;
            }
            else if (addressChanged)
            {
                // Address was hand-edited without re-picking a suggestion; the old coordinates no longer apply.
                var geocode = await _geocodingService.GeocodeAsync(model.Address);
                property.Latitude = geocode?.Latitude;
                property.Longitude = geocode?.Longitude;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int propertyId)
        {
            var property = await GetOwnedPropertyAsync(propertyId);
            if (property == null)
            {
                return NotFound();
            }

            property.IsActive = !property.IsActive;
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

            ViewBag.UnitImages = unit.Images.OrderByDescending(i => i.IsCover).ThenBy(i => i.Id).ToList();
            ViewBag.MaxImagesPerUnit = UnitImageStorage.MaxImagesPerUnit;

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

            var today = DateTime.UtcNow.Date;
            var activeLeaseCount = unit.Leases.Count(l => l.EndDate == null || l.EndDate >= today);
            var newBookableSlots = model.UnitType == UnitType.SharedRoom ? model.Capacity : 1;
            if (newBookableSlots < activeLeaseCount)
            {
                ModelState.AddModelError(string.Empty, $"This unit has {activeLeaseCount} active tenant(s); end enough leases before reducing capacity/type below that.");
            }

            if (!ModelState.IsValid)
            {
                model.PropertyName = unit.Property!.Name;
                ViewBag.UnitImages = unit.Images.OrderByDescending(i => i.IsCover).ThenBy(i => i.Id).ToList();
                ViewBag.MaxImagesPerUnit = UnitImageStorage.MaxImagesPerUnit;
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

            var today = DateTime.UtcNow.Date;
            var activeLeases = unit.Leases.Where(l => l.EndDate == null || l.EndDate >= today).OrderBy(l => l.StartDate).ToList();
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
                    StartDate = l.StartDate,
                    EndDate = l.EndDate
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

            var today = DateTime.UtcNow.Date;
            var activeLeaseCount = unit.Leases.Count(l => l.EndDate == null || l.EndDate >= today);
            var alreadyActive = unit.Leases.Any(l => (l.EndDate == null || l.EndDate >= today) && l.TenantId == tenantId);

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

            var today = DateTime.UtcNow.Date;

            if (lease.EndDate == null || lease.EndDate > today)
            {
                lease.EndDate = today;
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
                .Include(u => u.Images)
                .FirstOrDefaultAsync(u => u.Id == unitId && u.Property!.LandlordId == landlordId);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(UnitImageStorage.MaxFileSizeBytes * UnitImageStorage.MaxImagesPerUnit)]
        public async Task<IActionResult> UploadImages(int unitId, List<IFormFile> files)
        {
            var unit = await GetOwnedUnitAsync(unitId);
            if (unit == null)
            {
                return NotFound();
            }

            var remainingSlots = Math.Max(0, UnitImageStorage.MaxImagesPerUnit - unit.Images.Count);
            var hasCover = unit.Images.Any(i => i.IsCover);
            var skippedAny = files.Count > remainingSlots;

            foreach (var file in files.Take(remainingSlots))
            {
                if (!_imageStorage.IsAllowed(file, out var extension))
                {
                    skippedAny = true;
                    continue;
                }

                var fileName = await _imageStorage.SaveAsync(unitId, file, extension);
                _context.UnitImages.Add(new UnitImage
                {
                    UnitId = unitId,
                    FileName = fileName,
                    IsCover = !hasCover
                });
                hasCover = true;
            }

            await _context.SaveChangesAsync();

            TempData["Message"] = skippedAny
                ? $"Some files were skipped — up to {UnitImageStorage.MaxImagesPerUnit} photos per unit, JPG/PNG/WEBP up to 5 MB each."
                : "Photos uploaded.";

            return RedirectToAction(nameof(EditUnit), new { id = unitId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int imageId)
        {
            var landlordId = _userManager.GetUserId(User)!;

            var image = await _context.UnitImages
                .Include(i => i.Unit)
                    .ThenInclude(u => u!.Property)
                .FirstOrDefaultAsync(i => i.Id == imageId && i.Unit!.Property!.LandlordId == landlordId);

            if (image == null)
            {
                return NotFound();
            }

            var unitId = image.UnitId;
            var wasCover = image.IsCover;

            _imageStorage.Delete(unitId, image.FileName);
            _context.UnitImages.Remove(image);
            await _context.SaveChangesAsync();

            if (wasCover)
            {
                var nextCover = await _context.UnitImages
                    .Where(i => i.UnitId == unitId)
                    .OrderBy(i => i.Id)
                    .FirstOrDefaultAsync();

                if (nextCover != null)
                {
                    nextCover.IsCover = true;
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction(nameof(EditUnit), new { id = unitId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetCoverImage(int imageId)
        {
            var landlordId = _userManager.GetUserId(User)!;

            var image = await _context.UnitImages
                .Include(i => i.Unit)
                    .ThenInclude(u => u!.Property)
                .FirstOrDefaultAsync(i => i.Id == imageId && i.Unit!.Property!.LandlordId == landlordId);

            if (image == null)
            {
                return NotFound();
            }

            var currentCover = await _context.UnitImages
                .FirstOrDefaultAsync(i => i.UnitId == image.UnitId && i.IsCover);

            if (currentCover != null)
            {
                currentCover.IsCover = false;
            }
            image.IsCover = true;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(EditUnit), new { id = image.UnitId });
        }
    }
}
