package com.agricheck.agritrack.ui.screens.auth

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.agricheck.agritrack.data.model.AddressBarangayOptionDto
import com.agricheck.agritrack.data.model.AddressOptionDto
import com.agricheck.agritrack.data.model.RegisterDriverRequest
import com.agricheck.agritrack.data.repository.AuthRepository
import com.agricheck.agritrack.data.repository.DriverRepository
import com.agricheck.agritrack.ui.camera.CameraCaptureScreen
import com.agricheck.agritrack.ui.components.*
import com.agricheck.agritrack.ui.theme.AgriColors
import kotlinx.coroutines.launch

private enum class RegisterStep { Invite, Account, Address, License, LicenseFront, LicenseBack, Selfie }

@Composable
fun RegisterScreen(
    authRepository: AuthRepository,
    driverRepository: DriverRepository,
    onRegistered: () -> Unit,
    onBackToLogin: () -> Unit,
) {
    var step by remember { mutableStateOf(RegisterStep.Invite) }
    var inviteCode by remember { mutableStateOf("") }
    var operatorName by remember { mutableStateOf<String?>(null) }
    var email by remember { mutableStateOf("") }
    var password by remember { mutableStateOf("") }
    var fullName by remember { mutableStateOf("") }
    var birthDate by remember { mutableStateOf("") }
    var phone by remember { mutableStateOf("") }
    var licenseNumber by remember { mutableStateOf("") }
    var licenseExpiry by remember { mutableStateOf("") }
    var street by remember { mutableStateOf("") }
    var zipCode by remember { mutableStateOf("") }
    var regions by remember { mutableStateOf<List<AddressOptionDto>>(emptyList()) }
    var provinces by remember { mutableStateOf<List<AddressOptionDto>>(emptyList()) }
    var cities by remember { mutableStateOf<List<AddressOptionDto>>(emptyList()) }
    var barangays by remember { mutableStateOf<List<AddressBarangayOptionDto>>(emptyList()) }
    var selectedRegion by remember { mutableStateOf<AddressOptionDto?>(null) }
    var selectedProvince by remember { mutableStateOf<AddressOptionDto?>(null) }
    var selectedCity by remember { mutableStateOf<AddressOptionDto?>(null) }
    var selectedBarangay by remember { mutableStateOf<AddressBarangayOptionDto?>(null) }
    var licenseFront by remember { mutableStateOf<ByteArray?>(null) }
    var licenseBack by remember { mutableStateOf<ByteArray?>(null) }
    var selfie by remember { mutableStateOf<ByteArray?>(null) }
    var loading by remember { mutableStateOf(false) }
    var submitting by remember { mutableStateOf(false) }
    var error by remember { mutableStateOf<String?>(null) }
    val scope = rememberCoroutineScope()

    LaunchedEffect(step) {
        if (step == RegisterStep.Address && regions.isEmpty()) {
            runCatching { regions = driverRepository.listRegions() }
        }
    }

    when (step) {
        RegisterStep.LicenseFront -> CameraCaptureScreen(
            title = "Capture driver's license (front)",
            useFrontCamera = false,
            onCaptured = { bytes -> licenseFront = bytes; step = RegisterStep.LicenseBack },
            onCancel = { step = RegisterStep.License },
        )
        RegisterStep.LicenseBack -> CameraCaptureScreen(
            title = "Capture driver's license (back)",
            useFrontCamera = false,
            onCaptured = { bytes -> licenseBack = bytes; error = null; step = RegisterStep.Selfie },
            onCancel = { step = RegisterStep.LicenseFront },
        )
        RegisterStep.Selfie -> if (submitting) {
            AgriAuthScreenLayout {
                Column(horizontalAlignment = androidx.compose.ui.Alignment.CenterHorizontally) {
                    CircularProgressIndicator(color = AgriColors.Primary)
                    Spacer(Modifier.height(12.dp))
                    Text("Creating your account…", color = AgriColors.OnSurface)
                    error?.let { Text(it, color = AgriColors.Error) }
                }
            }
        } else CameraCaptureScreen(
            title = "Take a selfie for accountability",
            useFrontCamera = true,
            hint = "Center your face in the frame, then tap Capture.",
            errorMessage = error,
            onCaptured = { bytes ->
                selfie = bytes
                submitting = true
                error = null
                scope.launch {
                    val request = RegisterDriverRequest(
                        email = email.trim().lowercase(),
                        password = password,
                        fullName = fullName.trim(),
                        birthDate = birthDate.trim(),
                        inviteCode = inviteCode.trim(),
                        regionId = selectedRegion!!.id,
                        provinceId = selectedProvince!!.id,
                        cityId = selectedCity!!.id,
                        barangayId = selectedBarangay!!.id,
                        zipCode = zipCode.trim(),
                        streetAddress = street.trim(),
                        licenseNumber = licenseNumber.trim(),
                        licenseExpiryDate = licenseExpiry.trim(),
                        phoneNumber = phone.trim(),
                    )
                    authRepository.registerDriver(request)
                        .onSuccess {
                            runCatching {
                                licenseFront?.let { driverRepository.uploadDocument("LicenseFront", it, "license-front.jpg") }
                                licenseBack?.let { driverRepository.uploadDocument("LicenseBack", it, "license-back.jpg") }
                                selfie?.let { driverRepository.uploadDocument("Selfie", it, "selfie.jpg") }
                                driverRepository.submitFaceVerification(null)
                            }
                            onRegistered()
                        }
                        .onFailure {
                            error = it.message ?: "Registration failed. Try again."
                            submitting = false
                        }
                }
            },
            onCancel = { step = RegisterStep.LicenseBack },
        )
        else -> AgriAuthScreenLayout {
            AgriAuthCard(
                title = "Driver registration",
                subtitle = when (step) {
                    RegisterStep.Invite -> "Enter your fleet operator invite code"
                    RegisterStep.Account -> "Create your AgriTrack login"
                    RegisterStep.Address -> "Philippine address (PSGC)"
                    RegisterStep.License -> "Driver's license details"
                    else -> ""
                },
            ) {
                Column(
                    Modifier.verticalScroll(rememberScrollState()),
                    verticalArrangement = Arrangement.spacedBy(10.dp),
                ) {
                    when (step) {
                        RegisterStep.Invite -> {
                            AgriOutlinedField(inviteCode, { inviteCode = it }, "Invite code")
                            operatorName?.let {
                                Text("Fleet: $it", color = AgriColors.Success, fontWeight = FontWeight.SemiBold)
                            }
                            AgriPrimaryButton(
                                text = "Validate code",
                                loading = loading,
                                enabled = inviteCode.isNotBlank(),
                                onClick = {
                                    loading = true
                                    error = null
                                    scope.launch {
                                        authRepository.validateInvite(inviteCode)
                                            .onSuccess { result ->
                                                if (result.isValid) {
                                                    operatorName = result.operatorName ?: result.label
                                                    step = RegisterStep.Account
                                                } else {
                                                    error = "Invalid or expired invite code."
                                                }
                                            }
                                            .onFailure { error = it.message }
                                        loading = false
                                    }
                                },
                            )
                        }
                        RegisterStep.Account -> {
                            AgriOutlinedField(fullName, { fullName = it }, "Full name")
                            AgriOutlinedField(birthDate, { birthDate = it }, "Birth date (YYYY-MM-DD)")
                            AgriOutlinedField(phone, { phone = it }, "Mobile number")
                            AgriOutlinedField(email, { email = it }, "Email")
                            AgriOutlinedField(password, { password = it }, "Password", password = true)
                            Text(passwordHint(), style = MaterialTheme.typography.bodySmall, color = AgriColors.TextSecondary)
                            AgriPrimaryButton(
                                "Next",
                                onClick = {
                                    error = when {
                                        !isIsoDate(birthDate.trim()) -> "Birth date must be YYYY-MM-DD."
                                        !isStrongPassword(password) -> passwordHint()
                                        else -> null
                                    }
                                    if (error == null) step = RegisterStep.Address
                                },
                                enabled = listOf(fullName, birthDate, phone, email, password).all { it.isNotBlank() },
                            )
                        }
                        RegisterStep.Address -> {
                            AddressDropdown("Region", regions, selectedRegion?.name) { option ->
                                selectedRegion = option
                                selectedProvince = null
                                selectedCity = null
                                selectedBarangay = null
                                scope.launch { provinces = driverRepository.listProvinces(option.id) }
                            }
                            if (provinces.isNotEmpty()) {
                                AddressDropdown("Province", provinces, selectedProvince?.name) { option ->
                                    selectedProvince = option
                                    selectedCity = null
                                    selectedBarangay = null
                                    scope.launch { cities = driverRepository.listCities(option.id) }
                                }
                            }
                            if (cities.isNotEmpty()) {
                                AddressDropdown("City / Municipality", cities, selectedCity?.name) { option ->
                                    selectedCity = option
                                    selectedBarangay = null
                                    scope.launch { barangays = driverRepository.listBarangays(option.id) }
                                }
                            }
                            if (barangays.isNotEmpty()) {
                                AddressBarangayDropdown(barangays, selectedBarangay) { option ->
                                    selectedBarangay = option
                                    option.zipCode?.let { zipCode = it }
                                }
                            }
                            AgriOutlinedField(street, { street = it }, "Street / house no.")
                            AgriOutlinedField(zipCode, { zipCode = it }, "ZIP code")
                            AgriPrimaryButton(
                                "Next",
                                onClick = { step = RegisterStep.License },
                                enabled = selectedRegion != null && selectedProvince != null && selectedCity != null && selectedBarangay != null && street.isNotBlank() && zipCode.isNotBlank(),
                            )
                        }
                        RegisterStep.License -> {
                            AgriOutlinedField(licenseNumber, { licenseNumber = it }, "Driver's license #")
                            AgriOutlinedField(licenseExpiry, { licenseExpiry = it }, "License expiry (YYYY-MM-DD)")
                            Text("Next: capture license photos and selfie.", style = MaterialTheme.typography.bodySmall, color = AgriColors.TextSecondary)
                            AgriPrimaryButton(
                                "Capture documents",
                                onClick = {
                                    error = if (!isIsoDate(licenseExpiry.trim())) {
                                        "License expiry must be YYYY-MM-DD."
                                    } else {
                                        null
                                    }
                                    if (error == null) step = RegisterStep.LicenseFront
                                },
                                enabled = licenseNumber.isNotBlank() && licenseExpiry.isNotBlank(),
                            )
                        }
                        else -> Unit
                    }

                    error?.let { Text(it, color = AgriColors.Error, style = MaterialTheme.typography.bodySmall) }
                    if (loading && step == RegisterStep.Selfie) {
                        Text("Creating account and uploading documents…", color = AgriColors.Primary)
                    }
                    TextButton(onClick = onBackToLogin, modifier = Modifier.fillMaxWidth()) {
                        Text("Back to sign in")
                    }
                    if (step != RegisterStep.Invite) {
                        Text(
                            "Demo invite: AGRITRACK-DEMO",
                            style = MaterialTheme.typography.bodySmall,
                            color = AgriColors.TextSecondary,
                        )
                    }
                }
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun AddressDropdown(
    label: String,
    options: List<AddressOptionDto>,
    selectedLabel: String?,
    onSelect: (AddressOptionDto) -> Unit,
) {
    var expanded by remember { mutableStateOf(false) }
    ExposedDropdownMenuBox(expanded = expanded, onExpandedChange = { expanded = !expanded }) {
        OutlinedTextField(
            value = selectedLabel ?: "Select $label",
            onValueChange = {},
            readOnly = true,
            label = { Text(label) },
            modifier = Modifier.menuAnchor(type = MenuAnchorType.PrimaryNotEditable).fillMaxWidth(),
        )
        ExposedDropdownMenu(expanded = expanded, onDismissRequest = { expanded = false }) {
            options.forEach { option ->
                DropdownMenuItem(
                    text = { Text(option.name) },
                    onClick = { onSelect(option); expanded = false },
                )
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun AddressBarangayDropdown(
    options: List<AddressBarangayOptionDto>,
    selected: AddressBarangayOptionDto?,
    onSelect: (AddressBarangayOptionDto) -> Unit,
) {
    var expanded by remember { mutableStateOf(false) }
    ExposedDropdownMenuBox(expanded = expanded, onExpandedChange = { expanded = !expanded }) {
        OutlinedTextField(
            value = selected?.name ?: "Select barangay",
            onValueChange = {},
            readOnly = true,
            label = { Text("Barangay") },
            modifier = Modifier.menuAnchor(type = MenuAnchorType.PrimaryNotEditable).fillMaxWidth(),
        )
        ExposedDropdownMenu(expanded = expanded, onDismissRequest = { expanded = false }) {
            options.forEach { option ->
                DropdownMenuItem(
                    text = { Text(option.name) },
                    onClick = { onSelect(option); expanded = false },
                )
            }
        }
    }
}
