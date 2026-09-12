import { useCallback, useEffect, useState } from 'react'
import {
  ActivityIndicator,
  Alert,
  Pressable,
  RefreshControl,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  View,
} from 'react-native'
import { assignDriver, claimContainer, getClaimableContainers } from '../api/agritrackApi'
import type { ContainerListItem } from '../types/api'
import { colors } from '../theme'

export function OperatorScreen() {
  const [containers, setContainers] = useState<ContainerListItem[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [assignTarget, setAssignTarget] = useState<string | null>(null)
  const [driverUuid, setDriverUuid] = useState('')
  const [busy, setBusy] = useState(false)

  const load = useCallback(async () => {
    setError('')
    try {
      setContainers(await getClaimableContainers())
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load claimable containers')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  const handleClaim = async (uuid: string) => {
    setBusy(true)
    try {
      await claimContainer(uuid)
      await load()
      Alert.alert('Claimed', 'Container claimed successfully.')
    } catch (err) {
      Alert.alert('Claim failed', err instanceof Error ? err.message : 'Unknown error')
    } finally {
      setBusy(false)
    }
  }

  const handleAssign = async () => {
    if (!assignTarget || !driverUuid.trim()) return
    setBusy(true)
    try {
      await assignDriver(assignTarget, driverUuid.trim())
      setAssignTarget(null)
      setDriverUuid('')
      await load()
      Alert.alert('Assigned', 'Driver assigned successfully.')
    } catch (err) {
      Alert.alert('Assign failed', err instanceof Error ? err.message : 'Unknown error')
    } finally {
      setBusy(false)
    }
  }

  return (
    <ScrollView
      style={styles.container}
      refreshControl={<RefreshControl refreshing={loading} onRefresh={() => { setLoading(true); void load() }} />}
    >
      <Text style={styles.title}>Claimable Containers</Text>
      <Text style={styles.subtitle}>Claim tagged containers and assign drivers.</Text>
      {error ? <Text style={styles.error}>{error}</Text> : null}

      {!loading && containers.length === 0 ? <Text style={styles.empty}>No claimable containers right now.</Text> : null}

      {containers.map((container) => (
        <View key={container.uuid} style={styles.card}>
          <Text style={styles.number}>{container.containerNumber}</Text>
          <Text style={styles.meta}>Entry: {container.entryReference}</Text>
          <Text style={styles.status}>{container.status}</Text>
          <View style={styles.actions}>
            <Pressable style={styles.primaryBtn} disabled={busy} onPress={() => handleClaim(container.uuid)}>
              <Text style={styles.primaryText}>Claim</Text>
            </Pressable>
            <Pressable style={styles.outlineBtn} disabled={busy} onPress={() => setAssignTarget(container.uuid)}>
              <Text style={styles.outlineText}>Assign driver</Text>
            </Pressable>
          </View>
        </View>
      ))}

      {assignTarget ? (
        <View style={styles.assignBox}>
          <Text style={styles.assignTitle}>Assign driver UUID</Text>
          <TextInput
            style={styles.input}
            value={driverUuid}
            onChangeText={setDriverUuid}
            placeholder="Driver user UUID"
            autoCapitalize="none"
          />
          <View style={styles.actions}>
            <Pressable style={styles.primaryBtn} disabled={busy || !driverUuid.trim()} onPress={handleAssign}>
              {busy ? <ActivityIndicator color="#fff" /> : <Text style={styles.primaryText}>Confirm</Text>}
            </Pressable>
            <Pressable style={styles.outlineBtn} disabled={busy} onPress={() => { setAssignTarget(null); setDriverUuid('') }}>
              <Text style={styles.outlineText}>Cancel</Text>
            </Pressable>
          </View>
        </View>
      ) : null}
    </ScrollView>
  )
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.bg, padding: 16 },
  title: { fontSize: 24, fontWeight: '800', color: colors.text },
  subtitle: { color: colors.muted, marginBottom: 12, marginTop: 4 },
  error: { color: colors.danger, marginBottom: 12 },
  empty: { color: colors.muted },
  card: {
    backgroundColor: colors.card,
    borderRadius: 12,
    padding: 16,
    marginBottom: 12,
    borderWidth: 1,
    borderColor: colors.border,
  },
  number: { fontSize: 18, fontWeight: '700', color: colors.text },
  meta: { color: colors.muted, marginTop: 4, fontSize: 13 },
  status: { marginTop: 8, fontWeight: '700', color: colors.primary },
  actions: { flexDirection: 'row', gap: 8, marginTop: 12, flexWrap: 'wrap' },
  primaryBtn: { backgroundColor: colors.primary, paddingHorizontal: 14, paddingVertical: 10, borderRadius: 8 },
  primaryText: { color: '#fff', fontWeight: '700' },
  outlineBtn: { borderWidth: 1, borderColor: colors.primary, paddingHorizontal: 14, paddingVertical: 10, borderRadius: 8 },
  outlineText: { color: colors.primary, fontWeight: '700' },
  assignBox: {
    marginTop: 8,
    padding: 16,
    borderRadius: 12,
    backgroundColor: colors.card,
    borderWidth: 1,
    borderColor: colors.border,
  },
  assignTitle: { fontWeight: '700', marginBottom: 8, color: colors.text },
  input: {
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: 10,
    marginBottom: 12,
    backgroundColor: '#fff',
  },
})
