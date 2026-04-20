import http from "k6/http";
import { check, sleep } from "k6";
import { Rate } from "k6/metrics";
import { env, parseUsers, requireEnvValue } from "../lib/env.js";
import { loginUsers } from "../lib/auth.js";
import { authHeaders, businessFailures, pickByVu } from "../lib/common.js";

const cfg = env();
requireEnvValue(cfg.tenantId, "NT_TENANT_ID");
requireEnvValue(cfg.queueId, "NT_QUEUE_ID");

const users = parseUsers(cfg.queueUsersJson, "NT_QUEUE_USERS_JSON");
const serverErrors = new Rate("server_errors");

function parseDurationToSeconds(duration) {
  const match = String(duration).trim().match(/^(\d+)([smh])$/i);
  if (!match) {
    return 480;
  }

  const value = Number(match[1]);
  const unit = match[2].toLowerCase();
  if (unit === "s") {
    return value;
  }

  if (unit === "m") {
    return value * 60;
  }

  return value * 3600;
}

const stressSeconds = parseDurationToSeconds(cfg.stressDuration);
const stageUpSeconds = Math.max(30, Math.floor(stressSeconds / 3));
const stageHoldSeconds = Math.max(30, Math.floor(stressSeconds / 3));

export const options = {
  scenarios: {
    queue_graceful_degradation: {
      executor: "ramping-vus",
      startVUs: cfg.stressStartVus,
      stages: [
        { duration: `${stageUpSeconds}s`, target: cfg.stressPeakVus },
        { duration: `${stageHoldSeconds}s`, target: cfg.stressPeakVus },
        { duration: `${stageUpSeconds}s`, target: cfg.stressStartVus },
      ],
      gracefulRampDown: "20s",
      tags: { scenario: "queue_graceful_degradation" },
    },
  },
  thresholds: {
    http_req_duration: [`p(95)<${cfg.stressP95Ms}`],
    http_req_failed: [`rate<${cfg.stressErrorRate}`],
    business_failures: ["rate<0.05"],
    server_errors: ["rate<0.01"],
    "http_reqs{scenario:queue_graceful_degradation}": [`rate>${cfg.stressMinReqRatePerSec}`],
  },
};

export function setup() {
  const tokens = loginUsers(cfg.baseUrl, cfg.tenantId, users);
  return { tokens };
}

export default function (data) {
  const token = pickByVu(data.tokens);
  const headers = authHeaders(token, cfg.tenantId);

  const statusResponse = http.get(
    `${cfg.baseUrl}/api/queues/${cfg.queueId}/status`,
    {
      headers,
      tags: { endpoint: "queue_status" },
      responseCallback: http.expectedStatuses(200),
    },
  );

  const statusOk = check(statusResponse, {
    "status call returns 200": (r) => r.status === 200,
  });
  businessFailures.add(!statusOk);
  serverErrors.add(statusResponse.status >= 500);

  const joinResponse = http.post(
    `${cfg.baseUrl}/api/queues/${cfg.queueId}/join`,
    null,
    {
      headers,
      tags: { endpoint: "queue_join" },
      responseCallback: http.expectedStatuses(200, 409),
    },
  );

  const joinOk = check(joinResponse, {
    "join status is 200 or 409": (r) => r.status === 200 || r.status === 409,
  });
  businessFailures.add(!joinOk);
  serverErrors.add(joinResponse.status >= 500);

  // Keep a short pacing delay so the test models repeated user polling and joins.
  sleep(0.6);
}
