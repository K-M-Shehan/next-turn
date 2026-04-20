import http from "k6/http";
import { check, sleep } from "k6";
import { env, parseUsers, requireEnvValue } from "../lib/env.js";
import { loginUsers } from "../lib/auth.js";
import { authHeaders, businessFailures, defaultThresholds, isoDateOnly, pickByVu } from "../lib/common.js";

const cfg = env();
requireEnvValue(cfg.tenantId, "NT_TENANT_ID");
requireEnvValue(cfg.organisationId, "NT_ORGANISATION_ID");
requireEnvValue(cfg.appointmentProfileId, "NT_APPOINTMENT_PROFILE_ID");

const users = parseUsers(cfg.appointmentUsersJson, "NT_APPOINTMENT_USERS_JSON");

export const options = {
  scenarios: {
    appointment_booking_spike: {
      executor: "constant-vus",
      vus: cfg.appointmentVus,
      duration: cfg.appointmentDuration,
      gracefulStop: "20s",
      tags: { scenario: "appointment_booking_spike" },
    },
  },
  thresholds: defaultThresholds("appointment_booking_spike", cfg.p95Ms, cfg.errorRate, cfg.minReqRatePerSec),
};

export function setup() {
  const tokens = loginUsers(cfg.baseUrl, cfg.tenantId, users);
  return { tokens };
}

export default function (data) {
  const token = pickByVu(data.tokens);
  const headers = authHeaders(token, cfg.tenantId);

  const slotDate = new Date();
  slotDate.setUTCDate(slotDate.getUTCDate() + 1);
  const dateText = isoDateOnly(slotDate);

  const slotsResponse = http.get(
    `${cfg.baseUrl}/api/appointments/slots?organisationId=${cfg.organisationId}&appointmentProfileId=${cfg.appointmentProfileId}&date=${dateText}`,
    {
      headers,
      tags: { endpoint: "appointment_slots" },
    },
  );

  const slotsOk = check(slotsResponse, {
    "slots status is 200": (r) => r.status === 200,
    "slots response is array": (r) => Array.isArray(r.json()),
  });
  businessFailures.add(!slotsOk);

  if (slotsResponse.status !== 200) {
    sleep(1);
    return;
  }

  const slots = slotsResponse.json();
  if (!Array.isArray(slots) || slots.length === 0) {
    // Empty slots are valid when the schedule is full. We skip booking for this iteration.
    sleep(0.5);
    return;
  }

  const slot = slots[__ITER % slots.length];

  const bookingResponse = http.post(
    `${cfg.baseUrl}/api/appointments`,
    JSON.stringify({
      organisationId: cfg.organisationId,
      appointmentProfileId: cfg.appointmentProfileId,
      slotStart: slot.slotStart,
      slotEnd: slot.slotEnd,
    }),
    {
      headers,
      tags: { endpoint: "appointment_book" },
    },
  );

  const bookingOk = check(bookingResponse, {
    "booking status is 200 or 409": (r) => r.status === 200 || r.status === 409,
  });
  businessFailures.add(!bookingOk);

  sleep(1);
}
