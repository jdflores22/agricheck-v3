import ArticleOutlinedIcon from '@mui/icons-material/ArticleOutlined'
import AssignmentOutlinedIcon from '@mui/icons-material/AssignmentOutlined'
import BarChartOutlinedIcon from '@mui/icons-material/BarChartOutlined'
import BusinessOutlinedIcon from '@mui/icons-material/BusinessOutlined'
import CheckCircleOutlinedIcon from '@mui/icons-material/CheckCircleOutlined'
import CreditCardOutlinedIcon from '@mui/icons-material/CreditCardOutlined'
import GridViewOutlinedIcon from '@mui/icons-material/GridViewOutlined'
import Inventory2OutlinedIcon from '@mui/icons-material/Inventory2Outlined'
import LockOutlinedIcon from '@mui/icons-material/LockOutlined'
import PersonOutlineOutlinedIcon from '@mui/icons-material/PersonOutlineOutlined'
import ReceiptLongOutlinedIcon from '@mui/icons-material/ReceiptLongOutlined'
import ReportProblemOutlinedIcon from '@mui/icons-material/ReportProblemOutlined'
import ScheduleOutlinedIcon from '@mui/icons-material/ScheduleOutlined'
import ShieldOutlinedIcon from '@mui/icons-material/ShieldOutlined'
import VerifiedOutlinedIcon from '@mui/icons-material/VerifiedOutlined'
import WarningAmberOutlinedIcon from '@mui/icons-material/WarningAmberOutlined'
import {
  Box,
  Button,
  Chip,
  LinearProgress,
  Skeleton,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material'
import type { ReactNode } from 'react'
import { useSelector } from 'react-redux'
import { Link as RouterLink } from 'react-router-dom'
import { selectCurrentUser } from '../../auth/authSlice'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { getStatusBadgeStyle } from '../../../components/portal/portalUtils'
import { portalEmptyStateSx, portalPrimaryButtonSx, portalTableHeadCellSx } from '../../../components/portal/portalStyles'
import { resolveAgencyLogoUrl } from '../../admin/components/adminAgencyUtils'
import { ClientDashboardStatCard } from '../components/ClientDashboardStatCard'
import { useGetDashboardQuery } from '../api/clientApi'
import type { ClientDashboardAccreditation, ClientDashboardLogisticsStats } from '../api/clientApi'
import { resolveDashboardAccreditation } from '../optimisticAccreditation'
import { isResubmittedForReview, resolveAccreditationListItemStatus } from '../../accreditation/accreditationStatusUtils'

function formatCurrency(amount: number) {
  return new Intl.NumberFormat('en-PH', { style: 'currency', currency: 'PHP' }).format(amount)
}

function formatStatusLabel(status?: string) {
  if (!status) return 'Unknown'
  return status.replace(/([A-Z])/g, ' $1').replace(/_/g, ' ').trim()
}

function statValue(isLoading: boolean, value?: number) {
  return isLoading ? '…' : (value ?? 0)
}

const sectionHeadSx = {
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'space-between',
  gap: 1.5,
  mb: 1.1,
} as const

const sectionTitleSx = {
  m: 0,
  fontSize: '0.875rem',
  fontWeight: 600,
  color: portalColors.textDark,
  display: 'flex',
  alignItems: 'center',
  gap: 0.5,
  '& .MuiSvgIcon-root': { fontSize: 16, color: portalColors.primary },
} as const

const statGridSx = (columns: 3 | 4) =>
  ({
    display: 'grid',
    gap: '0.875rem',
    gridTemplateColumns: {
      xs: '1fr',
      sm: 'repeat(2, minmax(0, 1fr))',
      lg: `repeat(${columns}, minmax(0, 1fr))`,
    },
  }) as const

function DashboardSectionHead({
  title,
  icon,
  action,
  badge,
}: {
  title: string
  icon: ReactNode
  action?: { label: string; to: string }
  badge?: ReactNode
}) {
  return (
    <Box sx={sectionHeadSx}>
      <Typography component="h2" sx={sectionTitleSx}>
        {icon}
        {title}
      </Typography>
      {badge}
      {action && (
        <Typography
          component={RouterLink}
          to={action.to}
          sx={{ fontSize: '0.875rem', fontWeight: 500, color: portalColors.primary, textDecoration: 'none' }}
        >
          {action.label}
        </Typography>
      )}
    </Box>
  )
}

function SectionCard({
  title,
  icon,
  badge,
  children,
}: {
  title: string
  icon: ReactNode
  badge?: ReactNode
  children: ReactNode
}) {
  return (
    <Box
      sx={{
        mb: 3,
        border: `1px solid ${portalColors.border}`,
        borderRadius: '0.75rem',
        bgcolor: portalColors.bgWhite,
        overflow: 'hidden',
      }}
    >
      <Box
        sx={{
          display: 'flex',
          flexWrap: 'wrap',
          alignItems: 'center',
          justifyContent: 'space-between',
          gap: 1.5,
          px: { xs: 2, sm: 2.5 },
          py: 1.5,
          borderBottom: '1px solid #f5f5f4',
        }}
      >
        <Typography sx={{ m: 0, fontSize: '0.9375rem', fontWeight: 600, color: portalColors.textDark, display: 'flex', alignItems: 'center', gap: 0.5 }}>
          <Box component="span" sx={{ color: portalColors.primary, display: 'inline-flex' }}>{icon}</Box>
          {title}
        </Typography>
        {badge}
      </Box>
      <Box sx={{ p: { xs: 2, sm: 2.5 } }}>{children}</Box>
    </Box>
  )
}

function StatusMessage({
  tone,
  icon,
  children,
}: {
  tone: 'success' | 'warning' | 'danger' | 'info'
  icon: ReactNode
  children: ReactNode
}) {
  const colors = {
    success: { border: '#bbf7d0', bg: '#f0fdf4', text: '#166534', icon: '#4ade80' },
    warning: { border: '#fde68a', bg: '#fffbeb', text: '#92400e', icon: '#fbbf24' },
    danger: { border: '#fecaca', bg: '#fef2f2', text: '#991b1b', icon: '#f87171' },
    info: { border: '#bfdbfe', bg: '#eff6ff', text: '#1d4ed8', icon: '#60a5fa' },
  }[tone]

  return (
    <Box
      sx={{
        display: 'flex',
        gap: 1,
        mb: 2,
        border: `1px solid ${colors.border}`,
        borderRadius: '0.5rem',
        bgcolor: colors.bg,
        px: 2,
        py: 1.5,
        color: colors.text,
        fontSize: '0.875rem',
        '& .MuiSvgIcon-root': { color: colors.icon, fontSize: 18, mt: 0.25, flexShrink: 0 },
      }}
    >
      {icon}
      <Box>{children}</Box>
    </Box>
  )
}

function AccreditationPanel({ accreditation }: { accreditation: ClientDashboardAccreditation }) {
  const status = accreditation.status ?? ''
  const { label: statusLabel, chipKey } = resolveAccreditationListItemStatus({
    status,
    displayStatus: accreditation.displayStatus,
  })
  const resubmitted = isResubmittedForReview({ status, displayStatus: accreditation.displayStatus })

  const statusBadge = (
    <Chip
      size="small"
      label={statusLabel}
      sx={portalStatusChipSx(chipKey)}
    />
  )

  let message: ReactNode = null
  let actions: ReactNode = null

  if (status === 'Draft') {
    message = (
      <StatusMessage tone="success" icon={<WarningAmberOutlinedIcon />}>
        Your accreditation application is in draft status. Please continue and submit your application.
      </StatusMessage>
    )
    actions = (
      <Button component={RouterLink} to="/client/accreditation" variant="outlined" fullWidth sx={{ textTransform: 'none' }}>
        Continue Draft
      </Button>
    )
  } else if (resubmitted) {
    message = (
      <StatusMessage tone="success" icon={<CheckCircleOutlinedIcon />}>
        Your revised documents were resubmitted successfully. Your application is now under review again.
      </StatusMessage>
    )
    actions = (
      <Button component={RouterLink} to="/client/accreditation" variant="outlined" fullWidth sx={{ textTransform: 'none' }}>
        View Status
      </Button>
    )
  } else if (status === 'Submitted' || status === 'UnderReview') {
    message = (
      <StatusMessage tone="success" icon={<CheckCircleOutlinedIcon />}>
        Your accreditation application has been submitted successfully. We will notify you once your accreditation is being evaluated.
      </StatusMessage>
    )
    actions = (
      <Button component={RouterLink} to="/client/accreditation" variant="outlined" fullWidth sx={{ textTransform: 'none' }}>
        View Status
      </Button>
    )
  } else if (status === 'Rejected') {
    message = (
      <>
        <StatusMessage tone="danger" icon={<ReportProblemOutlinedIcon />}>
          Your accreditation application was denied. Please address the issues and resubmit.
        </StatusMessage>
        {accreditation.reviewComments && (
          <Box sx={{ mb: 2 }}>
            <Typography sx={{ fontSize: '0.875rem', fontWeight: 600, mb: 1 }}>Reason:</Typography>
            <Box sx={{ bgcolor: portalColors.bgMuted, borderRadius: '0.5rem', p: 2, fontSize: '0.875rem' }}>
              {accreditation.reviewComments}
            </Box>
          </Box>
        )}
      </>
    )
    actions = (
      <Button component={RouterLink} to="/client/accreditation" variant="contained" sx={{ ...portalPrimaryButtonSx, width: '100%' }}>
        Resubmit Application
      </Button>
    )
  } else if (status === 'RevisionRequired') {
    message = (
      <>
        <StatusMessage tone="warning" icon={<WarningAmberOutlinedIcon />}>
          Additional compliance documents are required. Please submit them before the deadline.
        </StatusMessage>
        {accreditation.reviewComments && (
          <Box sx={{ mb: 2 }}>
            <Typography sx={{ fontSize: '0.875rem', fontWeight: 600, mb: 1 }}>Requirements:</Typography>
            <Box sx={{ bgcolor: portalColors.bgMuted, borderRadius: '0.5rem', p: 2, fontSize: '0.875rem' }}>
              {accreditation.reviewComments}
            </Box>
          </Box>
        )}
      </>
    )
    actions = (
      <Button component={RouterLink} to="/client/accreditation" variant="contained" sx={{ ...portalPrimaryButtonSx, width: '100%' }}>
        Submit Compliance
      </Button>
    )
  }

  return (
    <SectionCard title="Accreditation Status" icon={<ShieldOutlinedIcon />} badge={statusBadge}>
      <Box sx={{ display: 'grid', gridTemplateColumns: { lg: '1fr auto' }, gap: 2.5, alignItems: 'start' }}>
        <Box>
          {message}
          {accreditation.submissionType && (
            <Typography sx={{ fontSize: '0.875rem', color: portalColors.textDark }}>
              <strong>Submission Type:</strong> {accreditation.submissionType}
            </Typography>
          )}
        </Box>
        {actions && (
          <Stack spacing={1} sx={{ minWidth: { lg: 176 }, width: { xs: '100%', lg: 'auto' } }}>
            {actions}
            <Button component={RouterLink} to="/client/accreditation" variant="outlined" fullWidth sx={{ textTransform: 'none' }}>
              Application Details
            </Button>
          </Stack>
        )}
      </Box>
    </SectionCard>
  )
}

function DashboardAccreditationSkeleton() {
  return (
    <Box
      sx={{
        mb: 3,
        border: `1px solid ${portalColors.border}`,
        borderRadius: '0.75rem',
        bgcolor: portalColors.bgWhite,
        overflow: 'hidden',
      }}
    >
      <Box sx={{ px: 2.5, py: 1.75, borderBottom: `1px solid ${portalColors.border}` }}>
        <Skeleton variant="text" width={180} height={28} />
      </Box>
      <Box sx={{ p: 2.5 }}>
        <Skeleton variant="rounded" width={110} height={24} sx={{ mb: 2 }} />
        <Skeleton variant="text" width="88%" />
        <Skeleton variant="text" width="70%" />
        <Skeleton variant="rounded" width={160} height={40} sx={{ mt: 2 }} />
      </Box>
    </Box>
  )
}

function AccreditationRequiredPanel() {
  const benefits = [
    'Submit entries to DA agencies (BAI, BFAR, BPI, and more)',
    'Track your submissions in real time',
    'Receive inspection schedules and results',
    'Access compliance documents and certificates',
    'Manage warehouse bookings and containers',
  ]

  return (
    <SectionCard
      title="Accreditation Required"
      icon={<WarningAmberOutlinedIcon />}
      badge={<Chip size="small" label="Not Accredited" sx={getStatusBadgeStyle('Pending')} />}
    >
      <Box sx={{ display: 'grid', gridTemplateColumns: { md: '1.4fr 0.8fr' }, gap: 2.5, alignItems: 'center' }}>
        <Box>
          <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted, mb: 2 }}>
            To access the full features of AgriCheck and submit entries to DA agencies, complete the DA accreditation process first.
          </Typography>
          <Typography sx={{ fontSize: '0.875rem', fontWeight: 600, mb: 1 }}>After accreditation, you can:</Typography>
          <Box component="ul" sx={{ m: 0, mb: 2, p: 0, listStyle: 'none', display: 'grid', gap: 0.75 }}>
            {benefits.map((item) => (
              <Box component="li" key={item} sx={{ display: 'flex', gap: 1, fontSize: '0.875rem' }}>
                <CheckCircleOutlinedIcon sx={{ fontSize: 16, color: portalColors.primary, mt: 0.25 }} />
                <span>{item}</span>
              </Box>
            ))}
          </Box>
          <Box sx={{ border: '1px solid #bbf7d0', bgcolor: portalColors.successSoft, borderRadius: '0.625rem', px: 2, py: 1.5, fontSize: '0.875rem', color: portalColors.primary }}>
            <strong>Note:</strong> The accreditation review typically takes 3–5 business days.
          </Box>
        </Box>
        <Box
          sx={{
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            textAlign: 'center',
            p: 2,
            border: `1px dashed ${portalColors.borderStrong}`,
            borderRadius: '0.75rem',
            bgcolor: '#fafaf9',
          }}
        >
          <VerifiedOutlinedIcon sx={{ fontSize: 32, color: portalColors.primary, mb: 1.5 }} />
          <Button component={RouterLink} to="/client/accreditation" variant="contained" fullWidth sx={{ ...portalPrimaryButtonSx, mb: 1 }}>
            Apply for Accreditation
          </Button>
          <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted }}>Start your application in a few steps</Typography>
        </Box>
      </Box>
    </SectionCard>
  )
}

