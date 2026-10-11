package com.agricheck.agritrack.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.horizontalScroll
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Search
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import coil.compose.AsyncImage
import com.agricheck.agritrack.data.model.AgencyNewsDetailDto
import com.agricheck.agritrack.data.model.AgencyNewsListItemDto
import com.agricheck.agritrack.data.repository.NewsRepository
import com.agricheck.agritrack.ui.components.*
import com.agricheck.agritrack.ui.strings.UiStrings
import com.agricheck.agritrack.ui.theme.AgriColors
import kotlinx.coroutines.launch
import java.time.Instant
import java.time.ZoneId
import java.time.format.DateTimeFormatter
import java.util.Locale

private data class NewsFilter(val apiValue: String?, val label: String)

@Composable
fun NewsScreen(repository: NewsRepository) {
    val filters = remember {
        listOf(
            NewsFilter(null, UiStrings.News.filterAll),
            NewsFilter("News", UiStrings.News.filterNews),
            NewsFilter("Announcement", UiStrings.News.filterAnnouncements),
            NewsFilter("Article", UiStrings.News.filterArticles),
        )
    }
    var selectedFilter by remember { mutableIntStateOf(0) }
    var search by remember { mutableStateOf("") }
    var items by remember { mutableStateOf<List<AgencyNewsListItemDto>>(emptyList()) }
    var loading by remember { mutableStateOf(true) }
    var error by remember { mutableStateOf<String?>(null) }
    var detail by remember { mutableStateOf<AgencyNewsDetailDto?>(null) }
    var detailLoading by remember { mutableStateOf(false) }
    val scope = rememberCoroutineScope()

    fun load() {
        scope.launch {
            loading = true
            error = null
            runCatching {
                repository.list(
                    category = filters[selectedFilter].apiValue,
                    search = search.takeIf { it.isNotBlank() },
                )
            }.onSuccess { items = it }
                .onFailure { error = it.message ?: UiStrings.News.loadFailed }
            loading = false
        }
    }

    LaunchedEffect(selectedFilter) { load() }

    if (detail != null) {
        NewsDetailScreen(
            item = detail!!,
            loading = detailLoading,
            onBack = { detail = null },
        )
        return
    }

    Column(
        Modifier
            .fillMaxSize()
            .background(AgriColors.Background),
    ) {
        OutlinedTextField(
            value = search,
            onValueChange = { search = it },
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 20.dp, vertical = 12.dp),
            placeholder = { Text(UiStrings.News.searchHint) },
            leadingIcon = { Icon(Icons.Default.Search, contentDescription = null) },
            singleLine = true,
            shape = RoundedCornerShape(14.dp),
            colors = OutlinedTextFieldDefaults.colors(
                focusedContainerColor = Color.White,
                unfocusedContainerColor = Color.White,
            ),
            trailingIcon = {
                if (search.isNotBlank()) {
                    TextButton(onClick = { load() }) { Text("Go") }
                }
            },
        )
        Row(
            Modifier
                .horizontalScroll(rememberScrollState())
                .padding(horizontal = 16.dp),
            horizontalArrangement = Arrangement.spacedBy(8.dp),
        ) {
            filters.forEachIndexed { index, filter ->
                FilterChip(
                    selected = selectedFilter == index,
                    onClick = { selectedFilter = index },
                    label = { Text(filter.label) },
                    colors = FilterChipDefaults.filterChipColors(
                        selectedContainerColor = AgriColors.Primary.copy(alpha = 0.18f),
                        selectedLabelColor = AgriColors.PrimaryDark,
                    ),
                )
            }
        }
        Spacer(Modifier.height(8.dp))
        when {
            loading && items.isEmpty() -> LoadingState()
            error != null && items.isEmpty() -> ErrorState(error ?: UiStrings.News.loadFailed) { load() }
            items.isEmpty() -> EmptyState(UiStrings.News.empty)
            else -> {
                LazyColumn(
                    contentPadding = PaddingValues(horizontal = 20.dp, vertical = 8.dp),
                    verticalArrangement = Arrangement.spacedBy(12.dp),
                ) {
                    items(items, key = { it.uuid }) { post ->
                        NewsListCard(
                            post = post,
                            onClick = {
                                scope.launch {
                                    detailLoading = true
                                    runCatching { repository.get(post.uuid) }
                                        .onSuccess { detail = it }
                                        .onFailure { error = it.message }
                                    detailLoading = false
                                }
                            },
                        )
                    }
                }
            }
        }
    }
}

