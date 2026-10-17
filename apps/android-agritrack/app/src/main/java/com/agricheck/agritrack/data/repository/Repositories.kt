package com.agricheck.agritrack.data.repository

import com.agricheck.agritrack.AppContainer
import com.agricheck.agritrack.data.api.AuthInterceptor
import com.agricheck.agritrack.data.api.apiErrorMessage
import com.agricheck.agritrack.data.api.requireData
import com.agricheck.agritrack.data.api.userMessage
import com.agricheck.agritrack.data.local.TokenStore
import com.agricheck.agritrack.data.model.*
import com.agricheck.agritrack.data.api.AgriCheckApiService
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.MultipartBody
import okhttp3.RequestBody.Companion.toRequestBody
import retrofit2.HttpException
import java.time.Instant

class AuthRepository(
    private val container: AppContainer,
    private val tokenStore: TokenStore,
) {
    val authState = tokenStore.authState

    private suspend fun api(): AgriCheckApiService = container.services().api
    private suspend fun authInterceptor(): AuthInterceptor = container.services().authInterceptor

    suspend fun login(email: String, password: String): Result<UserSummaryDto> {
        return try {
            val response = api().login(LoginRequest(email.trim(), password)).requireData()
            val allowed = response.user.roles.any {
                it.equals("ROLE_DRIVER", true) ||
                    it.equals("ROLE_OPERATOR", true) ||
                    it.equals("ROLE_ADMIN", true)
            }
            if (!allowed) {
                tokenStore.clear()
                authInterceptor().resetSession()
                return Result.failure(IllegalStateException("Driver or operator access required."))
            }
            tokenStore.saveAuth(response.tokens, response.user)
            authInterceptor().resetSession()
            Result.success(response.user)
        } catch (error: Throwable) {
            tokenStore.clear()
            authInterceptor().resetSession()
            Result.failure(IllegalStateException(error.userMessage("Login failed.")))
        }
    }

    suspend fun logout() {
        tokenStore.clear()
        authInterceptor().resetSession()
    }

    /**
     * Keeps the driver signed in: silently refreshes an expired access token using the refresh token.
     * Called on cold start and when the app returns to the foreground.
     */
    suspend fun maintainSession() {
        if (!tokenStore.hasUsableRefreshToken()) return
        val user = tokenStore.getCurrentUser()
        val accessExpired = tokenStore.isAccessTokenExpired()
        val accessMissing = tokenStore.getAccessToken().isNullOrBlank()
        if (user != null && !accessExpired && !accessMissing) return
        refreshSessionQuietly()
    }

    private suspend fun refreshSessionQuietly() {
        val refresh = tokenStore.getRefreshToken() ?: return
        try {
            val response = api().refresh(RefreshTokenRequest(refresh)).requireData()
            tokenStore.saveAuth(response.tokens, response.user)
            authInterceptor().resetSession()
        } catch (error: HttpException) {
            if (error.code() == 400 || error.code() == 401 || error.code() == 403) {
                tokenStore.clear()
                authInterceptor().resetSession()
            }
        } catch (_: Throwable) {
            // Offline or server error — keep existing session.
        }
    }

    suspend fun validateInvite(code: String): Result<InviteCodeValidationDto> = try {
        Result.success(api().validateInvite(ValidateInviteCodeRequest(code.trim())).requireData())
    } catch (error: Throwable) {
        Result.failure(IllegalStateException(error.userMessage("Invite validation failed.")))
    }

    suspend fun registerDriver(request: RegisterDriverRequest): Result<UserSummaryDto> {
        return try {
            val response = api().registerDriver(request).requireData()
            tokenStore.saveAuth(response.tokens, response.user)
            authInterceptor().resetSession()
            Result.success(response.user)
        } catch (error: Throwable) {
            tokenStore.clear()
            authInterceptor().resetSession()
            Result.failure(IllegalStateException(error.apiErrorMessage("Registration failed. Check your email, password, and invite code.")))
        }
    }
}

class DriverRepository(private val container: AppContainer) {
    private suspend fun api() = container.services().api