function AgencyCard({
  agency,
  locked,
}: {
  agency: { id: number; code: string; name: string; logoUrl?: string; hasEntryForm: boolean }
  locked?: boolean
}) {
  const noForm = !locked && !agency.hasEntryForm
  const logoUrl = resolveAgencyLogoUrl(agency.logoUrl)
  const cardBody = (
    <>
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          width: 52,
          height: 52,
          mb: 1.5,
          border: `1px solid ${portalColors.border}`,
          borderRadius: '0.625rem',
          bgcolor: '#fafaf9',
          overflow: 'hidden',
        }}
      >
        {logoUrl ? (
          <Box component="img" src={logoUrl} alt={agency.name} sx={{ maxWidth: '100%', maxHeight: '100%', objectFit: 'contain' }} />
        ) : (
          <BusinessOutlinedIcon sx={{ fontSize: 24, color: portalColors.primary }} />
        )}
      </Box>
      <Typography sx={{ m: 0, fontSize: '0.9375rem', fontWeight: 600, color: portalColors.textDark }}>{agency.code}</Typography>
      <Typography sx={{ m: 0, mt: 0.5, fontSize: '0.75rem', lineHeight: 1.35, color: portalColors.textMuted }}>{agency.name}</Typography>
      {locked && (
        <Box
          sx={{
            position: 'absolute',
            inset: 0,
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            justifyContent: 'center',
            gap: 0.5,
            borderRadius: '0.75rem',
            bgcolor: 'rgba(250, 250, 249, 0.82)',
            color: portalColors.textMuted,
            fontSize: '0.75rem',
            fontWeight: 500,
          }}
        >
          <LockOutlinedIcon sx={{ fontSize: 20 }} />
          Accreditation required
        </Box>
      )}
      {noForm && (
        <Chip size="small" label="No entry form yet" sx={{ mt: 1.5, ...getStatusBadgeStyle('Draft') }} />
      )}
    </>
  )

  const cardSx = {
    position: 'relative',
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    minHeight: 152,
    p: '1.125rem 1rem',
    border: `1px solid ${portalColors.border}`,
    borderRadius: '0.75rem',
    bgcolor: locked || noForm ? '#fafaf9' : portalColors.bgWhite,
    textAlign: 'center',
    textDecoration: 'none',
    opacity: locked || noForm ? 0.72 : 1,
    pointerEvents: locked || noForm ? 'none' : 'auto',
    transition: 'border-color 0.15s ease, box-shadow 0.15s ease, transform 0.15s ease',
    ...(!locked && !noForm
      ? {
          '&:hover': {
            borderColor: portalColors.primary,
            boxShadow: '0 8px 20px rgba(22, 163, 74, 0.1)',
            transform: 'translateY(-1px)',
          },
        }
      : {}),
  } as const

  if (locked || noForm) {
    return <Box sx={cardSx}>{cardBody}</Box>
  }

  return (
    <Box component={RouterLink} to={`/client/entries/new?agencyId=${agency.id}`} sx={cardSx}>
      {cardBody}
    </Box>
  )
}

