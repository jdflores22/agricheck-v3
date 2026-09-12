export interface ApiEnvelope<T> {
  success: boolean
  data?: T
  errors?: Array<{ code: string; message: string }> | null
}

export interface AuthTokens {
  accessToken: string
  refreshToken: string
  accessTokenExpiresAt: string
  refreshTokenExpiresAt: string
}

export interface AuthUser {
  uuid: string
  email: string
  firstName: string
  lastName: string
  status: string
  roles: string[]
}

export interface AuthResponse {
  tokens: AuthTokens
  user: AuthUser
  redirectPath?: string
}

export interface ContainerListItem {
  uuid: string
  containerNumber: string
  entryUuid: string
  entryReference: string
  status: string
  departureTime?: string
  arrivalTime?: string
  lastLatitude?: number
  lastLongitude?: number
  lastLocationAt?: string
}

export interface DriverDashboard {
  assignedContainers: number
  inTransitContainers: number
  profileCompletion: number
  faceVerified: boolean
}

export interface DriverProfile {
  licenseNumber?: string
  licenseExpiryDate?: string
  vehicleType?: string
  vehicleRegistration?: string
  phoneNumber?: string
  emergencyContact?: string
  emergencyPhone?: string
  address?: string
  completionPercentage: number
  faceVerified: boolean
  submittedAt?: string
  approvedAt?: string
}

export interface MobileSyncPullResult {
  containers: ContainerListItem[]
  serverTime: string
}
