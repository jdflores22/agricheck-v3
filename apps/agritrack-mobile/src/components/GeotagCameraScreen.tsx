import { CameraView, useCameraPermissions } from 'expo-camera'
import * as Location from 'expo-location'
import { useCallback, useEffect, useRef, useState } from 'react'
import { ActivityIndicator, Pressable, StyleSheet, Text, View } from 'react-native'
import { colors, radius } from '../theme'

type Props = {
  title: string
  hint: string
  onCancel: () => void
  onCaptured: (jpegUri: string, latitude: number, longitude: number, capturedAtIso: string) => void
}

export function GeotagCameraScreen({ title, hint, onCancel, onCaptured }: Props) {
  const [permission, requestPermission] = useCameraPermissions()
  const [coords, setCoords] = useState<{ latitude: number; longitude: number } | null>(null)
  const [capturedAt, setCapturedAt] = useState(() => new Date().toISOString())
  const [busy, setBusy] = useState(false)
  const cameraRef = useRef<CameraView | null>(null)

  useEffect(() => {
    const tick = setInterval(() => setCapturedAt(new Date().toISOString()), 1000)
    return () => clearInterval(tick)
  }, [])

  useEffect(() => {
    void (async () => {
      const locPerm = await Location.requestForegroundPermissionsAsync()
      if (!locPerm.granted) return
      const pos = await Location.getCurrentPositionAsync({ accuracy: Location.Accuracy.High })
      setCoords({ latitude: pos.coords.latitude, longitude: pos.coords.longitude })
    })()
  }, [])

  const capture = useCallback(async () => {
    if (!coords || busy) return
    setBusy(true)
    try {
      const photo = await cameraRef.current?.takePictureAsync({ quality: 0.9, skipProcessing: false })
      if (!photo?.uri) throw new Error('Capture failed')
      onCaptured(photo.uri, coords.latitude, coords.longitude, capturedAt)
    } catch (err) {
      console.warn(err)
    } finally {
      setBusy(false)
    }
  }, [busy, capturedAt, coords, onCaptured])

  if (!permission?.granted) {
    return (
      <View style={styles.root}>
        <Text style={styles.title}>{title}</Text>
        <Text style={styles.hint}>{hint}</Text>
        <Pressable style={styles.primary} onPress={() => void requestPermission()}>
          <Text style={styles.primaryText}>Allow camera</Text>
        </Pressable>
        <Pressable style={styles.secondary} onPress={onCancel}>
          <Text style={styles.secondaryText}>Cancel</Text>
        </Pressable>
      </View>
    )
  }

  return (
    <View style={styles.root}>
      <CameraView ref={cameraRef} style={styles.camera} facing="back">
        <View style={styles.top}>
          <Text style={styles.title}>{title}</Text>
          <Text style={styles.hint}>{hint}</Text>
        </View>
        {coords ? null : (
          <View style={styles.gpsWait}>
            <Text style={styles.stampLine}>Getting GPS…</Text>
          </View>
        )}
        <View style={styles.actions}>
          <Pressable style={styles.secondary} onPress={onCancel}>
            <Text style={styles.secondaryText}>Cancel</Text>
          </Pressable>
          <Pressable
            style={[styles.primary, (!coords || busy) && styles.primaryDisabled]}
            disabled={!coords || busy}
            onPress={() => void capture()}
          >
            {busy ? <ActivityIndicator color="#fff" /> : <Text style={styles.primaryText}>Capture</Text>}
          </Pressable>
        </View>
      </CameraView>
    </View>
  )
}

const styles = StyleSheet.create({
  root: { flex: 1, backgroundColor: '#000' },
  camera: { flex: 1 },
  top: { paddingTop: 48, paddingHorizontal: 16, backgroundColor: 'rgba(0,0,0,0.55)' },
  title: { color: '#fff', fontWeight: '800', fontSize: 18 },
  hint: { color: 'rgba(255,255,255,0.9)', marginTop: 6, fontSize: 13 },
  gpsWait: {
    position: 'absolute',
    left: 16,
    right: 16,
    bottom: 100,
    backgroundColor: 'rgba(0,0,0,0.55)',
    padding: 12,
    borderRadius: 8,
  },
  stampLine: { color: '#fff', fontSize: 13, fontWeight: '600' },
  actions: {
    position: 'absolute',
    bottom: 0,
    left: 0,
    right: 0,
    flexDirection: 'row',
    gap: 10,
    padding: 16,
    backgroundColor: 'rgba(0,0,0,0.65)',
  },
  primary: {
    flex: 1,
    backgroundColor: colors.primary,
    borderRadius: radius.sm,
    paddingVertical: 14,
    alignItems: 'center',
  },
  primaryDisabled: { opacity: 0.5 },
  primaryText: { color: '#fff', fontWeight: '800' },
  secondary: {
    flex: 1,
    borderWidth: 1,
    borderColor: '#fff',
    borderRadius: radius.sm,
    paddingVertical: 14,
    alignItems: 'center',
  },
  secondaryText: { color: '#fff', fontWeight: '700' },
})