const WORKFLOW_ENTRY_STATUSES = [
  { key: 'DaIssueBilling', label: 'DA Billing', variant: 'warn' as const },
  { key: 'DaBillingPaymentPending', label: 'Payment Verification', variant: 'warn' as const, filterStatus: 'DaIssueBilling' },
  { key: 'ForInspection', label: 'For Inspection', variant: 'warn' as const },
  { key: 'ReadyForTransport', label: 'Ready for Transport', variant: 'info' as const },
  { key: 'AwaitingTransport', label: 'Awaiting Transport', variant: 'info' as const },
  { key: 'InTransit', label: 'In Transit', variant: 'info' as const },
] as const

function EntryOverviewSection({
  isLoading,
  isAccredited,
  entries,
  workflowCounts,
  workflowLoading,
}: {
  isLoading: boolean
  isAccredited: boolean
  entries?: { total: number; pending: number; approved: number; forCompliance: number }
  workflowCounts: Record<string, number>
  workflowLoading: boolean
}) {
  const lockedBadge = (
    <Chip
      size="small"
      icon={<LockOutlinedIcon sx={{ fontSize: '14px !important' }} />}
      label="Accreditation required"
      sx={getStatusBadgeStyle('Draft')}
    />
  )

  return (
    <Box sx={{ mb: 3 }}>
      <DashboardSectionHead
        title="Entry Overview"
        icon={<BarChartOutlinedIcon />}
        action={isAccredited ? { label: 'View all entries', to: '/client/entries' } : undefined}
        badge={!isAccredited ? lockedBadge : undefined}
      />
      <Box sx={statGridSx(4)}>
        <ClientDashboardStatCard
          to={isAccredited ? '/client/entries' : undefined}
          locked={!isAccredited}
          label="Total Entries"
          value={isAccredited ? statValue(isLoading, entries?.total) : 0}
          meta="View all entries"
          icon={<ArticleOutlinedIcon />}
        />
        <ClientDashboardStatCard
          to={isAccredited ? '/client/entries' : undefined}
          locked={!isAccredited}
          label="Pending"
          value={isAccredited ? statValue(isLoading, entries?.pending) : 0}
          meta="Awaiting action"
          icon={<ScheduleOutlinedIcon />}
          variant="warn"
        />
        <ClientDashboardStatCard
          to={isAccredited ? '/client/entries' : undefined}
          locked={!isAccredited}
          label="In Pipeline"
          value={isAccredited ? statValue(isLoading, entries?.approved) : 0}
          meta="Active workflow entries"
          icon={<CheckCircleOutlinedIcon />}
        />
        <ClientDashboardStatCard
          to={isAccredited ? '/client/entries' : undefined}
          locked={!isAccredited}
          label="For Compliance"
          value={isAccredited ? statValue(isLoading, entries?.forCompliance) : 0}
          meta="Needs documents"
          icon={<ReportProblemOutlinedIcon />}
          variant="warn"
        />
      </Box>
      {isAccredited && WORKFLOW_ENTRY_STATUSES.some((status) => (workflowCounts[status.key] ?? 0) > 0) && (
        <Box sx={{ ...statGridSx(4), mt: '0.875rem' }}>
          {WORKFLOW_ENTRY_STATUSES.map((status) => {
            const count = workflowCounts[status.key] ?? 0
            if (count <= 0) return null
            const filterStatus = 'filterStatus' in status ? status.filterStatus : status.key
            return (
              <ClientDashboardStatCard
                key={status.key}
                to={`/client/entries?status=${filterStatus}`}
                label={status.label}
                value={workflowLoading ? '…' : count}
                meta="View entries"
                icon={<AssignmentOutlinedIcon />}
                variant={status.variant}
              />
            )
          })}
        </Box>
      )}
    </Box>
  )
}

