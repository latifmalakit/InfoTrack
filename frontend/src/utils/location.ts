export const MAX_LOCATION_LENGTH = 80;

export function normalizeLocationInput(value: string): string {
  return value.trim();
}

export function validateLocationInput(value: string): string | null {
  const normalized = normalizeLocationInput(value);
  if (!normalized) {
    return null;
  }

  if (normalized.length > MAX_LOCATION_LENGTH) {
    return `Location cannot exceed ${MAX_LOCATION_LENGTH} characters.`;
  }

  return null;
}
