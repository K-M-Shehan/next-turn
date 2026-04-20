import { fail } from "k6";

const scenario = (__ENV.NT_SCENARIO || "queue").toLowerCase();

if (scenario !== "queue" && scenario !== "staff" && scenario !== "appointment") {
  fail("NT_SCENARIO must be one of: queue, staff, appointment");
}

// This file intentionally documents available scripts. Run one script at a time:
// k6 run tests/load/scenarios/queue-join-view.js
// k6 run tests/load/scenarios/staff-serve-skip.js
// k6 run tests/load/scenarios/appointment-booking-spike.js
export default function () {}
