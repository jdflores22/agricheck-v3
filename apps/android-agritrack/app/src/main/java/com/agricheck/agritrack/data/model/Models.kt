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
data class DriverDocumentDto(
    val documentType: String,
    val originalFileName: String,
    val uploadedAt: String,
)

@Serializable
data class DriverProfileDto(
    val fullName: String? = null,
    val birthDate: String? = null,
    val licenseNumber: String? = null,
    val licenseExpiryDate: String? = null,
    val vehicleType: String? = null,
    val vehicleRegistration: String? = null,
    val phoneNumber: String? = null,
    val emergencyContact: String? = null,
    val emergencyPhone: String? = null,
    val address: String? = null,
    val regionId: Long? = null,
    val provinceId: Long? = null,
    val cityId: Long? = null,
    val barangayId: Long? = null,
    val zipCode: String? = null,
    val streetAddress: String? = null,
    val operatorName: String? = null,
    val documents: List<DriverDocumentDto> = emptyList(),
    val completionPercentage: Int = 0,
    val faceVerified: Boolean = false,
    val submittedAt: String? = null,
    val approvedAt: String? = null,
)

@Serializable
data class ValidateInviteCodeRequest(val inviteCode: String)

@Serializable
data class InviteCodeValidationDto(
    val isValid: Boolean,
    val operatorName: String? = null,
    val label: String? = null,
)

@Serializable
data class RegisterDriverRequest(
    val email: String,
    val password: String,
    val fullName: String,
    val birthDate: String,
    val inviteCode: String,
    val regionId: Long,
    val provinceId: Long,
    val cityId: Long,
    val barangayId: Long,
    val zipCode: String,
    val streetAddress: String,
    val licenseNumber: String,
    val licenseExpiryDate: String,
    val phoneNumber: String,
)

@Serializable
data class DriverTransportQrPreviewDto(
    val containerUuid: String,
    val containerNumber: String,
    val entryReference: String,
    val warehouseName: String,
    val warehouseAddress: String? = null,
    val warehouseLatitude: Double? = null,
    val warehouseLongitude: Double? = null,
    val scheduledWarehouseDate: String? = null,
    val containerStatus: String,
    val canAccept: Boolean,
    val blockReason: String? = null,
)

@Serializable
data class DriverWarehouseCheckInRequest(val latitude: Double, val longitude: Double)

@Serializable
data class DriverWarehouseCheckInResultDto(
    val withinGeofence: Boolean,
    val distanceMeters: Double,
    val container: ContainerListItemDto,
)

@Serializable
data class SubmitFaceVerificationRequest(
    val confidence: Double? = null,
    val notes: String? = null,
)

@Serializable
data class AddressOptionDto(
    val id: Long,
    val code: String,
    val name: String,
)

@Serializable
data class AddressBarangayOptionDto(
    val id: Long,
    val code: String,
    val name: String,
    val zipCode: String? = null,
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
