namespace AgriCheck.Application.Common;

public interface IAddressLookupService
{
    Task<IReadOnlyList<AddressOptionDto>> ListRegionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AddressOptionDto>> ListProvincesAsync(long regionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AddressOptionDto>> ListCitiesAsync(long provinceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AddressBarangayOptionDto>> ListBarangaysAsync(long cityId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RegisteredWarehouseDto>> ListRegisteredWarehousesAsync(CancellationToken cancellationToken = default);
}

public interface ISystemBrandingService
{
    Task<SystemBrandingDto> GetPublicBrandingAsync(CancellationToken cancellationToken = default);
}
