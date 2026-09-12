import { useNavigation } from '@react-navigation/native'
import type { NativeStackNavigationProp } from '@react-navigation/native-stack'
import * as Location from 'expo-location'
import { useCallback, useEffect, useState } from 'react'
import {
  ActivityIndicator,
  Alert,
  Pressable,
  RefreshControl,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from 'react-native'
import {
  getAssignedContainers,
  recordContainerLocation,
  syncPull,
  updateContainerStatus,
} from '../api/agritrackApi'
import { getActiveTrackingContainerUuid, startBackgroundTracking, stopBackgroundTracking } from '../services/backgroundTracking'
import type { ContainerListItem } from '../types/api'
import { colors } from '../theme'

export type DriverStackParamList = {
  ContainersList: undefined
  Map: { containerUuid: string; containerNumber: string }
}

export function ContainersScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<DriverStackParamList>>()
  const [containers, setContainers] = useState<ContainerListItem[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [busyUuid, setBusyUuid] = useState<string | null>(null)

  const load = useCallback(async () => {
    setError('')
    try {
      const data = await getAssignedContainers()
      setContainers(data)
    } catch (err) {
      try {
        const pull = await syncPull()
        setContainers(pull.containers)
      } catch {
        setError(err instanceof Error ? err.message : 'Failed to load containers')
      }
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  const runAction = async (uuid: string, action: () => Promise<unknown>) => {
    setBusyUuid(uuid)
    try {
      await action()
      await load()
    } catch (err) {
      Alert.alert('Action failed', err instanceof Error ? err.message : 'Unknown error')
    } finally {
      setBusyUuid(null)
    }
  }

  const handleGps = async (uuid: string) => {
    const permission = await Location.requestForegroundPermissionsAsync()
    if (!permission.granted) {
      Alert.alert('Location required', 'Allow location access to record GPS.')
      return
    }
    const position = await Location.getCurrentPositionAsync({ accuracy: Location.Accuracy.Balanced })
    await recordContainerLocation(uuid, position.coords.latitude, position.coords.longitude)
  }

  const handleInTransit = async (container: ContainerListItem) => {
    await startBackgroundTracking(container.uuid)
    await updateContainerStatus(container.uuid, 'InTransit')
  }

  const handleAtWarehouse = async (container: ContainerListItem) => {
    await updateContainerStatus(container.uuid, 'AtWarehouse')
    const activeUuid = await getActiveTrackingContainerUuid()
    if (activeUuid === container.uuid) {
      await stopBackgroundTracking()
    }
  }

  return (
    <ScrollView
      style={styles.container}
      refreshControl={<RefreshControl refreshing={loading} onRefresh={() => { setLoading(true); void load() }} />}
    >
      <Text style={styles.title}>My Containers</Text>
      {error ? <Text style={styles.error}>{error}</Text> : null}

      {!loading && containers.length === 0 ? (
        <Text style={styles.empty}>No assigned containers.</Text>
      ) : null}

      {containers.map((container) => (
        <View key={container.uuid} style={styles.card}>
          <Text style={styles.number}>{container.containerNumber}</Text>
          <Text style={styles.meta}>Entry: {container.entryReference}</Text>
          <Text style={styles.status}>{container.status}</Text>
          {container.lastLatitude != null ? (
            <Text style={styles.meta}>
              Last GPS: {container.lastLatitude.toFixed(5)}, {container.lastLongitude?.toFixed(5)}
            </Text>
          ) : null}

          <View style={styles.actions}>
            <ActionButton
              label="In Transit"
              disabled={busyUuid === container.uuid}
              onPress={() =>
                runAction(container.uuid, async () => {
                  try {
                    await handleInTransit(container)
                  } catch (err) {
                    await stopBackgroundTracking()
                    throw err
                  }
                })
              }
            />
            <ActionButton
              label="At Warehouse"
              disabled={busyUuid === container.uuid}
              onPress={() => runAction(container.uuid, () => handleAtWarehouse(container))}
            />
            <ActionButton
              label="GPS"
              disabled={busyUuid === container.uuid}
              onPress={() => runAction(container.uuid, () => handleGps(container.uuid))}
            />
            <ActionButton
              label="Live Map"
              disabled={busyUuid === container.uuid}
              onPress={() =>
                navigation.navigate('Map', {
                  containerUuid: container.uuid,
                  containerNumber: container.containerNumber,
                })
              }
            />
          </View>
          {busyUuid === container.uuid ? <ActivityIndicator color={colors.primary} style={{ marginTop: 8 }} /> : null}
        </View>
      ))}
    </ScrollView>
  )
}

function ActionButton({ label, onPress, disabled }: { label: string; onPress: () => void; disabled?: boolean }) {
  return (
    <Pressable style={[styles.actionBtn, disabled && styles.actionBtnDisabled]} onPress={onPress} disabled={disabled}>
      <Text style={styles.actionText}>{label}</Text>
    </Pressable>
  )
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.bg, padding: 16 },
  title: { fontSize: 24, fontWeight: '800', color: colors.text, marginBottom: 12 },
  error: { color: colors.danger, marginBottom: 12 },
  empty: { color: colors.muted, fontSize: 15 },
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
  actions: { flexDirection: 'row', flexWrap: 'wrap', gap: 8, marginTop: 12 },
  actionBtn: {
    backgroundColor: colors.primary,
    paddingHorizontal: 12,
    paddingVertical: 8,
    borderRadius: 8,
  },
  actionBtnDisabled: { opacity: 0.6 },
  actionText: { color: '#fff', fontWeight: '600', fontSize: 13 },
})
