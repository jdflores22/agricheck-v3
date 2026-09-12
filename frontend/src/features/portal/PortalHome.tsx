import { Card, CardContent, Typography } from '@mui/material'

interface PortalHomeProps {
  description: string
}

export function PortalHome({ description }: PortalHomeProps) {
  return (
    <Card>
      <CardContent>
        <Typography variant="body1" color="text.secondary">
          {description}
        </Typography>
        <Typography variant="body2" sx={{ mt: 2 }}>
          Phase P1 complete — portal modules will be built in Phase P2+.
        </Typography>
      </CardContent>
    </Card>
  )
}
