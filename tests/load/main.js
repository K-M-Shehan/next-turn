import { fail } from "k6";

const scenario = (__ENV.NT_SCENARIO || "queue").toLowerCase();

if (scenario !== "queue" && scenario !== "staff" && scenario !== "appointment" && scenario !== "stress") {
  fail("NT_SCENARIO must be one of: queue, staff, appointment, stress");
}

// This file intentionally documents available scripts. Run one script at a time:
// k6 run tests/load/scenarios/queue-join-view.js
// k6 run tests/load/scenarios/staff-serve-skip.js
// k6 run tests/load/scenarios/appointment-booking-spike.js
// k6 run tests/load/scenarios/queue-graceful-degradation.js
export default function () {}
