import http from "k6/http";
import { check } from "k6";

export function loginUsers(baseUrl, tenantId, users, loginPath = "/api/auth/login") {
  const tokens = [];

  for (const user of users) {
    const response = http.post(
      `${baseUrl}${loginPath}`,
      JSON.stringify({ email: user.email, password: user.password }),
      {
        headers: {
          "Content-Type": "application/json",
          "X-Tenant-Id": tenantId,
        },
        tags: { endpoint: "auth_login" },
      },
    );

    const ok = check(response, {
      "login status is 200": (r) => r.status === 200,
      "login returns token": (r) => {
        const body = r.json();
        return !!body && typeof body.accessToken === "string" && body.accessToken.length > 20;
      },
    });

    if (!ok) {
      throw new Error(`Login failed for ${user.email}. Status=${response.status}, Body=${response.body}`);
    }

    tokens.push(response.json("accessToken"));
  }

  return tokens;
}
