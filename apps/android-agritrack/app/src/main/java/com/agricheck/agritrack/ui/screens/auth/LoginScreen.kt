package com.agricheck.agritrack.ui.screens.auth

import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import com.agricheck.agritrack.R
import com.agricheck.agritrack.data.repository.AuthRepository
import com.agricheck.agritrack.ui.components.*
import com.agricheck.agritrack.ui.theme.AgriColors

@Composable
fun LoginScreen(
    authRepository: AuthRepository,
    onLoggedIn: () -> Unit,
    onRegister: () -> Unit,
) {
    var email by remember { mutableStateOf("") }
    var password by remember { mutableStateOf("") }
    var loading by remember { mutableStateOf(false) }
    var error by remember { mutableStateOf<String?>(null) }

    AgriAuthScreenLayout {
        AgriAuthCard(
            title = stringResource(R.string.login_title),
            subtitle = stringResource(R.string.login_subtitle),
        ) {
            AgriOutlinedField(email, { email = it }, stringResource(R.string.email))
            Spacer(Modifier.height(12.dp))
            AgriOutlinedField(password, { password = it }, stringResource(R.string.password), password = true)

            error?.let {
                Spacer(Modifier.height(8.dp))
                Text(it, color = AgriColors.Error, style = MaterialTheme.typography.bodySmall, textAlign = TextAlign.Center)
            }

            Spacer(Modifier.height(20.dp))
            AgriPrimaryButton(
                text = stringResource(R.string.sign_in),
                onClick = { loading = true; error = null },
                enabled = email.isNotBlank() && password.isNotBlank(),
                loading = loading,
            )

            LaunchedEffect(loading) {
                if (loading) {
                    authRepository.login(email.trim(), password)
                        .onSuccess { onLoggedIn() }
                        .onFailure { error = it.message ?: "Login failed" }
                    loading = false
                }
            }

            Spacer(Modifier.height(12.dp))
            TextButton(onClick = onRegister, modifier = Modifier.fillMaxWidth()) {
                Text("New driver? Register with invite code", color = AgriColors.Primary)
            }

            Spacer(Modifier.height(8.dp))
            Text(
                "Demo: driver@agricheck.local / Driver@12345",
                style = MaterialTheme.typography.bodySmall,
                color = AgriColors.TextSecondary,
                textAlign = TextAlign.Center,
                modifier = Modifier.fillMaxWidth(),
            )
        }
    }
}
