using AgriCheck.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace AgriCheck.Api.Controllers;

[ApiController]
[Route("api/v1/addresses")]
public class AddressController : ControllerBase
{
    private readonly IAddressLookupService _service;

    public AddressController(IAddressLookupService service) => _service = service;

    [HttpGet("regions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AddressOptionDto>>>> ListRegions(CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<AddressOptionDto>>.Ok(await _service.ListRegionsAsync(cancellationToken)));

    [HttpGet("regions/{regionId:long}/provinces")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AddressOptionDto>>>> ListProvinces(long regionId, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<AddressOptionDto>>.Ok(await _service.ListProvincesAsync(regionId, cancellationToken)));

    [HttpGet("provinces/{provinceId:long}/cities")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AddressOptionDto>>>> ListCities(long provinceId, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<AddressOptionDto>>.Ok(await _service.ListCitiesAsync(provinceId, cancellationToken)));

    [HttpGet("cities/{cityId:long}/barangays")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AddressBarangayOptionDto>>>> ListBarangays(long cityId, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<AddressBarangayOptionDto>>.Ok(await _service.ListBarangaysAsync(cityId, cancellationToken)));

    [HttpGet("warehouses")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RegisteredWarehouseDto>>>> ListRegisteredWarehouses(CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<RegisteredWarehouseDto>>.Ok(await _service.ListRegisteredWarehousesAsync(cancellationToken)));
}
