# AgriTrack Mobile (AgriCheck V3)

> **Native Android (recommended):** [`../android-agritrack`](../android-agritrack) — Kotlin + Jetpack Compose, same stack as ECMS `android-trucker`.

Expo React Native app for **drivers** and **operators**, connected to the AgriCheck V3 mobile API.



## Features



- JWT login via `POST /api/mobile/auth/login`

- Token refresh via `POST /api/v1/auth/refresh`

- Driver: assigned containers, status updates, real GPS capture

- **Live map (OSRM)**: route + GPS trail to warehouse destination

- **Background GPS**: auto-upload every ~60s while status is `InTransit`

- **Push notifications**: Expo push + local fallback when operator assigns a driver

- Operator: claim containers, assign driver

- Driver profile view/update

- Configurable API base URL (emulator vs physical device)



## Prerequisites



- Node.js 20+

- AgriCheck V3 API running on port **5000**

- Expo Go app (phone) or Android emulator



## Start the API (listen on LAN for physical devices)



```powershell

cd c:\xampp\htdocs\agricheck-v3\backend\src\AgriCheck.Api

dotnet run --urls http://0.0.0.0:5000

```



## Run the app



```powershell

cd c:\xampp\htdocs\agricheck-v3\apps\agritrack-mobile

npm start

```



Press `a` for Android emulator, or scan the QR code with Expo Go.



## API URL defaults



| Environment | Base URL |

|-------------|----------|

| Android emulator | `http://10.0.2.2:5000` |

| iOS simulator | `http://localhost:5000` |

| Physical phone | `http://<your-pc-lan-ip>:5000` |



Change this from the login screen → **Change** (Settings).



## Demo accounts



| Role | Email | Password |

|------|-------|----------|

| Driver | `driver@agricheck.local` | `Driver@12345` |

| Operator | `operator@agricheck.local` | `Operator@12345` |



## How to test the new features



1. **Live map** — Log in as driver → **My Loads** → tap **Live Map** on an assigned container. Blue line = recorded GPS trail; green line = OSRM driving route to warehouse.

2. **Background GPS** — Tap **In Transit** (allow background location). A foreground notification appears on Android while GPS uploads every minute.

3. **Push / assignment alert** — Log in as operator, assign a container to the driver. Driver gets Expo push (if token registered) plus a local notification via 60s sync polling fallback.



## Mobile API endpoints used



| Method | Path |

|--------|------|

| GET | `/api/mobile/health` |

| POST | `/api/mobile/auth/login` |

| GET | `/api/mobile/containers/assigned` |

| PUT | `/api/mobile/containers/{uuid}/status` |

| POST | `/api/mobile/containers/{uuid}/location` |

| GET | `/api/mobile/containers/{uuid}/track` |

| POST/DELETE | `/api/mobile/push/register` |

| GET | `/api/mobile/sync/pull` |

| GET | `/api/mobile/operator/containers/claimable` |

| POST | `/api/mobile/operator/containers/{uuid}/claim` |

| POST | `/api/mobile/operator/containers/{uuid}/assign-driver` |

| GET/PUT | `/api/v1/ops/driver/profile` |

| GET | `/api/v1/ops/driver/dashboard` |



## Notes



- OSRM uses the public `router.project-osrm.org` service (requires internet on the device).

- Remote push in Expo Go works for development; production builds need an EAS `projectId` in app config.

- Background GPS requires **Allow all the time** location on Android and a development build for full background behavior in some Expo versions.


