import { CameraView, useCameraPermissions } from 'expo-camera'
import { useState } from 'react'
import { ActivityIndicator, Alert, Pressable, StyleSheet, Text, View } from 'react-native'
import { claimContainerByQr } from '../api/agritrackApi'
import { colors } from '../theme'

type OperatorQrScanScreenProps = {
  onClose: () => void
  onClaimed: () => void
}

export function OperatorQrScanScreen({ onClose, onClaimed }: OperatorQrScanScreenProps) {
  const [permission, requestPermission] = useCameraPermissions()
  const [busy, setBusy] = useState(false)
  const [scanned, setScanned] = useState(false)

  const handleScan = async (qrData: string) => {
    if (busy || scanned) return
    setScanned(true)
    setBusy(true)
    try {
      const container = await claimContainerByQr(qrData)
      Alert.alert('Container claimed', `${container.containerNumber} is now assigned to you.`, [
        {
          text: 'OK',
          onPress: () => {
            onClaimed()
            onClose()
          },
        },
      ])
    } catch (err) {
      setScanned(false)
      Alert.alert('Scan failed', err instanceof Error ? err.message : 'Unable to claim container from QR.')
    } finally {
      setBusy(false)
    }
  }

  if (!permission) {
    return (
      <View style={styles.center}>
        <ActivityIndicator color={colors.primary} />
      </View>
    )
  }

  if (!permission.granted) {
    return (
      <View style={styles.center}>
        <Text style={styles.title}>Camera permission required</Text>
        <Text style={styles.subtitle}>Allow camera access to scan transport QR codes.</Text>
        <Pressable style={styles.primaryBtn} onPress={() => void requestPermission()}>
          <Text style={styles.primaryText}>Grant permission</Text>
        </Pressable>
        <Pressable style={styles.outlineBtn} onPress={onClose}>
          <Text style={styles.outlineText}>Cancel</Text>
        </Pressable>
      </View>
    )
  }

  return (
    <View style={styles.container}>
      <CameraView
        style={StyleSheet.absoluteFillObject}
        facing="back"
        barcodeScannerSettings={{ barcodeTypes: ['qr'] }}
        onBarcodeScanned={({ data }) => {
          void handleScan(data)
        }}
      />
      <View style={styles.overlay}>
        <Text style={styles.overlayTitle}>Scan transport QR</Text>
        <Text style={styles.overlaySubtitle}>Point the camera at the QR generated after transport tagging.</Text>
        {busy ? <ActivityIndicator color="#fff" style={{ marginTop: 12 }} /> : null}
        <Pressable style={styles.cancelBtn} disabled={busy} onPress={onClose}>
          <Text style={styles.cancelText}>Cancel</Text>
        </Pressable>
      </View>
    </View>
  )
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#000' },
  center: {
    flex: 1,
    backgroundColor: colors.bg,
    alignItems: 'center',
    justifyContent: 'center',
    padding: 24,
    gap: 12,
  },
  title: { fontSize: 20, fontWeight: '800', color: colors.text, textAlign: 'center' },
  subtitle: { color: colors.muted, textAlign: 'center', marginBottom: 8 },
  primaryBtn: { backgroundColor: colors.primary, paddingHorizontal: 16, paddingVertical: 12, borderRadius: 8 },
  primaryText: { color: '#fff', fontWeight: '700' },
  outlineBtn: { borderWidth: 1, borderColor: colors.primary, paddingHorizontal: 16, paddingVertical: 12, borderRadius: 8 },
  outlineText: { color: colors.primary, fontWeight: '700' },
  overlay: {
    position: 'absolute',
    left: 0,
    right: 0,
    bottom: 0,
    padding: 20,
    backgroundColor: 'rgba(0,0,0,0.65)',
  },
  overlayTitle: { color: '#fff', fontSize: 18, fontWeight: '800' },
  overlaySubtitle: { color: '#e5e7eb', marginTop: 6, fontSize: 13 },
  cancelBtn: {
    marginTop: 16,
    alignSelf: 'flex-start',
    borderWidth: 1,
    borderColor: '#fff',
    borderRadius: 8,
    paddingHorizontal: 14,
    paddingVertical: 10,
  },
  cancelText: { color: '#fff', fontWeight: '700' },
})
