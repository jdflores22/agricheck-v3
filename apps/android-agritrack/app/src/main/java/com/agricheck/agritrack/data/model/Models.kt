package com.agricheck.agritrack.data.model

import kotlinx.serialization.Serializable

@Serializable
data class ApiEnvelope<T>(
    val success: Boolean,
    val data: T? = null,
    val errors: List<ApiError>? = null,
)

@Serializable
data class ApiError(
    val code: String? = null,
    val message: String? = null,
)

@Serializable
data class LoginRequest(val email: String, val password: String)

@Serializable
data class RefreshTokenRequest(val refreshToken: String)

@Serializable
data class AuthTokensDto(
    val accessToken: String,
    val refreshToken: String,
    val accessTokenExpiresAt: String,
    val refreshTokenExpiresAt: String,
)

@Serializable
data class UserSummaryDto(
    val uuid: String,
    val email: String,
    val firstName: String,
    val lastName: String,
    val status: String,
    val emailVerified: Boolean = false,
    val mustChangePassword: Boolean = false,
    val roles: List<String> = emptyList(),
)

@Serializable
data class AuthResponseDto(
    val tokens: AuthTokensDto,
    val user: UserSummaryDto,
    val redirectPath: String? = null,
)

@Serializable
data class ContainerListItemDto(
    val uuid: String,
    val containerNumber: String,
    val entryUuid: String,
    val entryReference: String,
    val status: String,
    val departureTime: String? = null,
    val arrivalTime: String? = null,
    val lastLatitude: Double? = null,
    val lastLongitude: Double? = null,
    val lastLocationAt: String? = null,
)

@Serializable
data class UpdateContainerStatusRequest(val status: String)

@Serializable
data class RecordContainerLocationRequest(val latitude: Double, val longitude: Double)

@Serializable
data class RegisterMobilePushDeviceRequest(val expoPushToken: String, val platform: String)

@Serializable
data class AssignDriverRequest(val driverUserUuid: String)

@Serializable
data class ScanTransportQrRequest(val qrData: String)

@Serializable
data class DriverDashboardDto(
    val assignedContainers: Int,
    val inTransitContainers: Int,
    val profileCompletion: Int,
    val faceVerified: Boolean,
)

@Serializable
data class DriverProfileDto(
    val licenseNumber: String? = null,
    val licenseExpiryDate: String? = null,
    val vehicleType: String? = null,
    val vehicleRegistration: String? = null,
    val phoneNumber: String? = null,
    val emergencyContact: String? = null,
    val emergencyPhone: String? = null,
    val address: String? = null,
    val completionPercentage: Int = 0,
    val faceVerified: Boolean = false,
    val submittedAt: String? = null,
    val approvedAt: String? = null,
)

@Serializable
data class MobileSyncPullResultDto(
    val containers: List<ContainerListItemDto> = emptyList(),
    val serverTime: String,
)

@Serializable
data class ContainerTrackDestinationDto(
    val name: String,
    val latitude: Double,
    val longitude: Double,
)

@Serializable
data class ContainerLocationPointDto(
    val latitude: Double,
    val longitude: Double,
    val recordedAt: String,
)

@Serializable
data class ContainerTrackDto(
    val containerUuid: String,
    val containerNumber: String,
    val entryReference: String,
    val status: String,
    val currentLatitude: Double? = null,
    val currentLongitude: Double? = null,
    val destination: ContainerTrackDestinationDto,
    val trail: List<ContainerLocationPointDto> = emptyList(),
)
