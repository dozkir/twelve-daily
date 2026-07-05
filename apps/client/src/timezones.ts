// Timezone data helpers shared across screens.
// Prefer the engine's full IANA list (Intl.supportedValuesOf) when available;
// fall back to a curated set of common zones on runtimes that lack it (e.g. some Hermes builds).

export function getDeviceTimezone(): string {
  try {
    return Intl.DateTimeFormat().resolvedOptions().timeZone || "UTC";
  } catch {
    return "UTC";
  }
}

const FALLBACK_TIMEZONES: string[] = [
  "UTC",
  // Americas
  "America/Anchorage",
  "America/Los_Angeles",
  "America/Denver",
  "America/Phoenix",
  "America/Chicago",
  "America/Mexico_City",
  "America/New_York",
  "America/Toronto",
  "America/Bogota",
  "America/Lima",
  "America/Halifax",
  "America/Santiago",
  "America/Sao_Paulo",
  "America/Argentina/Buenos_Aires",
  "America/Noronha",
  // Europe / Africa
  "Atlantic/Azores",
  "Europe/Lisbon",
  "Europe/London",
  "Europe/Dublin",
  "Africa/Casablanca",
  "Africa/Lagos",
  "Europe/Madrid",
  "Europe/Paris",
  "Europe/Berlin",
  "Europe/Rome",
  "Europe/Amsterdam",
  "Europe/Zurich",
  "Europe/Warsaw",
  "Africa/Cairo",
  "Africa/Johannesburg",
  "Europe/Athens",
  "Europe/Helsinki",
  "Europe/Bucharest",
  "Europe/Istanbul",
  "Africa/Nairobi",
  "Europe/Moscow",
  // Asia / Oceania
  "Asia/Jerusalem",
  "Asia/Riyadh",
  "Asia/Dubai",
  "Asia/Tehran",
  "Asia/Karachi",
  "Asia/Kolkata",
  "Asia/Kathmandu",
  "Asia/Dhaka",
  "Asia/Bangkok",
  "Asia/Jakarta",
  "Asia/Singapore",
  "Asia/Kuala_Lumpur",
  "Asia/Hong_Kong",
  "Asia/Shanghai",
  "Asia/Taipei",
  "Asia/Manila",
  "Asia/Tokyo",
  "Asia/Seoul",
  "Australia/Perth",
  "Australia/Adelaide",
  "Australia/Brisbane",
  "Australia/Sydney",
  "Pacific/Guam",
  "Pacific/Auckland",
  "Pacific/Fiji",
  "Pacific/Honolulu"
];

export function getTimezones(): string[] {
  const intl = Intl as typeof Intl & { supportedValuesOf?: (key: string) => string[] };

  if (typeof intl.supportedValuesOf === "function") {
    try {
      const zones = intl.supportedValuesOf("timeZone");
      if (zones.length > 0) {
        return zones;
      }
    } catch {
      // fall through to the curated list
    }
  }

  return FALLBACK_TIMEZONES;
}

// Short UTC-offset label for a zone (e.g. "GMT-3"), computed from the current instant.
// Returns an empty string when the runtime can't resolve it.
export function formatTimezoneOffset(timeZone: string): string {
  for (const timeZoneName of ["shortOffset", "short"] as const) {
    try {
      const parts = new Intl.DateTimeFormat("en-US", { timeZone, timeZoneName }).formatToParts(new Date());
      const value = parts.find((part) => part.type === "timeZoneName")?.value;
      if (value && /GMT|UTC/i.test(value)) {
        return value;
      }
    } catch {
      // try the next style, then give up
    }
  }

  return "";
}
