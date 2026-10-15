const GUID_REGEX =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i

export function isContainerUuid(value: string): boolean {
  return GUID_REGEX.test(value.trim())
}
