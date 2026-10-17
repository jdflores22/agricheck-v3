package com.agricheck.agritrack.data.api

import com.agricheck.agritrack.data.model.*
import okhttp3.MultipartBody
import okhttp3.RequestBody
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

    @GET("api/v1/system/branding")
    suspend fun getSystemBranding(): ApiEnvelope<SystemBrandingDto>

    @GET("api/mobile/containers/assigned")
    suspend fun assignedContainers(): ApiEnvelope<List<ContainerListItemDto>>

    @PUT("api/mobile/containers/{uuid}/status")
    suspend fun updateContainerStatus(
        @Path("uuid") uuid: String,
        @Body request: UpdateContainerStatusRequest,
    ): ApiEnvelope<ContainerListItemDto>

    @POST("api/mobile/containers/{uuid}/pickup")
    suspend fun confirmPickup(
        @Path("uuid") uuid: String,
        @Body request: ConfirmContainerPickupRequest,
    ): ApiEnvelope<ContainerListItemDto>

    @Multipart
    @POST("api/mobile/containers/{uuid}/pickup")
    suspend fun confirmPickupWithGeotag(
        @Path("uuid") uuid: String,
        @Part photo: MultipartBody.Part,
        @Part("latitude") latitude: RequestBody,
        @Part("longitude") longitude: RequestBody,
        @Part("label") label: RequestBody?,
        @Part("capturedAt") capturedAt: RequestBody,
    ): ApiEnvelope<ContainerListItemDto>

    @GET("api/mobile/containers/{uuid}/delivery-qr")
    suspend fun deliveryHandoffQr(@Path("uuid") uuid: String): ApiEnvelope<DriverDeliveryHandoffQrDto>

    @POST("api/mobile/containers/{uuid}/location")
    suspend fun recordLocation(
        @Path("uuid") uuid: String,
        @Body request: RecordContainerLocationRequest,
    ): ApiEnvelope<Map<String, Boolean>>

    @POST("api/mobile/containers/{uuid}/locations/batch")
    suspend fun recordLocationsBatch(
        @Path("uuid") uuid: String,
        @Body request: RecordContainerLocationsBatchRequest,
    ): ApiEnvelope<Map<String, Int>>

    @POST("api/mobile/containers/{uuid}/compliance-events")
    suspend fun reportCompliance(
        @Path("uuid") uuid: String,
        @Body request: ReportDeliveryComplianceRequest,
    ): ApiEnvelope<Map<String, Boolean>>

    @Multipart
    @POST("api/mobile/containers/{uuid}/driver-incident-report")
    suspend fun submitDriverIncidentReport(
        @Path("uuid") uuid: String,
        @Part photo1: MultipartBody.Part,
        @Part photo2: MultipartBody.Part,
        @Part("category") category: RequestBody,
        @Part("notes") notes: RequestBody,
        @Part("latitude") latitude: RequestBody,
        @Part("longitude") longitude: RequestBody,
        @Part("capturedAt") capturedAt: RequestBody,
    ): ApiEnvelope<DriverIncidentReportResultDto>

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
    @POST("api/mobile/containers/{uuid}/check-in")
    suspend fun checkInAtWarehouseWithGeotag(
        @Path("uuid") uuid: String,
        @Part photo: MultipartBody.Part,
        @Part("latitude") latitude: RequestBody,
        @Part("longitude") longitude: RequestBody,
        @Part("capturedAt") capturedAt: RequestBody,
    ): ApiEnvelope<DriverWarehouseCheckInResultDto>

    @Multipart
    @POST("api/mobile/containers/{uuid}/empty-container-geotag")
    suspend fun submitEmptyContainerGeotag(
        @Path("uuid") uuid: String,
        @Part photo: MultipartBody.Part,
        @Part("latitude") latitude: RequestBody,
        @Part("longitude") longitude: RequestBody,
        @Part("capturedAt") capturedAt: RequestBody,
    ): ApiEnvelope<ContainerListItemDto>

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

    @GET("api/mobile/news")
    suspend fun listNews(
        @Query("category") category: String? = null,
        @Query("search") search: String? = null,
    ): ApiEnvelope<List<AgencyNewsListItemDto>>

    @GET("api/mobile/news/{uuid}")
    suspend fun getNews(@Path("uuid") uuid: String): ApiEnvelope<AgencyNewsDetailDto>

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

    @GET("api/mobile/operator/containers/claimed")
    suspend fun claimedContainers(): ApiEnvelope<List<ContainerListItemDto>>

    @GET("api/mobile/operator/containers/{uuid}/delivery-stops")
    suspend fun getOperatorDeliveryStops(@Path("uuid") uuid: String): ApiEnvelope<OperatorDeliveryStopsDto>

    @PUT("api/mobile/operator/containers/{uuid}/delivery-stops")
    suspend fun bookOperatorDeliveryStops(
        @Path("uuid") uuid: String,
        @Body request: BookOperatorDeliveryStopsRequest,
    ): ApiEnvelope<OperatorDeliveryStopsDto>

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
