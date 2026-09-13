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
    suspend fun updateStatus(uuid: String, status: String) =
        api().updateContainerStatus(uuid, UpdateContainerStatusRequest(status)).requireData()
    suspend fun recordLocation(uuid: String, latitude: Double, longitude: Double) =
        api().recordLocation(uuid, RecordContainerLocationRequest(latitude, longitude)).requireData()
    suspend fun track(uuid: String) = api().containerTrack(uuid).requireData()
    suspend fun healthCheck(): Boolean = runCatching { api().health().requireData() }.isSuccess
    suspend fun previewTransportQr(qrData: String) =
        api().previewTransportQr(ScanTransportQrRequest(qrData)).requireData()
    suspend fun acceptTransportQr(qrData: String) =
        api().acceptTransportQr(ScanTransportQrRequest(qrData)).requireData()
    suspend fun checkInAtWarehouse(uuid: String, latitude: Double, longitude: Double) =
        api().checkInAtWarehouse(uuid, DriverWarehouseCheckInRequest(latitude, longitude)).requireData()
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
}

class PushRepository(private val container: AppContainer) {
    private suspend fun api() = container.services().api

    suspend fun register(token: String, platform: String) =
        api().registerPush(RegisterMobilePushDeviceRequest(token, platform)).requireData()

    suspend fun unregister(token: String) =
        api().unregisterPush(token).requireData()
}
