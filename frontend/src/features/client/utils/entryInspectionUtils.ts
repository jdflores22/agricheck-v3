import type { ClientContainerInspectionStatus } from '../api/clientApi'

export function getContainerInspectionProgress(containers: ClientContainerInspectionStatus[]) {
  const completeContainers = containers.filter((container) => container.isComplete).length
  const approvedContainers = containers.filter((container) => container.isApproved).length
  const allUploadsComplete =
    containers.length > 0 && containers.every((container) => container.isComplete)
  const allApproved =
    containers.length > 0 && containers.every((container) => container.isApproved)
  const awaitingInspectorReview = allUploadsComplete && !allApproved

  return {
    completeContainers,
    approvedContainers,
    allUploadsComplete,
    allApproved,
    awaitingInspectorReview,
  }
}
