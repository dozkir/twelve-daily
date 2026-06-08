# Twelve Daily Front-end

Expo + Expo Router app.

## Prerequisites

- Node.js v20 (use fnm or nvm)
- npm

## Quick start

```bash
# Install dependencies
npm install

# Start Expo dev server
npx expo start
```

The app will be available at:
- Web: `http://localhost:8081` (press `w` to open)
- iOS Simulator: press `i`
- Android Emulator: press `a`
- Expo Go: scan QR code

## API client

The `@twelve-daily/api-client` package is linked locally from `../../packages/api-client`.

## Folder structure

```
app/               # Expo Router screens
  (auth)/          # Login, Register
  (app)/           # Main app (Timeline, Habits, Dashboard, Settings)
src/
  api/             # API client factory
  auth/            # Auth context and token storage
  ui/              # Reusable UI components
```
