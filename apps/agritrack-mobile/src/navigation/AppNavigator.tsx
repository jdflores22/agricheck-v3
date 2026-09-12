import { createBottomTabNavigator } from '@react-navigation/bottom-tabs'
import { NavigationContainer } from '@react-navigation/native'
import { createNativeStackNavigator } from '@react-navigation/native-stack'
import { ActivityIndicator, View } from 'react-native'
import { useAuth } from '../context/AuthContext'
import { ContainersScreen, type DriverStackParamList } from '../screens/ContainersScreen'
import { DashboardScreen } from '../screens/DashboardScreen'
import { LoginScreen } from '../screens/LoginScreen'
import { MapScreen } from '../screens/MapScreen'
import { OperatorScreen } from '../screens/OperatorScreen'
import { ProfileScreen } from '../screens/ProfileScreen'
import { SettingsScreen } from '../screens/SettingsScreen'
import { colors } from '../theme'

const Stack = createNativeStackNavigator()
const Tabs = createBottomTabNavigator()
const DriverStack = createNativeStackNavigator<DriverStackParamList>()

function DriverContainersStack() {
  return (
    <DriverStack.Navigator
      screenOptions={{
        headerStyle: { backgroundColor: colors.primary },
        headerTintColor: '#fff',
      }}
    >
      <DriverStack.Screen name="ContainersList" component={ContainersScreen} options={{ title: 'My Loads' }} />
      <DriverStack.Screen name="Map" options={{ title: 'Live Map' }}>
        {({ route }) => (
          <MapScreen
            containerUuid={route.params.containerUuid}
            containerNumber={route.params.containerNumber}
          />
        )}
      </DriverStack.Screen>
    </DriverStack.Navigator>
  )
}

function MainTabs() {
  const { isOperator, isDriver } = useAuth()

  return (
    <Tabs.Navigator
      screenOptions={{
        headerStyle: { backgroundColor: colors.primary },
        headerTintColor: '#fff',
        tabBarActiveTintColor: colors.primary,
      }}
    >
      <Tabs.Screen name="Dashboard" component={DashboardScreen} />
      {isDriver ? (
        <Tabs.Screen
          name="Containers"
          component={DriverContainersStack}
          options={{ title: 'My Loads', headerShown: false }}
        />
      ) : null}
      {isOperator ? <Tabs.Screen name="Operator" component={OperatorScreen} options={{ title: 'Claim' }} /> : null}
      <Tabs.Screen name="Profile" options={{ title: 'Profile' }}>
        {({ navigation }) => <ProfileScreen onOpenSettings={() => navigation.navigate('Settings')} />}
      </Tabs.Screen>
    </Tabs.Navigator>
  )
}

export function AppNavigator() {
  const { user, loading } = useAuth()

  if (loading) {
    return (
      <View style={{ flex: 1, alignItems: 'center', justifyContent: 'center', backgroundColor: colors.bg }}>
        <ActivityIndicator color={colors.primary} size="large" />
      </View>
    )
  }

  return (
    <NavigationContainer>
      <Stack.Navigator screenOptions={{ headerShown: false }}>
        {!user ? (
          <>
            <Stack.Screen name="Login">
              {({ navigation }) => (
                <LoginScreen onOpenSettings={() => navigation.navigate('Settings')} />
              )}
            </Stack.Screen>
            <Stack.Screen name="Settings" options={{ headerShown: true, title: 'Settings' }}>
              {({ navigation }) => <SettingsScreen onBack={() => navigation.goBack()} />}
            </Stack.Screen>
          </>
        ) : (
          <>
            <Stack.Screen name="Main" component={MainTabs} />
            <Stack.Screen name="Settings" options={{ headerShown: true, title: 'Settings' }}>
              {({ navigation }) => <SettingsScreen onBack={() => navigation.goBack()} />}
            </Stack.Screen>
          </>
        )}
      </Stack.Navigator>
    </NavigationContainer>
  )
}
