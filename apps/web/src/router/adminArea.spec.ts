import { describe, expect, it } from "vitest";
import { createRouter, createMemoryHistory, type RouteRecordRaw } from "vue-router";
import { ADMIN_BASE, registerAdminArea, splitByArea } from "./adminArea";

const page = { template: "<div />" };

const contributed: RouteRecordRaw[] = [
  { path: "/login", name: "login", component: page },
  { path: "/dashboard", name: "dashboard", component: page, meta: { requiresAuth: true } },
  { path: "/users", name: "users", component: page, meta: { requiresAuth: true } },
];

function router(routes: RouteRecordRaw[]) {
  return createRouter({ history: createMemoryHistory(), routes });
}

describe("area split", () => {
  it("keeps routes anyone may open at the root", () => {
    const { publicRoutes } = splitByArea(contributed);

    expect(publicRoutes.map((route) => route.path)).toEqual(["/login"]);
  });

  it("rebases everything requiring a session under the private base", () => {
    const { adminRoutes } = splitByArea(contributed);

    // A public page added later can then never shadow an admin screen.
    expect(adminRoutes.map((route) => route.path)).toEqual([
      `${ADMIN_BASE}/dashboard`,
      `${ADMIN_BASE}/users`,
    ]);
  });
});

describe("private area registration", () => {
  it("does not exist until it is registered", () => {
    const { publicRoutes } = splitByArea(contributed);
    const instance = router(publicRoutes);

    // The failure this guards against: `meta.requiresAuth` is enforced by the
    // Auth module's guard and by nothing else, so an app assembled without
    // that module must not carry administrative screens at all. Registering
    // them requires proof the guard exists, which is why this test can only
    // reach this state by NOT calling registerAdminArea.
    expect(instance.resolve(`${ADMIN_BASE}/users`).matched).toHaveLength(0);
    expect(instance.resolve("/login").matched).toHaveLength(1);
  });

  it("adds the private routes once the guard is installed", () => {
    const { publicRoutes, adminRoutes } = splitByArea(contributed);
    const instance = router(publicRoutes);

    registerAdminArea(instance, adminRoutes, { guardInstalled: true });

    expect(instance.resolve(`${ADMIN_BASE}/users`).matched).toHaveLength(1);
  });

  it("sends the root into the private area only once that area exists", () => {
    const { publicRoutes, adminRoutes } = splitByArea(contributed);
    const instance = router(publicRoutes);

    expect(instance.resolve("/").matched).toHaveLength(0);

    registerAdminArea(instance, adminRoutes, { guardInstalled: true });

    expect(instance.resolve("/").redirectedFrom).toBeUndefined();
    expect(instance.resolve("/").matched[0]?.redirect).toBe(ADMIN_BASE);
  });
});
