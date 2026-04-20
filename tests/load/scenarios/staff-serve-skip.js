import http from "k6/http";
import { check, sleep } from "k6";
import { env, parseUsers, requireEnvValue } from "../lib/env.js";
import { loginUsers } from "../lib/auth.js";
import { authHeaders, businessFailures, defaultThresholds, pickByVu } from "../lib/common.js";

const cfg = env();
requireEnvValue(cfg.tenantId, "NT_TENANT_ID");
requireEnvValue(cfg.queueId, "NT_QUEUE_ID");

const users = parseUsers(cfg.staffUsersJson, "NT_STAFF_USERS_JSON");

export const options = {
  scenarios: {
    staff_serve_skip: {
      executor: "constant-vus",
      vus: cfg.staffVus,
      duration: cfg.staffDuration,
      gracefulStop: "20s",
      tags: { scenario: "staff_serve_skip" },
    },
  },
  thresholds: defaultThresholds("staff_serve_skip", cfg.p95Ms, cfg.errorRate, cfg.minReqRatePerSec),
};

export function setup() {
  const tokens = loginUsers(cfg.baseUrl, cfg.tenantId, users);
  return { tokens };
}

export default function (data) {
  const token = pickByVu(data.tokens);
  const headers = authHeaders(token, cfg.tenantId);

  const callNextResponse = http.post(
    `${cfg.baseUrl}/api/queues/${cfg.queueId}/call-next`,
    null,
    {
      headers,
      tags: { endpoint: "call_next" },
    },
  );

  const callNextOk = check(callNextResponse, {
    "call-next status is 200 or 400": (r) => r.status === 200 || r.status === 400,
  });
  businessFailures.add(!callNextOk);

  const shouldServe = __ITER % 2 === 0;
  const endpoint = shouldServe ? "served" : "skip";
  const body = shouldServe ? null : JSON.stringify({ reason: cfg.skipReason });

  const finalizeResponse = http.post(
    `${cfg.baseUrl}/api/queues/${cfg.queueId}/${endpoint}`,
    body,
    {
      headers,
      tags: { endpoint: shouldServe ? "mark_served" : "skip" },
    },
  );

  const finalizeOk = check(finalizeResponse, {
    "served/skip status is 200 or 400": (r) => r.status === 200 || r.status === 400,
  });
  businessFailures.add(!finalizeOk);

  sleep(0.8);
}