@Composable
private fun NewsListCard(post: AgencyNewsListItemDto, onClick: () -> Unit) {
    Surface(
        modifier = Modifier
            .fillMaxWidth()
            .clickable(onClick = onClick),
        shape = RoundedCornerShape(16.dp),
        color = Color.White,
        shadowElevation = 1.dp,
    ) {
        Row(Modifier.padding(12.dp), verticalAlignment = Alignment.Top) {
            if (!post.imageUrl.isNullOrBlank()) {
                AsyncImage(
                    model = post.imageUrl,
                    contentDescription = null,
                    modifier = Modifier
                        .size(72.dp)
                        .clip(RoundedCornerShape(12.dp)),
                    contentScale = ContentScale.Crop,
                )
                Spacer(Modifier.width(12.dp))
            }
            Column(Modifier.weight(1f)) {
                Row(horizontalArrangement = Arrangement.spacedBy(6.dp), verticalAlignment = Alignment.CenterVertically) {
                    AgencyBadge(post.agencyCode, post.agencyName)
                    if (post.isRegulatory) {
                        RegulatoryBadge()
                    }
                }
                Spacer(Modifier.height(6.dp))
                Text(
                    post.title,
                    fontWeight = FontWeight.SemiBold,
                    color = AgriColors.OnSurface,
                    maxLines = 2,
                    overflow = TextOverflow.Ellipsis,
                )
                Spacer(Modifier.height(4.dp))
                Text(
                    UiStrings.News.published(formatNewsDate(post.publishedAt)),
                    style = MaterialTheme.typography.bodySmall,
                    color = AgriColors.TextSecondary,
                )
                post.summary?.takeIf { it.isNotBlank() }?.let {
                    Spacer(Modifier.height(4.dp))
                    Text(
                        it,
                        style = MaterialTheme.typography.bodySmall,
                        color = AgriColors.TextSecondary,
                        maxLines = 2,
                        overflow = TextOverflow.Ellipsis,
                    )
                }
            }
        }
    }
}

@Composable
private fun NewsDetailScreen(item: AgencyNewsDetailDto, loading: Boolean, onBack: () -> Unit) {
    Column(
        Modifier
            .fillMaxSize()
            .background(AgriColors.Background),
    ) {
        Row(
            Modifier
                .fillMaxWidth()
                .padding(horizontal = 8.dp, vertical = 8.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            IconButton(onClick = onBack) {
                Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = UiStrings.Map.back)
            }
            Text(UiStrings.News.title, fontWeight = FontWeight.Bold, modifier = Modifier.weight(1f))
        }
        if (loading) {
            LoadingState()
        } else {
            LazyColumn(
                contentPadding = PaddingValues(horizontal = 20.dp, vertical = 8.dp),
                verticalArrangement = Arrangement.spacedBy(12.dp),
            ) {
                item {
                    Row(horizontalArrangement = Arrangement.spacedBy(6.dp)) {
                        AgencyBadge(item.agencyCode, item.agencyName)
                        if (item.isRegulatory) RegulatoryBadge()
                    }
                }
                item {
                    Text(item.title, style = MaterialTheme.typography.headlineSmall, fontWeight = FontWeight.Bold)
                }
                item {
                    Text(
                        UiStrings.News.published(formatNewsDate(item.publishedAt)),
                        color = AgriColors.TextSecondary,
                        style = MaterialTheme.typography.bodySmall,
                    )
                }
                if (!item.imageUrl.isNullOrBlank()) {
                    item {
                        AsyncImage(
                            model = item.imageUrl,
                            contentDescription = null,
                            modifier = Modifier
                                .fillMaxWidth()
                                .height(180.dp)
                                .clip(RoundedCornerShape(16.dp)),
                            contentScale = ContentScale.Crop,
                        )
                    }
                }
                item {
                    Text(item.body, style = MaterialTheme.typography.bodyLarge, color = AgriColors.OnSurface)
                }
            }
        }
    }
}

@Composable
fun AgencyBadge(code: String, name: String) {
    Surface(
        color = AgriColors.Primary.copy(alpha = 0.12f),
        shape = RoundedCornerShape(8.dp),
    ) {
        Text(
            code,
            modifier = Modifier.padding(horizontal = 8.dp, vertical = 4.dp),
            style = MaterialTheme.typography.labelSmall,
            fontWeight = FontWeight.Bold,
            color = AgriColors.PrimaryDark,
        )
    }
}

@Composable
private fun RegulatoryBadge() {
    Surface(
        color = AgriColors.Warning.copy(alpha = 0.15f),
        shape = RoundedCornerShape(8.dp),
    ) {
        Text(
            UiStrings.News.regulatory,
            modifier = Modifier.padding(horizontal = 8.dp, vertical = 4.dp),
            style = MaterialTheme.typography.labelSmall,
            fontWeight = FontWeight.SemiBold,
            color = AgriColors.Warning,
        )
    }
}

private fun formatNewsDate(raw: String): String = runCatching {
    val instant = Instant.parse(raw)
    val formatter = DateTimeFormatter.ofPattern("MMM d, yyyy", Locale.US)
    formatter.format(instant.atZone(ZoneId.systemDefault()))
}.getOrElse { raw.take(10) }