    suspend fun dashboard() = api().driverDashboard().requireData()
    suspend fun profile() = api().driverProfile().requireData()
    suspend fun assignedContainers() = api().assignedContainers().requireData()
    suspend fun syncPull() = api().syncPull().requireData()
    suspend fun updateStatus(
        uuid: String,
        status: String,
        latitude: Double? = null,
        longitude: Double? = null,
    ) = api().updateContainerStatus(
        uuid,
        UpdateContainerStatusRequest(status, latitude, longitude),
    ).requireData()
    suspend fun confirmPickup(uuid: String, latitude: Double, longitude: Double, label: String? = null) =
        api().confirmPickup(uuid, ConfirmContainerPickupRequest(latitude, longitude, label)).requireData()

    suspend fun confirmPickupWithGeotag(
        uuid: String,
        latitude: Double,
        longitude: Double,
        label: String?,
        jpeg: ByteArray,
        capturedAt: Instant,
    ) = api().confirmPickupWithGeotag(
        uuid,
        MultipartBody.Part.createFormData(
            "photo",
            "pickup-geotag.jpg",
            jpeg.toRequestBody("image/jpeg".toMediaType()),
        ),
        latitude.toString().toRequestBody("text/plain".toMediaType()),
        longitude.toString().toRequestBody("text/plain".toMediaType()),
        label?.toRequestBody("text/plain".toMediaType()),
        capturedAt.toString().toRequestBody("text/plain".toMediaType()),
    ).requireData()
    suspend fun deliveryHandoffQr(uuid: String) = api().deliveryHandoffQr(uuid).requireData()
    suspend fun recordLocation(
        uuid: String,
        latitude: Double,
        longitude: Double,
        recordedAtIso: String? = null,
        capturedOffline: Boolean = false,
    ) = api()
        .recordLocation(uuid, RecordContainerLocationRequest(latitude, longitude, recordedAtIso, capturedOffline))
        .requireData()

    suspend fun recordLocationsBatch(uuid: String, points: List<RecordContainerLocationPointRequest>) =
        api().recordLocationsBatch(uuid, RecordContainerLocationsBatchRequest(points)).requireData()

    suspend fun reportCompliance(
        uuid: String,
        eventType: String,
        latitude: Double,
        longitude: Double,
        details: String? = null,
        offRouteMeters: Double? = null,
    ) = api().reportCompliance(
        uuid,
        ReportDeliveryComplianceRequest(eventType, latitude, longitude, details, offRouteMeters),
    ).requireData()

    suspend fun submitDriverIncidentReport(
        uuid: String,
        category: String,
        notes: String?,
        latitude: Double,
        longitude: Double,
        photo1: ByteArray,
        photo2: ByteArray,
        capturedAt: Instant,
    ) = api().submitDriverIncidentReport(
        uuid,
        MultipartBody.Part.createFormData(
            "photo1",
            "incident-geotag-1.jpg",
            photo1.toRequestBody("image/jpeg".toMediaType()),
        ),
        MultipartBody.Part.createFormData(
            "photo2",
            "incident-geotag-2.jpg",
            photo2.toRequestBody("image/jpeg".toMediaType()),
        ),
        category.toRequestBody("text/plain".toMediaType()),
        (notes ?: "").toRequestBody("text/plain".toMediaType()),
        latitude.toString().toRequestBody("text/plain".toMediaType()),
        longitude.toString().toRequestBody("text/plain".toMediaType()),
        capturedAt.toString().toRequestBody("text/plain".toMediaType()),
    ).requireData()
    suspend fun track(uuid: String) = api().containerTrack(uuid).requireData()
    suspend fun healthCheck(): Boolean = runCatching { api().health().requireData() }.isSuccess
    suspend fun previewTransportQr(qrData: String) =
        api().previewTransportQr(ScanTransportQrRequest(qrData)).requireData()
    suspend fun acceptTransportQr(qrData: String) =
        api().acceptTransportQr(ScanTransportQrRequest(qrData)).requireData()
    suspend fun checkInAtWarehouse(uuid: String, latitude: Double, longitude: Double) =
        api().checkInAtWarehouse(uuid, DriverWarehouseCheckInRequest(latitude, longitude)).requireData()

