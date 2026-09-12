export default {
  expo: {
    name: 'AgriTrack',
    slug: 'agritrack-mobile',
    version: '1.0.0',
    orientation: 'portrait',
    icon: './assets/icon.png',
    userInterfaceStyle: 'light',
    extra: {
      apiBaseUrl: process.env.EXPO_PUBLIC_API_URL ?? 'http://10.0.2.2:5000',
    },
    plugins: [
      [
        'expo-location',
        {
          locationAlwaysAndWhenInUsePermission:
            'AgriTrack records GPS in the background while a container is in transit.',
          isAndroidBackgroundLocationEnabled: true,
          isAndroidForegroundServiceEnabled: true,
        },
      ],
      [
        'expo-notifications',
        {
          icon: './assets/icon.png',
          color: '#166534',
        },
      ],
    ],
    ios: {
      supportsTablet: true,
      infoPlist: {
        NSLocationWhenInUseUsageDescription: 'AgriTrack records container GPS location during transport.',
        NSLocationAlwaysAndWhenInUseUsageDescription:
          'AgriTrack continues GPS tracking in the background while a container is in transit.',
        UIBackgroundModes: ['location', 'remote-notification'],
      },
    },
    android: {
      adaptiveIcon: {
        backgroundColor: '#166534',
        foregroundImage: './assets/android-icon-foreground.png',
        backgroundImage: './assets/android-icon-background.png',
        monochromeImage: './assets/android-icon-monochrome.png',
      },
      permissions: [
        'ACCESS_COARSE_LOCATION',
        'ACCESS_FINE_LOCATION',
        'ACCESS_BACKGROUND_LOCATION',
        'FOREGROUND_SERVICE',
        'FOREGROUND_SERVICE_LOCATION',
        'POST_NOTIFICATIONS',
      ],
    },
    web: {
      favicon: './assets/favicon.png',
    },
  },
}
