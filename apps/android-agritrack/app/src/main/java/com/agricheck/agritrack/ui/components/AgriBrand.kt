package com.agricheck.agritrack.ui.components

import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.agricheck.agritrack.R
import com.agricheck.agritrack.ui.strings.UiStrings
import com.agricheck.agritrack.ui.theme.AgriColors
import java.util.Calendar

@Composable
fun AgriBrandLogo(size: Dp = 32.dp, modifier: Modifier = Modifier) {
    Box(
        modifier
            .size(size)
            .clip(CircleShape)
            .background(AgriColors.Primary),
        contentAlignment = Alignment.Center,
    ) {
        Image(
            painter = painterResource(R.drawable.ic_launcher_foreground),
            contentDescription = UiStrings.Brand.name,
            modifier = Modifier.size(size * 0.62f),
        )
    }
}

@Composable
fun AgriBrandName(
    style: androidx.compose.ui.text.TextStyle = MaterialTheme.typography.titleMedium,
    color: Color = AgriColors.OnSurface,
) {
    Text(
        UiStrings.Brand.name,
        style = style,
        fontWeight = FontWeight.Bold,
        color = color,
    )
}

@OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class)
@Composable
fun AgriScreenTopBar(
    title: String,
    subtitle: String? = null,
    onBack: (() -> Unit)? = null,
    onLogout: (() -> Unit)? = null,
) {
    Surface(
        modifier = Modifier.fillMaxWidth(),
        color = Color.White,
        shadowElevation = 1.dp,
    ) {
        Row(
            Modifier
                .fillMaxWidth()
                .statusBarsPadding()
                .padding(horizontal = 8.dp, vertical = 8.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            if (onBack != null) {
                IconButton(onClick = onBack) {
                    Icon(
                        Icons.AutoMirrored.Filled.ArrowBack,
                        contentDescription = "Back",
                        tint = AgriColors.OnSurface,
                    )
                }
            } else {
                Spacer(Modifier.width(8.dp))
            }
            AgriBrandLogo(size = 28.dp)
            Spacer(Modifier.width(10.dp))
            Column(Modifier.weight(1f)) {
                Text(
                    UiStrings.Brand.name,
                    style = MaterialTheme.typography.labelMedium,
                    fontWeight = FontWeight.Bold,
                    color = AgriColors.PrimaryDark,
                )
                Text(title, fontWeight = FontWeight.Bold, style = MaterialTheme.typography.titleMedium)
                subtitle?.let {
                    Text(it, style = MaterialTheme.typography.bodySmall, color = AgriColors.TextSecondary)
                }
            }
            if (onLogout != null) {
                TextButton(onClick = onLogout) {
                    Text("Log out", color = AgriColors.Error)
                }
            }
        }
    }
}

/** Compact strip for main app shell and screen scaffolds */
@Composable
fun AgriBrandTopBar(
    modifier: Modifier = Modifier,
    pageTitle: String? = null,
    pageSubtitle: String? = null,
) {
    Surface(
        modifier = modifier.fillMaxWidth(),
        color = Color.White,
        shadowElevation = 1.dp,
    ) {
        Row(
            Modifier
                .fillMaxWidth()
                .statusBarsPadding()
                .padding(horizontal = 16.dp, vertical = 10.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            AgriBrandLogo(size = 30.dp)
            Spacer(Modifier.width(10.dp))
            Column(Modifier.weight(1f)) {
                AgriBrandName(
                    style = MaterialTheme.typography.titleSmall.copy(fontSize = 17.sp),
                    color = AgriColors.PrimaryDark,
                )
                if (pageTitle != null) {
                    Text(
                        pageTitle,
                        style = MaterialTheme.typography.labelMedium,
                        color = AgriColors.TextSecondary,
                        maxLines = 1,
                    )
                } else {
                    Text(
                        UiStrings.Brand.tagline,
                        style = MaterialTheme.typography.labelSmall,
                        color = AgriColors.TextSecondary,
                        maxLines = 1,
                    )
                }
            }
            pageSubtitle?.let {
                Text(it, style = MaterialTheme.typography.labelSmall, color = AgriColors.Primary)
            }
        }
    }
}

@Composable
fun AgriBrandHero(modifier: Modifier = Modifier) {
    Column(
        modifier = modifier.fillMaxWidth(),
        horizontalAlignment = Alignment.CenterHorizontally,
    ) {
        AgriBrandLogo(size = 76.dp)
        Spacer(Modifier.height(12.dp))
        Text(
            UiStrings.Brand.name,
            style = MaterialTheme.typography.headlineMedium,
            fontWeight = FontWeight.Bold,
            color = AgriColors.PrimaryDark,
        )
        Text(
            UiStrings.Brand.tagline,
            style = MaterialTheme.typography.bodyMedium,
            color = AgriColors.TextSecondary,
            textAlign = TextAlign.Center,
        )
    }
}

@Composable
fun AgriAuthFooter(modifier: Modifier = Modifier) {
    val year = Calendar.getInstance().get(Calendar.YEAR)
    Column(
        modifier = modifier.fillMaxWidth(),
        horizontalAlignment = Alignment.CenterHorizontally,
    ) {
        Text(
            UiStrings.Brand.copyright(year),
            style = MaterialTheme.typography.labelSmall,
            color = AgriColors.TextSecondary,
            textAlign = TextAlign.Center,
        )
        Text(
            UiStrings.Brand.integratedWith,
            style = MaterialTheme.typography.labelSmall,
            fontWeight = FontWeight.SemiBold,
            color = AgriColors.PrimaryDark,
            textAlign = TextAlign.Center,
        )
    }
}
