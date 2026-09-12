import ArticleOutlinedIcon from '@mui/icons-material/ArticleOutlined'
import BusinessOutlinedIcon from '@mui/icons-material/BusinessOutlined'
import HistoryOutlinedIcon from '@mui/icons-material/HistoryOutlined'
import PeopleOutlinedIcon from '@mui/icons-material/PeopleOutlined'
import VerifiedOutlinedIcon from '@mui/icons-material/VerifiedOutlined'
import { Box, LinearProgress, Typography } from '@mui/material'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { PortalQuickAction } from '../../../components/portal/PortalQuickAction'
import { PortalStatCard } from '../../../components/portal/PortalStatCard'
import { portalColors } from '../../../components/portal/portalTheme'
import { useGetAdminDashboardQuery } from '../api/adminApi'

function ProgressRow({ label, value, total, color = portalColors.primary }: {
  label: string
  value: number
  total: number
  color?: string
}) {
  const pct = total > 0 ? Math.round((value / total) * 100) : 0

  return (
    <Box>
      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 1 }}>
        <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted }}>{label}</Typography>
        <Typography sx={{ fontSize: '0.875rem', fontWeight: 500, fontVariantNumeric: 'tabular-nums' }}>
          {value}
        </Typography>
      </Box>
      <LinearProgress
        variant="determinate"
        value={pct}
        aria-label={label}
        sx={{
          height: 6,
          borderRadius: 999,
          bgcolor: portalColors.bgMuted,
          '& .MuiLinearProgress-bar': { borderRadius: 999, bgcolor: color },
        }}
      />
    </Box>
  )
}

export function AdminDashboardPage() {
  const { data, isLoading } = useGetAdminDashboardQuery()
  const stats = data?.data
  const today = new Date().toLocaleDateString(undefined, { month: 'long', day: 'numeric', year: 'numeric' })

  const display = (value?: number) => (isLoading ? '…' : (value ?? 0))

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Overview"
        title="Admin Dashboard"
        subtitle={`System activity for ${today}`}
        action={{ label: 'Audit Logs', to: '/admin/audit-logs' }}
      />

      <Box
        sx={{
          mb: 3,
          display: 'grid',
          gap: 2,
          gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr', xl: 'repeat(4, 1fr)' },
        }}
      >
        <PortalStatCard
          label="Total Users"
          value={display(stats?.totalUsers)}
          caption={`${display(stats?.activeUsers)} active`}
        />
        <PortalStatCard
          label="Total Entries"
          value={display(stats?.totalEntries)}
          caption="Across all agencies"
        />
        <PortalStatCard
          label="Agencies"
          value={display(stats?.totalAgencies)}
          caption="Registered agencies"
        />
        <PortalStatCard
          label="Certificates"
          value={display(stats?.totalCertificates)}
          caption={`${display(stats?.activeCertificates)} active`}
        />
      </Box>

      <Box
        sx={{
          mb: 3,
          display: 'grid',
          gap: 2,
          gridTemplateColumns: { xs: '1fr', lg: '2fr 1fr' },
        }}
      >
        <PortalPanel title="Platform resources" action={{ label: 'Manage forms', to: '/admin/forms' }}>
          <Box
            sx={{
              display: 'grid',
              gridTemplateColumns: { xs: '1fr 1fr', md: 'repeat(3, 1fr)' },
              gap: '1px',
              bgcolor: portalColors.border,
            }}
          >
            {[
              { label: 'Form Templates', value: display(stats?.formTemplates) },
              { label: 'Cert Templates', value: display(stats?.certificateTemplates) },
              { label: 'Recent Audit Logs', value: display(stats?.recentAuditLogs) },
            ].map((item) => (
              <Box key={item.label} sx={{ bgcolor: portalColors.bgWhite, px: 2.5, py: 2.5 }}>
                <Typography
                  sx={{
                    fontSize: '1.5rem',
                    fontWeight: 600,
                    fontVariantNumeric: 'tabular-nums',
                    color: portalColors.textDark,
                  }}
                >
                  {item.value}
                </Typography>
                <Typography sx={{ mt: 0.5, fontSize: '0.875rem', color: portalColors.textMuted }}>
                  {item.label}
                </Typography>
              </Box>
            ))}
          </Box>
        </PortalPanel>

        <PortalPanel title="User overview" action={{ label: 'Manage', to: '/admin/users' }}>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2.5, px: 2.5, py: 2.5 }}>
            <ProgressRow label="Total" value={stats?.totalUsers ?? 0} total={stats?.totalUsers || 1} />
            <ProgressRow label="Active" value={stats?.activeUsers ?? 0} total={stats?.totalUsers ?? 0} />
            <ProgressRow
              label="Inactive"
              value={(stats?.totalUsers ?? 0) - (stats?.activeUsers ?? 0)}
              total={stats?.totalUsers ?? 0}
              color="#a8a29e"
            />
          </Box>
        </PortalPanel>
      </Box>

      <PortalPanel title="Quick actions">
        <Box
          sx={{
            display: 'grid',
            gap: 1.5,
            p: 2.5,
            gridTemplateColumns: { xs: '1fr 1fr', md: 'repeat(3, 1fr)', lg: 'repeat(5, 1fr)' },
          }}
        >
          <PortalQuickAction
            to="/admin/users"
            icon={<PeopleOutlinedIcon />}
            title="Users"
            caption="Manage accounts"
          />
          <PortalQuickAction
            to="/admin/agencies"
            icon={<BusinessOutlinedIcon />}
            title="Agencies"
            caption="Manage agencies"
          />
          <PortalQuickAction
            to="/admin/forms"
            icon={<ArticleOutlinedIcon />}
            title="Form Builder"
            caption="Edit templates"
          />
          <PortalQuickAction
            to="/admin/certificates"
            icon={<VerifiedOutlinedIcon />}
            title="Certificates"
            caption="Issued certificates"
          />
          <PortalQuickAction
            to="/admin/audit-logs"
            icon={<HistoryOutlinedIcon />}
            title="Audit Logs"
            caption="Review activity"
          />
        </Box>
      </PortalPanel>
    </Box>
  )
}
