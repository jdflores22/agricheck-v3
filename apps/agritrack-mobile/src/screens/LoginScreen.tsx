import { useEffect, useState } from 'react'
import {
  ActivityIndicator,
  KeyboardAvoidingView,
  Platform,
  Pressable,
  StyleSheet,
  Text,
  TextInput,
  View,
} from 'react-native'
import { checkHealth } from '../api/client'
import { useAuth } from '../context/AuthContext'
import { defaultApiBaseUrl, resolveApiBaseUrl } from '../config'
import { getApiBaseUrl } from '../storage/session'
import { colors } from '../theme'

export function LoginScreen({ onOpenSettings }: { onOpenSettings: () => void }) {
  const { login } = useAuth()
  const [email, setEmail] = useState('driver@agricheck.local')
  const [password, setPassword] = useState('Driver@12345')
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [apiHint, setApiHint] = useState('')

  useEffect(() => {
    void (async () => {
      setApiHint(resolveApiBaseUrl(await getApiBaseUrl()))
    })()
  }, [])

  const handleLogin = async () => {
    setError('')
    setSubmitting(true)
    try {
      const healthy = await checkHealth()
      if (!healthy) {
        setError(`Cannot reach API at ${apiHint || defaultApiBaseUrl()}. Check Settings.`)
        return
      }
      await login(email.trim(), password)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Login failed')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <KeyboardAvoidingView behavior={Platform.OS === 'ios' ? 'padding' : undefined} style={styles.container}>
      <View style={styles.card}>
        <Text style={styles.brand}>AgriTrack</Text>
        <Text style={styles.subtitle}>Container transport linked to AgriCheck V3</Text>

        <TextInput
          style={styles.input}
          autoCapitalize="none"
          keyboardType="email-address"
          placeholder="Email"
          value={email}
          onChangeText={setEmail}
        />
        <TextInput
          style={styles.input}
          secureTextEntry
          placeholder="Password"
          value={password}
          onChangeText={setPassword}
        />

        {error ? <Text style={styles.error}>{error}</Text> : null}

        <Pressable style={[styles.button, submitting && styles.buttonDisabled]} onPress={handleLogin} disabled={submitting}>
          {submitting ? <ActivityIndicator color="#fff" /> : <Text style={styles.buttonText}>Sign in</Text>}
        </Pressable>

        <Pressable onPress={onOpenSettings}>
          <Text style={styles.link}>API: {apiHint || defaultApiBaseUrl()} · Change</Text>
        </Pressable>
      </View>
    </KeyboardAvoidingView>
  )
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.bg, justifyContent: 'center', padding: 24 },
  card: { backgroundColor: colors.card, borderRadius: 16, padding: 24, borderWidth: 1, borderColor: colors.border },
  brand: { fontSize: 28, fontWeight: '800', color: colors.primary, textAlign: 'center' },
  subtitle: { fontSize: 14, color: colors.muted, textAlign: 'center', marginTop: 8, marginBottom: 24 },
  input: {
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 10,
    paddingHorizontal: 14,
    paddingVertical: 12,
    marginBottom: 12,
    fontSize: 16,
    backgroundColor: '#fff',
  },
  button: { backgroundColor: colors.primary, borderRadius: 10, paddingVertical: 14, alignItems: 'center', marginTop: 8 },
  buttonDisabled: { opacity: 0.7 },
  buttonText: { color: '#fff', fontWeight: '700', fontSize: 16 },
  error: { color: colors.danger, marginBottom: 8, fontSize: 14 },
  link: { color: colors.primary, textAlign: 'center', marginTop: 16, fontSize: 13 },
})
