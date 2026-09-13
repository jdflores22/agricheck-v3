import ContentCopyOutlinedIcon from '@mui/icons-material/ContentCopyOutlined'
import DeleteOutlineOutlinedIcon from '@mui/icons-material/DeleteOutlineOutlined'
import ExpandMoreIcon from '@mui/icons-material/ExpandMoreOutlined'
import InfoOutlinedIcon from '@mui/icons-material/InfoOutlined'
import Inventory2OutlinedIcon from '@mui/icons-material/Inventory2Outlined'
import WarningAmberOutlinedIcon from '@mui/icons-material/WarningAmberOutlined'
import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Alert,
  Box,
  Button,
  Chip,
  LinearProgress,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { useEffect, useMemo, useState } from 'react'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx } from '../../../components/portal/portalStyles'
import { DynamicFormRenderer } from './DynamicFormRenderer'
import type { FormFieldSchema } from '../../forms/formSchema'
import {
  MAX_CONTAINERS,
  copyContainerValues,
  getContainerCompletion,
  getContainerNumber,
  getContainerType,
  isContainerComplete,
  type ContainerDraftState,
  type ContainerValidationResult,
} from '../utils/containerFormUtils'

interface ContainerInformationSectionProps {
  schemaFields: FormFieldSchema[]
  numContainers: number
  containers: ContainerDraftState
  validation?: ContainerValidationResult
  disabled?: boolean
  onNumContainersChange: (count: number) => void
  onContainersChange: (containers: ContainerDraftState) => void
  onClearDraft?: () => void
}