function LogisticsSection({
  isLoading,
  logistics,
}: {
  isLoading: boolean
  logistics?: ClientDashboardLogisticsStats
}) {
  const showPendingPayments = (logistics?.pendingPayments ?? 0) > 0
  const showApprovedContainers = (logistics?.approvedContainers ?? 0) > 0
  const showAssignedContainers = (logistics?.assignedContainers ?? 0) > 0
  const showPendingInspections = (logistics?.pendingInspections ?? 0) > 0

  return (
    <Box sx={{ mb: 3 }}>
      <DashboardSectionHead title="Payments & Logistics" icon={<GridViewOutlinedIcon />} />
      <Box sx={statGridSx(3)}>
        {showPendingPayments && (
          <ClientDashboardStatCard
            to="/client/bills"
            label="Pending Payments"
            value={statValue(isLoading, logistics?.pendingPayments)}
            meta="Review payments"
            icon={<CreditCardOutlinedIcon />}
            variant="info"
          />
        )}
          <ClientDashboardStatCard
            to="/client/bills"
            label="Unpaid Bills"
          value={statValue(isLoading, logistics?.unpaidBills)}
          meta="Pay bills"
          icon={<ReceiptLongOutlinedIcon />}
          variant={(logistics?.overdueBills ?? 0) > 0 ? 'danger' : 'warn'}
          extra={
            (logistics?.overdueBills ?? 0) > 0 ? (
              <Typography sx={{ mt: 0.5, fontSize: '0.75rem', color: '#991b1b' }}>
                {logistics?.overdueBills} overdue
              </Typography>
            ) : undefined
          }
        />
        {showApprovedContainers && (
          <ClientDashboardStatCard
            to="/client/warehouse/bookings"
            label="Approved Containers"
            value={statValue(isLoading, logistics?.approvedContainers)}
            meta="Warehouse booking"
            icon={<CheckCircleOutlinedIcon />}
          />
        )}
        {showAssignedContainers && (
          <ClientDashboardStatCard
            to="/client/containers"
            label="Assigned Containers"
            value={statValue(isLoading, logistics?.assignedContainers)}
            meta="Track containers"
            icon={<Inventory2OutlinedIcon />}
          />
        )}
        {showPendingInspections && (
          <ClientDashboardStatCard
            to="/client/inspections"
            label="Pending Inspections"
            value={statValue(isLoading, logistics?.pendingInspections)}
            meta="View inspections"
            icon={<AssignmentOutlinedIcon />}
            variant="warn"
          />
        )}
      </Box>
    </Box>
  )
}