    suspend fun checkInAtWarehouseWithGeotag(
        uuid: String,
        latitude: Double,
        longitude: Double,
        jpeg: ByteArray,
        capturedAt: Instant,
    ) = api().checkInAtWarehouseWithGeotag(
        uuid,
        MultipartBody.Part.createFormData(
            "photo",
            "warehouse-geotag.jpg",
            jpeg.toRequestBody("image/jpeg".toMediaType()),
        ),
        latitude.toString().toRequestBody("text/plain".toMediaType()),
        longitude.toString().toRequestBody("text/plain".toMediaType()),
        capturedAt.toString().toRequestBody("text/plain".toMediaType()),
    ).requireData()

    suspend fun submitEmptyContainerGeotag(
        uuid: String,
        latitude: Double,
        longitude: Double,
        jpeg: ByteArray,
        capturedAt: Instant,
    ) = api().submitEmptyContainerGeotag(
        uuid,
        MultipartBody.Part.createFormData(
            "photo",
            "empty-container-geotag.jpg",
            jpeg.toRequestBody("image/jpeg".toMediaType()),
        ),
        latitude.toString().toRequestBody("text/plain".toMediaType()),
        longitude.toString().toRequestBody("text/plain".toMediaType()),
        capturedAt.toString().toRequestBody("text/plain".toMediaType()),
    ).requireData()

    suspend fun uploadDocument(documentType: String, bytes: ByteArray, fileName: String) {
        val part = MultipartBody.Part.createFormData(
            "file",
            fileName,
            bytes.toRequestBody("image/jpeg".toMediaType()),
        )
        api().uploadDriverDocument(documentType, part).requireData()
    }
    suspend fun submitFaceVerification(confidence: Float?) =
        api().submitFaceVerification(
            SubmitFaceVerificationRequest(
                confidence = confidence?.toDouble(),
                notes = if (confidence != null) "ML Kit face detected on device" else null,
            ),
        ).requireData()
    suspend fun listRegions() = api().listRegions().requireData()
    suspend fun listProvinces(regionId: Long) = api().listProvinces(regionId).requireData()
    suspend fun listCities(provinceId: Long) = api().listCities(provinceId).requireData()
    suspend fun listBarangays(cityId: Long) = api().listBarangays(cityId).requireData()
}

class OperatorRepository(private val container: AppContainer) {
    private suspend fun api() = container.services().api

    suspend fun claimableContainers() = api().claimableContainers().requireData()
    suspend fun claim(uuid: String) = api().claimContainer(uuid).requireData()
    suspend fun claimByQr(qrData: String) = api().claimContainerByQr(ScanTransportQrRequest(qrData)).requireData()
    suspend fun assignDriver(uuid: String, driverUserUuid: String) =
        api().assignDriver(uuid, AssignDriverRequest(driverUserUuid)).requireData()

    suspend fun claimedContainers() = api().claimedContainers().requireData()

    suspend fun getDeliveryStops(uuid: String) = api().getOperatorDeliveryStops(uuid).requireData()

    suspend fun bookDeliveryStops(uuid: String, request: BookOperatorDeliveryStopsRequest) =
        api().bookOperatorDeliveryStops(uuid, request).requireData()
}

class NewsRepository(private val container: AppContainer) {
    private suspend fun api() = container.services().api

    suspend fun list(category: String? = null, search: String? = null): List<AgencyNewsListItemDto> =
        api().listNews(category, search).requireData()

    suspend fun get(uuid: String): AgencyNewsDetailDto =
        api().getNews(uuid).requireData()
}

class PushRepository(private val container: AppContainer) {
    private suspend fun api() = container.services().api

    suspend fun register(token: String, platform: String) =
        api().registerPush(RegisterMobilePushDeviceRequest(token, platform)).requireData()

    suspend fun unregister(token: String) =
        api().unregisterPush(token).requireData()
}
