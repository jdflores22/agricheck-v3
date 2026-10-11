package com.agricheck.agritrack.navigation

import android.content.Context
import android.speech.tts.TextToSpeech
import java.util.Locale
import java.util.concurrent.atomic.AtomicBoolean

class NavigationVoiceAnnouncer(context: Context) {
    private var tts: TextToSpeech? = null
    private val ready = AtomicBoolean(false)
    private var lastSpoken: String? = null
    private var lastSpokenAtMs: Long = 0

    var muted: Boolean = false

    init {
        tts = TextToSpeech(context.applicationContext) { status ->
            if (status == TextToSpeech.SUCCESS) {
                tts?.language = Locale.US
                tts?.setSpeechRate(0.95f)
                ready.set(true)
            }
        }
    }

    fun speak(prompt: String) {
        if (muted || !ready.get()) return
        val trimmed = prompt.trim()
        if (trimmed.isEmpty()) return
        val now = System.currentTimeMillis()
        if (trimmed == lastSpoken && now - lastSpokenAtMs < 8_000) return
        lastSpoken = trimmed
        lastSpokenAtMs = now
        tts?.speak(trimmed, TextToSpeech.QUEUE_FLUSH, null, "agritrack-nav-${now}")
    }

    fun stop() {
        tts?.stop()
        lastSpoken = null
        lastSpokenAtMs = 0
    }

    fun shutdown() {
        tts?.stop()
        tts?.shutdown()
        tts = null
        ready.set(false)
    }
}
