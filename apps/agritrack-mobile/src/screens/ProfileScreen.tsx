import { useEffect, useState } from 'react'
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native'
import { getDriverProfile, updateDriverProfile } from '../api/agritrackApi'
import type { DriverProfile } from '../types/api'
import { useAuth } from '../context/AuthContext'
import { colors } from '../theme'

export function ProfileScreen({ onOpenSettings }: { onOpenSettings?: () => void }) {
  const { user, logout } = useAuth()
  const [profile, setProfile] = useState<DriverProfile | null>(null)
  const [phoneNumber, setPhoneNumber] = useState('')
  const [vehicleType, setVehicleType] = useState('')
  const [vehicleRegistration, setVehicleRegistration] = useState('')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [message, setMessage] = useState('')

  useEffect(() => {
    void (async () => {
      try {
        const data = await getDriverProfile()
        setProfile(data)
        setPhoneNumber(data.phoneNumber ?? '')
        setVehicleType(data.vehicleType ?? '')
        setVehicleRegistration(data.vehicleRegistration ?? '')
      } finally {
        setLoading(false)
      }
    })()
  }, [])

  const handleSave = async () => {
    setSaving(true)
    setMessage('')
    try {
      const updated = await updateDriverProfile({ phoneNumber, vehicleType, vehicleRegistration })
      setProfile(updated)
      setMessage('Profile saved.')
    } catch (err) {
      setMessage(err instanceof Error ? err.message : 'Save failed')
    } finally {
      setSaving(false)
    }
  }

  if (loading) {
    return (
      <View style={styles.centered}>
        <ActivityIndicator color={colors.primary} size="large" />
      </View>
    )
  }

  return (
    <ScrollView style={styles.container}>
      <Text style={styles.title}>Profile</Text>
      <Text style={styles.meta}>{user?.firstName} {user?.lastName}</Text>
      <Text style={styles.meta}>{user?.email}</Text>
      <Text style={styles.meta}>Completion: {profile?.completionPercentage ?? 0}%</Text>

      <TextInput style={styles.input} placeholder="Phone" value={phoneNumber} onChangeText={setPhoneNumber} />
      <TextInput style={styles.input} placeholder="Vehicle type" value={vehicleType} onChangeText={setVehicleType} />
      <TextInput
        style={styles.input}
        placeholder="Vehicle registration"
        value={vehicleRegistration}
        onChangeText={setVehicleRegistration}
      />

      {message ? <Text style={styles.message}>{message}</Text> : null}

      <Pressable style={styles.primaryBtn} onPress={handleSave} disabled={saving}>
        {saving ? <ActivityIndicator color="#fff" /> : <Text style={styles.primaryText}>Save profile</Text>}
      </Pressable>

      <Pressable style={styles.linkBtn} onPress={() => onOpenSettings?.()}>
        <Text style={styles.linkText}>API settings</Text>
      </Pressable>

      <Pressable style={styles.logoutBtn} onPress={() => void logout()}>
        <Text style={styles.logoutText}>Sign out</Text>
      </Pressable>
    </ScrollView>
  )
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.bg, padding: 16 },
  centered: { flex: 1, alignItems: 'center', justifyContent: 'center', backgroundColor: colors.bg },
  title: { fontSize: 24, fontWeight: '800', color: colors.text, marginBottom: 8 },
  meta: { color: colors.muted, marginBottom: 4 },
  input: {
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 10,
    paddingHorizontal: 12,
    paddingVertical: 10,
    marginTop: 12,
    backgroundColor: '#fff',
  },
  message: { marginTop: 12, color: colors.success },
  primaryBtn: {
    marginTop: 16,
    backgroundColor: colors.primary,
    borderRadius: 10,
    paddingVertical: 14,
    alignItems: 'center',
  },
  primaryText: { color: '#fff', fontWeight: '700' },
  linkBtn: { marginTop: 12, alignItems: 'center' },
  linkText: { color: colors.primary, fontWeight: '600' },
  logoutBtn: { marginTop: 24, alignItems: 'center' },
  logoutText: { color: colors.danger, fontWeight: '700' },
})
