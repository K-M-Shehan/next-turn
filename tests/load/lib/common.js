import { Rate } from "k6/metrics";

export const businessFailures = new Rate("business_failures");

export function authHeaders(token, tenantId) {
  return {
    Authorization: `Bearer ${token}`,
    "Content-Type": "application/json",
    "X-Tenant-Id": tenantId,
  };
}

export function defaultThresholds(scenarioName, p95Ms, errorRate, minReqRatePerSec) {
  return {
    http_req_duration: [`p(95)<${p95Ms}`],
    http_req_failed: [`rate<${errorRate}`],
    business_failures: ["rate<0.02"],
    [`http_reqs{scenario:${scenarioName}}`]: [`rate>${minReqRatePerSec}`],
  };
}

export function pickByVu(items) {
  return items[(__VU - 1) % items.length];
}

export function isoDateOnly(date) {
  const year = date.getUTCFullYear();
  const month = String(date.getUTCMonth() + 1).padStart(2, "0");
  const day = String(date.getUTCDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}