function AgencySection({
  agencies,
  isAccredited,
}: {
  agencies: Array<{ id: number; code: string; name: string; logoUrl?: string; hasEntryForm: boolean }>
  isAccredited: boolean
}) {
  if (agencies.length === 0) return null

  const gridColumns = agencies.length >= 5 ? { lg: 'repeat(5, minmax(0, 1fr))' } : { lg: 'repeat(3, minmax(0, 1fr))' }

  return (
    <Box sx={{ mb: 3 }}>
      <DashboardSectionHead
        title={isAccredited ? 'Submit to DA Agencies' : 'DA Agencies'}
        icon={<BusinessOutlinedIcon />}
        action={isAccredited ? { label: 'Browse all agencies', to: '/client/entries/agencies' } : undefined}
        badge={
          !isAccredited ? (
            <Chip
              size="small"
              icon={<LockOutlinedIcon sx={{ fontSize: '14px !important' }} />}
              label="Unlock after accreditation"
              sx={getStatusBadgeStyle('Draft')}
            />
          ) : undefined
        }
      />
      <Box
        sx={{
          display: 'grid',
          gap: '0.875rem',
          gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, minmax(0, 1fr))', ...gridColumns },
        }}
      >
        {agencies.map((agency) => (
          <AgencyCard key={agency.id} agency={agency} locked={!isAccredited} />
        ))}
      </Box>
    </Box>
  )
}

