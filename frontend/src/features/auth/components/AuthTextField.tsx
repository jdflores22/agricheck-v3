import { TextField, TextFieldProps } from '@mui/material'
import { authFieldSx } from './authTheme'

export function AuthTextField(props: TextFieldProps) {
  return (
    <TextField
      fullWidth
      variant="outlined"
      slotProps={{ inputLabel: { shrink: true } }}
      sx={authFieldSx}
      {...props}
    />
  )
}
