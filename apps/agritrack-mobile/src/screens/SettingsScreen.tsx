import { useEffect, useState } from 'react'
import { Pressable, StyleSheet, Text, TextInput, View } from 'react-native'
import { checkHealth } from '../api/client'
import { defaultApiBaseUrl, resolveApiBaseUrl } from '../config'
import { getApiBaseUrl, setApiBaseUrl } from '../storage/session'
import { colors } from '../theme'

export function SettingsScreen({ onBack }: { onBack: () => void }) {
  const [url, setUrl] = useState(defaultApiBaseUrl())
  const [status, setStatus] = useState('')

  useEffect(() => {
    void (async () => {
      setUrl(resolveApiBaseUrl(await getApiBaseUrl()))
    })()
  }, [])

  const save = async () => {
    await setApiBaseUrl(url.trim())
    const ok = await checkHealth()
    setStatus(ok ? 'Connected to API.' : 'Saved, but health check failed.')
  }

  return (
    <View style={styles.container}>
      <Text style={styles.title}>API Settings</Text>
      <Text style={styles.help}>
        Production: https://agricheck-v3-production.up.railway.app{'\n'}
        Local emulator: http://10.0.2.2:5000
      </Text>
      <TextInput style={styles.input} value={url} onChangeText={setUrl} autoCapitalize="none" autoCorrect={false} />
      {status ? <Text style={styles.status}>{status}</Text> : null}
      <Pressable style={styles.primaryBtn} onPress={() => void save()}>
        <Text style={styles.primaryText}>Save & test</Text>
      </Pressable>
      <Pressable style={styles.linkBtn} onPress={onBack}>
        <Text style={styles.linkText}>Back</Text>
      </Pressable>
    </View>
  )
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.bg, padding: 16 },
  title: { fontSize: 24, fontWeight: '800', color: colors.text, marginBottom: 8 },
  help: { color: colors.muted, marginBottom: 12, lineHeight: 20 },
  input: {
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 10,
    paddingHorizontal: 12,
    paddingVertical: 10,
    backgroundColor: '#fff',
  },
  status: { marginTop: 12, color: colors.primary },
  primaryBtn: {
    marginTop: 16,
    backgroundColor: colors.primary,
    borderRadius: 10,
    paddingVertical: 14,
    alignItems: 'center',
  },
  primaryText: { color: '#fff', fontWeight: '700' },
  linkBtn: { marginTop: 16, alignItems: 'center' },
  linkText: { color: colors.primary, fontWeight: '600' },
})