function PaymentRequiredSection({
  isLoading,
  logistics,
  recentBills,
}: {
  isLoading: boolean
  logistics?: ClientDashboardLogisticsStats
  recentBills: Array<{
    uuid: string
    entryUuid?: string
    billNumber: string
    entryReferenceNo?: string
    agencyName?: string
    amount: number
    status: string
    dueDate?: string
    isOverdue: boolean
  }>
}) {
  if (recentBills.length === 0) return null

  const overdue = logistics?.overdueBills ?? 0
  const unpaid = logistics?.unpaidBills ?? 0
  const totalDue = logistics?.totalAmountDue ?? 0

  return (
    <Box sx={{ mb: 3, border: `1px solid ${portalColors.border}`, borderRadius: '0.75rem', bgcolor: portalColors.bgWhite, overflow: 'hidden' }}>
      <Box
        sx={{
          display: 'flex',
          flexWrap: 'wrap',
          alignItems: 'center',
          justifyContent: 'space-between',
          gap: 2,
          px: { xs: 2, sm: 2.5 },
          py: 2,
          borderBottom: `1px solid ${portalColors.border}`,
        }}
      >
        <Box sx={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: 1.5 }}>
          <Typography sx={{ fontSize: '1.125rem', fontWeight: 600 }}>
            <ReceiptLongOutlinedIcon sx={{ fontSize: 18, mr: 1, verticalAlign: 'text-bottom' }} />
            Payment Required
          </Typography>
          {overdue > 0 && <Chip size="small" label={`${overdue} Overdue`} sx={getStatusBadgeStyle('Rejected')} />}
        </Box>
        <Button component={RouterLink} to="/client/bills" variant="outlined" sx={{ textTransform: 'none' }}>
          View All Bills
        </Button>
      </Box>

      <Box sx={{ p: { xs: 2, sm: 2.5 } }}>
        {overdue > 0 && (
          <StatusMessage tone="danger" icon={<WarningAmberOutlinedIcon />}>
            <strong>Urgent:</strong> You have {overdue} overdue bill{overdue > 1 ? 's' : ''}. Please pay immediately to avoid service interruption.
          </StatusMessage>
        )}

        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: { xs: '1fr', sm: 'repeat(3, 1fr)' },
            gap: 2,
            mb: 3,
            textAlign: 'center',
          }}
        >
          <Box>
            <Typography sx={{ fontSize: { xs: '1.25rem', sm: '1.5rem' }, fontWeight: 700, color: overdue > 0 ? '#dc2626' : '#ca8a04' }}>
              {statValue(isLoading, unpaid)}
            </Typography>
            <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted }}>Unpaid Bills</Typography>
          </Box>
          <Box>
            <Typography sx={{ fontSize: { xs: '1.25rem', sm: '1.5rem' }, fontWeight: 700, color: '#2563eb' }}>
              {isLoading ? '…' : formatCurrency(totalDue)}
            </Typography>
            <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted }}>Total Amount Due</Typography>
          </Box>
          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
            <Button
              component={RouterLink}
              to="/client/bills"
              variant="contained"
              sx={{
                ...portalPrimaryButtonSx,
                bgcolor: overdue > 0 ? '#dc2626' : '#eab308',
                '&:hover': { bgcolor: overdue > 0 ? '#b91c1c' : '#ca8a04' },
              }}
            >
              Pay Now
            </Button>
          </Box>
        </Box>

        <Box sx={{ overflowX: 'auto' }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                {['Entry Number', 'Bill Reference', 'Amount', 'Due Date', 'Status', 'Action'].map((column) => (
                  <TableCell key={column} sx={portalTableHeadCellSx}>
                    {column}
                  </TableCell>
                ))}
              </TableRow>
            </TableHead>
            <TableBody>
              {recentBills.map((bill) => (
                <TableRow key={bill.uuid} sx={{ bgcolor: bill.isOverdue ? '#fef2f2' : 'inherit' }}>
                  <TableCell>
                    <Typography sx={{ fontWeight: 600, fontSize: '0.875rem' }}>{bill.entryReferenceNo ?? '—'}</Typography>
                    <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted }}>{bill.agencyName ?? '—'}</Typography>
                  </TableCell>
                  <TableCell>{bill.billNumber}</TableCell>
                  <TableCell>
                    <Typography sx={{ fontWeight: 600 }}>{formatCurrency(bill.amount)}</Typography>
                    {bill.isOverdue && (
                      <Typography sx={{ fontSize: '0.75rem', color: '#dc2626', display: 'flex', alignItems: 'center', gap: 0.5 }}>
                        <WarningAmberOutlinedIcon sx={{ fontSize: 14 }} />
                        Overdue
                      </Typography>
                    )}
                  </TableCell>
                  <TableCell>{bill.dueDate ? new Date(bill.dueDate).toLocaleDateString('en-PH', { month: 'short', day: 'numeric', year: 'numeric' }) : 'No due date'}</TableCell>
                  <TableCell>
                    <Chip size="small" label={formatStatusLabel(bill.status)} sx={getStatusBadgeStyle(bill.status)} />
                  </TableCell>
                  <TableCell>
                    <Button
                      component={RouterLink}
                      to={bill.entryUuid ? `/client/entries/${bill.entryUuid}` : '/client/entries'}
                      size="small"
                      variant="contained"
                      sx={{
                        textTransform: 'none',
                        fontSize: '0.75rem',
                        bgcolor: bill.isOverdue ? '#dc2626' : '#2563eb',
                        boxShadow: 'none',
                        '&:hover': { bgcolor: bill.isOverdue ? '#b91c1c' : '#1d4ed8', boxShadow: 'none' },
                      }}
                    >
                      Pay
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Box>
      </Box>
    </Box>
  )
}