export function ContainerInformationSection({
  schemaFields,
  numContainers,
  containers,
  validation,
  disabled = false,
  onNumContainersChange,
  onContainersChange,
  onClearDraft,
}: ContainerInformationSectionProps) {
  const [expandedIndex, setExpandedIndex] = useState(0)
  const completion = useMemo(() => getContainerCompletion(schemaFields, containers), [schemaFields, containers])

  useEffect(() => {
    if (expandedIndex >= containers.length) {
      setExpandedIndex(Math.max(0, containers.length - 1))
    }
  }, [containers.length, expandedIndex])

  const handleCountChange = (rawValue: string) => {
    if (rawValue === '') {
      onNumContainersChange(0)
      return
    }

    const parsed = Number.parseInt(rawValue, 10)
    if (Number.isNaN(parsed)) return
    onNumContainersChange(Math.max(0, Math.min(MAX_CONTAINERS, parsed)))
  }

  const updateContainerField = (index: number, name: string, value: string) => {
    const next = containers.map((item, itemIndex) => (itemIndex === index ? { ...item, [name]: value } : item))
    onContainersChange(next)
  }

  const updateContainerFields = (index: number, updates: Record<string, string>) => {
    const next = containers.map((item, itemIndex) => (itemIndex === index ? { ...item, ...updates } : item))
    onContainersChange(next)
  }

  const handleCopyPrevious = (index: number) => {
    if (index <= 0) return
    const next = containers.map((item, itemIndex) =>
      itemIndex === index ? copyContainerValues(containers[index - 1]) : item,
    )
    onContainersChange(next)
  }

  return (
    <Stack id="container-information" spacing={3}>
      <PortalPanel title="Container Information">
        <Box sx={{ px: 2.5, py: 2.5 }}>
          <Stack direction="row" spacing={1.5} sx={{ alignItems: 'flex-start', mb: 2 }}>
            <Inventory2OutlinedIcon sx={{ color: portalColors.primary, mt: 0.25 }} />
            <Box>
              <Typography sx={{ fontWeight: 600, color: portalColors.textDark }}>
                Specify the number of containers for this entry
              </Typography>
              <Typography sx={{ mt: 0.5, fontSize: '0.8125rem', color: portalColors.textMuted }}>
                At least 1 container is required before you can submit this entry. Maximum {MAX_CONTAINERS} per entry.
              </Typography>
            </Box>
          </Stack>

          <TextField
            label="Number of Containers"
            type="number"
            size="small"
            value={numContainers === 0 ? '' : numContainers}
            onChange={(event) => handleCountChange(event.target.value)}
            disabled={disabled}
            slotProps={{ htmlInput: { min: 0, max: MAX_CONTAINERS } }}
            sx={{ maxWidth: 280, '& .MuiOutlinedInput-root': { borderRadius: '0.5rem' } }}
          />

          <Stack direction="row" spacing={1} sx={{ mt: 1.5, alignItems: 'center', color: portalColors.textMuted }}>
            <InfoOutlinedIcon sx={{ fontSize: 16 }} />
            <Typography sx={{ fontSize: '0.75rem' }}>
              Container cards will appear below once you enter a count greater than 0.
            </Typography>
          </Stack>
        </Box>
      </PortalPanel>

      {numContainers > 0 && (
        <PortalPanel title="Container Details">
          <Box sx={{ px: 2.5, py: 2.5 }}>
            <Stack
              direction={{ xs: 'column', sm: 'row' }}
              spacing={2}
              sx={{ justifyContent: 'space-between', alignItems: { sm: 'center' }, mb: 2 }}
            >
              <Box sx={{ minWidth: 0, flex: 1 }}>
                <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted }}>
                  {completion.completed} of {completion.total} containers completed
                </Typography>
                <LinearProgress
                  variant="determinate"
                  value={completion.percent}
                  sx={{
                    mt: 1,
                    height: 8,
                    borderRadius: 999,
                    bgcolor: portalColors.bgMuted,
                    '& .MuiLinearProgress-bar': { bgcolor: portalColors.primary, borderRadius: 999 },
                  }}
                />
              </Box>
              {onClearDraft ? (
                <Button
                  size="small"
                  variant="outlined"
                  color="error"
                  startIcon={<DeleteOutlineOutlinedIcon />}
                  onClick={onClearDraft}
                  disabled={disabled}
                  sx={portalOutlinedButtonSx}
                >
                  Clear Draft
                </Button>
              ) : null}
            </Stack>

            {validation?.duplicateContainerNumbers.length ? (
              <Alert severity="warning" sx={{ mb: 2, borderRadius: '0.75rem' }}>
                Duplicate container numbers detected. Each container must have a unique number.
              </Alert>
            ) : null}

            <Stack spacing={1.5}>
              {containers.map((values, index) => {
                const complete = isContainerComplete(schemaFields, values)
                const errorCount = Object.keys(validation?.errors[index] ?? {}).length
                const containerNumber = getContainerNumber(values)
                const containerType = getContainerType(values)

                return (
                  <Accordion
                    key={index}
                    expanded={expandedIndex === index}
                    onChange={(_, expanded) => setExpandedIndex(expanded ? index : -1)}
                    disableGutters
                    sx={{
                      border: `1px solid ${portalColors.border}`,
                      borderRadius: '0.75rem !important',
                      overflow: 'hidden',
                      '&:before': { display: 'none' },
                      boxShadow: 'none',
                    }}
                  >
                    <AccordionSummary expandIcon={<ExpandMoreIcon />} sx={{ px: 2, bgcolor: portalColors.bgMuted }}>
                      <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center', flexWrap: 'wrap', width: '100%' }}>
                        <Typography sx={{ fontWeight: 600, color: portalColors.textDark }}>
                          Container {index + 1} of {numContainers}
                        </Typography>
                        <Chip
                          size="small"
                          label={complete ? 'Complete' : 'Incomplete'}
                          sx={{
                            bgcolor: complete ? portalColors.successSoft : portalColors.bgWhite,
                            color: complete ? portalColors.successText : portalColors.textMuted,
                            fontWeight: 600,
                          }}
                        />
                        {errorCount > 0 ? (
                          <Chip
                            size="small"
                            icon={<WarningAmberOutlinedIcon sx={{ fontSize: '16px !important' }} />}
                            label={`${errorCount} error${errorCount === 1 ? '' : 's'}`}
                            color="error"
                            variant="outlined"
                          />
                        ) : null}
                        {containerNumber || containerType ? (
                          <Typography sx={{ ml: 'auto', fontSize: '0.8125rem', color: portalColors.textMuted }}>
                            {[containerNumber, containerType].filter(Boolean).join(' · ')}
                          </Typography>
                        ) : null}
                      </Stack>
                    </AccordionSummary>
                    <AccordionDetails sx={{ px: 2, pb: 2.5, pt: 1 }}>
                      {index > 0 ? (
                        <Box sx={{ display: 'flex', justifyContent: 'flex-end', mb: 2 }}>
                          <Button
                            size="small"
                            variant="outlined"
                            startIcon={<ContentCopyOutlinedIcon />}
                            onClick={() => handleCopyPrevious(index)}
                            disabled={disabled}
                            sx={portalOutlinedButtonSx}
                          >
                            Copy from Previous Container
                          </Button>
                        </Box>
                      ) : null}

                      <DynamicFormRenderer
                        schemaJson={JSON.stringify(schemaFields)}
                        values={values}
                        onChange={(name, value) => updateContainerField(index, name, value)}
                        onBatchChange={(updates) => updateContainerFields(index, updates)}
                        disabled={disabled}
                        fieldErrors={validation?.errors[index]}
                      />
                    </AccordionDetails>
                  </Accordion>
                )
              })}
            </Stack>
          </Box>
        </PortalPanel>
      )}
    </Stack>
  )
}
