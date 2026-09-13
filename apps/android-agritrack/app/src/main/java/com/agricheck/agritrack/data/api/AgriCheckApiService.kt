package com.agricheck.agritrack.data.api

import com.agricheck.agritrack.data.model.*
import okhttp3.MultipartBody
import retrofit2.http.Body
import retrofit2.http.DELETE
import retrofit2.http.GET
import retrofit2.http.Multipart
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Part
import retrofit2.http.Path
import retrofit2.http.Query

interface AgriCheckApiService {
    @POST("api/mobile/auth/login")
    suspend fun login(@Body request: LoginRequest): ApiEnvelope<AuthResponseDto>

    @POST("api/mobile/auth/register-driver")
    suspend fun registerDriver(@Body request: RegisterDriverRequest): ApiEnvelope<AuthResponseDto>

    @POST("api/mobile/auth/validate-invite")
    suspend fun validateInvite(@Body request: ValidateInviteCodeRequest): ApiEnvelope<InviteCodeValidationDto>

    @POST("api/v1/auth/refresh")
    suspend fun refresh(@Body request: RefreshTokenRequest): ApiEnvelope<AuthResponseDto>

    @GET("api/mobile/health")
    suspend fun health(): ApiEnvelope<Map<String, String>>

    @GET("api/mobile/containers/assigned")
    suspend fun assignedContainers(): ApiEnvelope<List<ContainerListItemDto>>

    @PUT("api/mobile/containers/{uuid}/status")
    suspend fun updateContainerStatus(
        @Path("uuid") uuid: String,
        @Body request: UpdateContainerStatusRequest,
    ): ApiEnvelope<ContainerListItemDto>

    @POST("api/mobile/containers/{uuid}/location")
    suspend fun recordLocation(
        @Path("uuid") uuid: String,
        @Body request: RecordContainerLocationRequest,
    ): ApiEnvelope<Map<String, Boolean>>

    @GET("api/mobile/containers/{uuid}/track")
    suspend fun containerTrack(@Path("uuid") uuid: String): ApiEnvelope<ContainerTrackDto>

    @POST("api/mobile/containers/scan/preview")
    suspend fun previewTransportQr(@Body request: ScanTransportQrRequest): ApiEnvelope<DriverTransportQrPreviewDto>

    @POST("api/mobile/containers/scan/accept")
    suspend fun acceptTransportQr(@Body request: ScanTransportQrRequest): ApiEnvelope<ContainerListItemDto>

    @POST("api/mobile/containers/{uuid}/check-in")
    suspend fun checkInAtWarehouse(
        @Path("uuid") uuid: String,
        @Body request: DriverWarehouseCheckInRequest,
    ): ApiEnvelope<DriverWarehouseCheckInResultDto>

    @Multipart
    @POST("api/mobile/driver/documents/{documentType}")
    suspend fun uploadDriverDocument(
        @Path("documentType") documentType: String,
        @Part file: MultipartBody.Part,
    ): ApiEnvelope<DriverDocumentDto>

    @POST("api/mobile/driver/face-verification")
    suspend fun submitFaceVerification(@Body request: SubmitFaceVerificationRequest): ApiEnvelope<DriverProfileDto>

    @GET("api/v1/addresses/regions")
    suspend fun listRegions(): ApiEnvelope<List<AddressOptionDto>>

    @GET("api/v1/addresses/regions/{regionId}/provinces")
    suspend fun listProvinces(@Path("regionId") regionId: Long): ApiEnvelope<List<AddressOptionDto>>

    @GET("api/v1/addresses/provinces/{provinceId}/cities")
    suspend fun listCities(@Path("provinceId") provinceId: Long): ApiEnvelope<List<AddressOptionDto>>

    @GET("api/v1/addresses/cities/{cityId}/barangays")
    suspend fun listBarangays(@Path("cityId") cityId: Long): ApiEnvelope<List<AddressBarangayOptionDto>>

    @GET("api/v1/ops/driver/dashboard")
    suspend fun driverDashboard(): ApiEnvelope<DriverDashboardDto>

    @GET("api/v1/ops/driver/profile")
    suspend fun driverProfile(): ApiEnvelope<DriverProfileDto>

    @GET("api/mobile/sync/pull")
    suspend fun syncPull(): ApiEnvelope<MobileSyncPullResultDto>

    @GET("api/mobile/operator/containers/claimable")
    suspend fun claimableContainers(): ApiEnvelope<List<ContainerListItemDto>>

    @POST("api/mobile/operator/containers/{uuid}/claim")
    suspend fun claimContainer(@Path("uuid") uuid: String): ApiEnvelope<ContainerListItemDto>

    @POST("api/mobile/operator/containers/scan")
    suspend fun claimContainerByQr(@Body request: ScanTransportQrRequest): ApiEnvelope<ContainerListItemDto>

    @POST("api/mobile/operator/containers/{uuid}/assign-driver")
    suspend fun assignDriver(
        @Path("uuid") uuid: String,
        @Body request: AssignDriverRequest,
    ): ApiEnvelope<ContainerListItemDto>

    @POST("api/mobile/push/register")
    suspend fun registerPush(@Body request: RegisterMobilePushDeviceRequest): ApiEnvelope<Map<String, Boolean>>

    @DELETE("api/mobile/push/register")
    suspend fun unregisterPush(@Query("expoPushToken") token: String): ApiEnvelope<Map<String, Boolean>>
}

fun Throwable.userMessage(fallback: String): String = apiErrorMessage(fallback)

fun <T> ApiEnvelope<T>.requireData(): T {
    if (!success || data == null) {
        throw IllegalStateException(errors?.firstOrNull()?.message ?: "Request failed")
    }
    return data
}