const liveAccreditationStatuses = new Set(['Submitted', 'UnderReview', 'RevisionRequired'])

export function ClientDashboardPage() {
  const { data, isLoading, isFetching } = useGetDashboardQuery(undefined, {
    refetchOnMountOrArgChange: true,
    refetchOnFocus: true,
    refetchOnReconnect: true,
  })
  const currentUser = useSelector(selectCurrentUser)
  const dashboard = data?.data
  const accreditationStatus = dashboard?.accreditation?.status
  useGetDashboardQuery(undefined, {
    skip: !accreditationStatus || !liveAccreditationStatuses.has(accreditationStatus),
    pollingInterval: 5000,
  })
  const showSkeleton = isLoading || (isFetching && !dashboard)
  const showRefreshFlash = Boolean(isFetching && dashboard)
  const workflow = dashboard?.workflow
  const workflowCounts = {
    DaIssueBilling: workflow?.daIssueBilling ?? 0,
    DaBillingPaymentPending: workflow?.daBillingPaymentPending ?? 0,
    ForInspection: workflow?.forInspection ?? 0,
    ReadyForTransport: workflow?.readyForTransport ?? 0,
    AwaitingTransport: workflow?.awaitingTransport ?? 0,
    InTransit: workflow?.inTransit ?? 0,
  }
  const workflowLoading = isLoading
  const profile = dashboard?.profile
  const accreditation = resolveDashboardAccreditation(dashboard?.accreditation, currentUser?.uuid)
  const isAccredited =
    accreditation?.isAccredited === true || accreditation?.status === 'Approved'
  const displayName = profile?.firstName
    ? `${profile.firstName} ${profile.lastName}`.trim()
    : profile?.email ?? 'Importer'

  return (
    <Box sx={{ width: '100%', maxWidth: '80rem', mx: 'auto' }}>
      <Box
        sx={{
          mb: 2.5,
          borderRadius: '0.85rem',
          px: { xs: 2.5, md: 3 },
          py: { xs: 2.5, md: 3 },
          color: '#fff',
          background: `linear-gradient(135deg, ${portalColors.primary} 0%, ${portalColors.primaryDark} 100%)`,
        }}
      >
        <Box sx={{ display: 'flex', flexDirection: { xs: 'column', sm: 'row' }, justifyContent: 'space-between', gap: 2 }}>
          <Box sx={{ minWidth: 0 }}>
            <Typography sx={{ fontSize: '0.625rem', fontWeight: 600, letterSpacing: '0.14em', textTransform: 'uppercase', color: 'rgba(255,255,255,0.8)' }}>
              Client Portal
            </Typography>
            <Typography component="h1" sx={{ fontSize: { xs: '1.125rem', sm: '1.25rem' }, fontWeight: 600, letterSpacing: '-0.01em' }}>
              Dashboard
            </Typography>
            <Typography sx={{ mt: 0.5, fontSize: '0.875rem', color: 'rgba(255,255,255,0.88)' }}>
              Welcome back, {displayName}
            </Typography>
          </Box>
          <Box
            sx={{
              alignSelf: { xs: 'stretch', sm: 'center' },
              display: 'inline-flex',
              alignItems: 'center',
              justifyContent: { xs: 'center', sm: 'flex-start' },
              gap: 1,
              px: 1.5,
              py: 1,
              borderRadius: '0.5rem',
              bgcolor: 'rgba(255,255,255,0.12)',
              border: '1px solid rgba(255,255,255,0.18)',
              flexShrink: 0,
            }}
          >
            <PersonOutlineOutlinedIcon sx={{ fontSize: 18 }} />
            <Typography sx={{ fontSize: '0.875rem', fontWeight: 500, overflow: 'hidden', textOverflow: 'ellipsis' }}>{displayName}</Typography>
          </Box>
        </Box>
      </Box>

      {showRefreshFlash && (
        <LinearProgress
          sx={{
            mb: 1.25,
            height: 2,
            borderRadius: 999,
            bgcolor: 'transparent',
            '& .MuiLinearProgress-bar': { bgcolor: portalColors.primary },
          }}
        />
      )}

      {showSkeleton ? (
        <DashboardAccreditationSkeleton />
      ) : isAccredited ? (
        <Box
          sx={{
            mb: 2.5,
            display: 'flex',
            flexWrap: 'wrap',
            alignItems: 'center',
            gap: '0.75rem 1.25rem',
            px: 2,
            py: 1.75,
            border: `1px solid ${portalColors.border}`,
            borderRadius: '0.75rem',
            bgcolor: portalColors.bgWhite,
          }}
        >
          <Chip
            size="small"
            icon={<VerifiedOutlinedIcon sx={{ fontSize: '14px !important' }} />}
            label="Accredited"
            sx={{ bgcolor: '#dcfce7', color: portalColors.primary, fontWeight: 600 }}
          />
          {accreditation?.accreditationNumber && (
            <Box>
              <Typography sx={{ fontSize: '0.6875rem', fontWeight: 600, letterSpacing: '0.08em', textTransform: 'uppercase', color: portalColors.textMuted }}>
                Accreditation Number
              </Typography>
              <Typography sx={{ fontSize: '0.9375rem', fontWeight: 600 }}>{accreditation.accreditationNumber}</Typography>
            </Box>
          )}
          {accreditation?.companyName && (
            <Box>
              <Typography sx={{ fontSize: '0.6875rem', fontWeight: 600, letterSpacing: '0.08em', textTransform: 'uppercase', color: portalColors.textMuted }}>
                Company
              </Typography>
              <Typography sx={{ fontSize: '0.9375rem', fontWeight: 600 }}>{accreditation.companyName}</Typography>
            </Box>
          )}
        </Box>
      ) : accreditation?.status ? (
        <AccreditationPanel accreditation={accreditation} />
      ) : (
        <AccreditationRequiredPanel />
      )}

      {isAccredited ? (
        <>
          <EntryOverviewSection
            isLoading={isLoading}
            isAccredited
            entries={dashboard?.entries}
            workflowCounts={workflowCounts}
            workflowLoading={workflowLoading}
          />
          <LogisticsSection isLoading={isLoading} logistics={dashboard?.logistics} />
          <AgencySection agencies={dashboard?.agencies ?? []} isAccredited />
        </>
      ) : (
        <>
          <EntryOverviewSection
            isLoading={isLoading}
            isAccredited={false}
            workflowCounts={workflowCounts}
            workflowLoading={workflowLoading}
          />
          <AgencySection agencies={dashboard?.agencies ?? []} isAccredited={false} />
        </>
      )}

      <PaymentRequiredSection
        isLoading={isLoading}
        logistics={dashboard?.logistics}
        recentBills={dashboard?.recentBills ?? []}
      />

      {!dashboard && !isLoading && !isFetching && (
        <Typography sx={portalEmptyStateSx}>Unable to load dashboard data.</Typography>
      )}
    </Box>
  )
}
