import { createTheme } from '@mui/material/styles'

export const agricheckTheme = createTheme({
  palette: {
    mode: 'light',
    primary: {
      main: '#166534',
      light: '#15803d',
      dark: '#14532d',
      contrastText: '#ffffff',
    },
    secondary: {
      main: '#8b7355',
      light: '#a89070',
      dark: '#6b5740',
      contrastText: '#ffffff',
    },
    background: {
      default: '#fafaf9',
      paper: '#ffffff',
    },
    text: {
      primary: '#1a1a1a',
      secondary: '#5c5c5c',
    },
    success: {
      main: '#2d5016',
    },
    warning: {
      main: '#b8860b',
    },
    error: {
      main: '#c0392b',
    },
  },
  typography: {
    fontFamily: '"IBM Plex Sans", system-ui, -apple-system, "Segoe UI", sans-serif',
    h1: { fontWeight: 700 },
    h2: { fontWeight: 700 },
    h3: { fontWeight: 600 },
    h4: { fontWeight: 600 },
    h5: { fontWeight: 600 },
    h6: { fontWeight: 600 },
  },
  shape: {
    borderRadius: 10,
  },
  components: {
    MuiButton: {
      styleOverrides: {
        root: {
          textTransform: 'none',
          fontWeight: 600,
        },
      },
    },
    MuiCard: {
      styleOverrides: {
        root: {
          boxShadow: '0 1px 3px rgba(45, 80, 22, 0.08)',
        },
      },
    },
  },
})
