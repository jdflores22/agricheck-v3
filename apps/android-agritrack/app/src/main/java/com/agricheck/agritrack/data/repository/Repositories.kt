package com.agricheck.agritrack.data.repository

import com.agricheck.agritrack.AppContainer
import com.agricheck.agritrack.data.api.AuthInterceptor
import com.agricheck.agritrack.data.api.requireData
import com.agricheck.agritrack.data.api.userMessage
import com.agricheck.agritrack.data.local.TokenStore
import com.agricheck.agritrack.data.model.*
import com.agricheck.agritrack.data.api.AgriCheckApiService

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
