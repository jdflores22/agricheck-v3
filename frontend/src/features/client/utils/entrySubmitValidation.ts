export const ENTRY_CONTAINER_EDIT_HASH = 'container-information'

export type EntrySubmitValidationIssue = {
  code: 'CONTAINERS_REQUIRED' | 'CONTAINERS_INCOMPLETE'
  title: string
  message: string
}

export function getEntryEditPath(entryUuid: string) {
  return `/client/entries/${entryUuid}/edit#${ENTRY_CONTAINER_EDIT_HASH}`
}

export function validateEntryForSubmit({
  hasContainerForm,
  savedContainerCount,
  expectedContainerCount,
}: {
  hasContainerForm: boolean
  savedContainerCount: number
  expectedContainerCount: number
}): EntrySubmitValidationIssue | null {
  const requiresContainers = hasContainerForm || expectedContainerCount > 0
  if (!requiresContainers) return null

  if (savedContainerCount === 0) {
    return {
      code: 'CONTAINERS_REQUIRED',
      title: 'No container details yet',
      message:
        'This entry has no container details. Add the number of containers and complete each container form before submitting.',
    }
  }

  if (expectedContainerCount > savedContainerCount) {
    const remaining = expectedContainerCount - savedContainerCount
    return {
      code: 'CONTAINERS_INCOMPLETE',
      title: 'Incomplete container details',
      message: `This entry expects ${expectedContainerCount} container${expectedContainerCount === 1 ? '' : 's'}, but only ${savedContainerCount} ${savedContainerCount === 1 ? 'is' : 'are'} saved. Add ${remaining} more container${remaining === 1 ? '' : 's'} before submitting.`,
    }
  }

  return null
}
