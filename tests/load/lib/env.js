export const defaults = {
  baseUrl: "https://qa-api.example.com",
  tenantId: "",
  queueId: "",
  organisationId: "",
  appointmentProfileId: "",
  queueUsersJson: "[]",
  staffUsersJson: "[]",
  appointmentUsersJson: "[]",
  queueVus: "120",
  queueDuration: "5m",
  staffVus: "50",
  staffDuration: "5m",
  appointmentVus: "100",
  appointmentDuration: "5m",
  p95Ms: "2000",
  errorRate: "0.01",
  minReqRatePerSec: "10",
  skipReason: "Skipped due to no-show",
};

function readEnv(name, fallback) {
  const value = __ENV[name];
  return value === undefined || value === null || value === "" ? fallback : value;
}

export function env() {
  return {
    baseUrl: readEnv("NT_BASE_URL", defaults.baseUrl).replace(/\/$/, ""),
    tenantId: readEnv("NT_TENANT_ID", defaults.tenantId),
    queueId: readEnv("NT_QUEUE_ID", defaults.queueId),
    organisationId: readEnv("NT_ORGANISATION_ID", defaults.organisationId),
    appointmentProfileId: readEnv("NT_APPOINTMENT_PROFILE_ID", defaults.appointmentProfileId),
    queueUsersJson: readEnv("NT_QUEUE_USERS_JSON", defaults.queueUsersJson),
    staffUsersJson: readEnv("NT_STAFF_USERS_JSON", defaults.staffUsersJson),
    appointmentUsersJson: readEnv("NT_APPOINTMENT_USERS_JSON", defaults.appointmentUsersJson),
    queueVus: Number(readEnv("NT_QUEUE_VUS", defaults.queueVus)),
    queueDuration: readEnv("NT_QUEUE_DURATION", defaults.queueDuration),
    staffVus: Number(readEnv("NT_STAFF_VUS", defaults.staffVus)),
    staffDuration: readEnv("NT_STAFF_DURATION", defaults.staffDuration),
    appointmentVus: Number(readEnv("NT_APPOINTMENT_VUS", defaults.appointmentVus)),
    appointmentDuration: readEnv("NT_APPOINTMENT_DURATION", defaults.appointmentDuration),
    p95Ms: Number(readEnv("NT_P95_MS", defaults.p95Ms)),
    errorRate: Number(readEnv("NT_MAX_ERROR_RATE", defaults.errorRate)),
    minReqRatePerSec: Number(readEnv("NT_MIN_REQ_RATE", defaults.minReqRatePerSec)),
    skipReason: readEnv("NT_SKIP_REASON", defaults.skipReason),
  };
}

export function parseUsers(jsonText, envVarName) {
  let parsed;

  try {
    parsed = JSON.parse(jsonText);
  } catch (error) {
    throw new Error(`${envVarName} must be valid JSON. ${String(error)}`);
  }

  if (!Array.isArray(parsed) || parsed.length === 0) {
    throw new Error(`${envVarName} must contain at least one credential object.`);
  }

  for (const [index, item] of parsed.entries()) {
    if (!item || typeof item.email !== "string" || typeof item.password !== "string") {
      throw new Error(`${envVarName}[${index}] must have string email and password fields.`);
    }
  }

  return parsed;
}

export function requireEnvValue(value, name) {
  if (!value) {
    throw new Error(`${name} is required. Set ${name} before running this script.`);
  }
}
