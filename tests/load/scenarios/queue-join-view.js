import http from "k6/http";
import { check, sleep } from "k6";
import { env, parseUsers, requireEnvValue } from "../lib/env.js";
import { loginUsers } from "../lib/auth.js";
import { authHeaders, businessFailures, defaultThresholds, pickByVu } from "../lib/common.js";

const cfg = env();
requireEnvValue(cfg.tenantId, "NT_TENANT_ID");
requireEnvValue(cfg.queueId, "NT_QUEUE_ID");

const users = parseUsers(cfg.queueUsersJson, "NT_QUEUE_USERS_JSON");

export const options = {
  scenarios: {
    queue_join_view: {
      executor: "constant-vus",
      vus: cfg.queueVus,
      duration: cfg.queueDuration,
      gracefulStop: "20s",
      tags: { scenario: "queue_join_view" },
    },
  },
  thresholds: defaultThresholds("queue_join_view", cfg.p95Ms, cfg.errorRate, cfg.minReqRatePerSec),
};

export function setup() {
  const tokens = loginUsers(cfg.baseUrl, cfg.tenantId, users);
  return { tokens };
}

export default function (data) {
  const token = pickByVu(data.tokens);
  const headers = authHeaders(token, cfg.tenantId);

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

  sleep(1);
}
