import { useCallback, useEffect, useState } from 'react'
import { ActivityIndicator, RefreshControl, ScrollView, StyleSheet, Text, View } from 'react-native'
import { getDriverDashboard } from '../api/agritrackApi'
import type { DriverDashboard } from '../types/api'
import { colors } from '../theme'

export function DashboardScreen() {
  const [data, setData] = useState<DriverDashboard | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const load = useCallback(async () => {
    setError('')
    try {
      const dashboard = await getDriverDashboard()
      setData(dashboard)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load dashboard')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  if (loading && !data) {
    return (
      <View style={styles.centered}>
        <ActivityIndicator color={colors.primary} size="large" />
      </View>
    )
  }

  return (
    <ScrollView
      style={styles.container}
      refreshControl={<RefreshControl refreshing={loading} onRefresh={() => { setLoading(true); void load() }} />}
    >
      <Text style={styles.title}>Dashboard</Text>
      {error ? <Text style={styles.error}>{error}</Text> : null}
      <View style={styles.grid}>
        <StatCard label="Assigned" value={data?.assignedContainers ?? 0} />
        <StatCard label="In transit" value={data?.inTransitContainers ?? 0} />
        <StatCard label="Profile" value={`${data?.profileCompletion ?? 0}%`} />
        <StatCard label="Face verified" value={data?.faceVerified ? 'Yes' : 'No'} />
      </View>
    </ScrollView>
  )
}

function StatCard({ label, value }: { label: string; value: string | number }) {
  return (
    <View style={styles.card}>
      <Text style={styles.cardLabel}>{label}</Text>
      <Text style={styles.cardValue}>{value}</Text>
    </View>
  )
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.bg, padding: 16 },
  centered: { flex: 1, alignItems: 'center', justifyContent: 'center', backgroundColor: colors.bg },
  title: { fontSize: 24, fontWeight: '800', color: colors.text, marginBottom: 16 },
  error: { color: colors.danger, marginBottom: 12 },
  grid: { flexDirection: 'row', flexWrap: 'wrap', gap: 12 },
  card: {
    width: '47%',
    backgroundColor: colors.card,
    borderRadius: 12,
    padding: 16,
    borderWidth: 1,
    borderColor: colors.border,
  },
  cardLabel: { color: colors.muted, fontSize: 13, marginBottom: 8 },
  cardValue: { color: colors.primary, fontSize: 28, fontWeight: '800' },
})
