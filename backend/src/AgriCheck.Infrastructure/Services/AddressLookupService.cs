using AgriCheck.Application.Common;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Services;

public class AddressLookupService : IAddressLookupService
{
    private readonly AgriCheckDbContext _db;

    public AddressLookupService(AgriCheckDbContext db) => _db = db;

    public async Task<IReadOnlyList<AddressOptionDto>> ListRegionsAsync(CancellationToken cancellationToken = default) =>
        await _db.AddressRegions.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new AddressOptionDto(x.Id, x.Code, x.Name))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AddressOptionDto>> ListProvincesAsync(long regionId, CancellationToken cancellationToken = default) =>
        await _db.AddressProvinces.AsNoTracking()
            .Where(x => x.IsActive && x.RegionId == regionId)
            .OrderBy(x => x.Name)
            .Select(x => new AddressOptionDto(x.Id, x.Code, x.Name))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AddressOptionDto>> ListCitiesAsync(long provinceId, CancellationToken cancellationToken = default) =>
        await _db.AddressCities.AsNoTracking()
            .Where(x => x.IsActive && x.ProvinceId == provinceId)
            .OrderBy(x => x.Name)
            .Select(x => new AddressOptionDto(x.Id, x.Code, x.Name))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AddressBarangayOptionDto>> ListBarangaysAsync(long cityId, CancellationToken cancellationToken = default) =>
        await _db.AddressBarangays.AsNoTracking()
            .Where(x => x.IsActive && x.CityId == cityId)
            .OrderBy(x => x.Name)
            .Select(x => new AddressBarangayOptionDto(x.Id, x.Code, x.Name, x.ZipCode))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RegisteredWarehouseDto>> ListRegisteredWarehousesAsync(CancellationToken cancellationToken = default)
    {
        var facilities = await _db.WarehouseFacilities.AsNoTracking()
            .Where(x => x.IsActive)
            .Include(x => x.Region)
            .Include(x => x.Province)
            .Include(x => x.City)
            .Include(x => x.Barangay)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return facilities.Select(MapRegisteredWarehouse).ToList();
    }

    private static RegisteredWarehouseDto MapRegisteredWarehouse(Domain.Entities.WarehouseFacility facility)
    {
        var formattedAddress = WarehouseAddressFormatter.Format(
            facility.StreetAddress,
            facility.Barangay?.Name,
            facility.City?.Name,
            facility.Province?.Name,
            facility.Region?.Name,
            facility.ZipCode ?? facility.Barangay?.ZipCode,
            facility.Location);

        return new RegisteredWarehouseDto(
            facility.Id,
            facility.Code,
            facility.Name,
            formattedAddress,
            facility.Region?.Name,
            facility.Province?.Name,
            facility.City?.Name,
            facility.Barangay?.Name,
            facility.StreetAddress,
            facility.ZipCode ?? facility.Barangay?.ZipCode,
            facility.Latitude,
            facility.Longitude);
    }
}
