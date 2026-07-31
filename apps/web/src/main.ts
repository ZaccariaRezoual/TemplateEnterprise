import { installAuthModule, useSessionStore } from "@enterprise/module-auth";
import { installAuthorizationModule, syncPermissions } from "@enterprise/module-authorization";
import { installUsersModule } from "@enterprise/module-users";
import { VueQueryPlugin } from "@tanstack/vue-query";
import { installTheme } from "@enterprise/ui";
import { createPinia } from "pinia";
import { createApp, watch } from "vue";
import App from "@/app/App.vue";
import { createQueryClient } from "@/app/providers/queryClient";
import { api, setAuthTokenProvider, setUnauthorizedHandler } from "@/core/api/apiClient";
import { logger } from "@/core/logger/logger";
import { createAppRouter } from "@/router";
import "@/assets/styles/main.css";

/**
 * Application bootstrap (composition root).
 *
 * The ONLY place that wires plugins and modules together: Pinia for client
 * state, TanStack Query for server state, the router, the theme engine and
 * the module frontends. Modules plug into seams core exposes — removing a
 * module means removing its install call and its routes import, nothing else.
 */
const app = createApp(App);

app.config.errorHandler = (error, _instance, info) => {
  logger.error("Unhandled Vue error", { error, info });
};

const router = createAppRouter();

app.use(createPinia());
app.use(router);
app.use(VueQueryPlugin, { queryClient: createQueryClient() });

// Modules (after Pinia: their stores activate at install time).
// Order matters: Auth installs the authentication guard, Authorization the
// permission guard, so an anonymous user is asked to sign in rather than told
// they lack permissions.
installAuthModule({ api, router, setAuthTokenProvider, setUnauthorizedHandler });
installAuthorizationModule({ app, router, api });
installUsersModule({ api });

// Permissions follow the session: fetched on sign-in, dropped on sign-out, so
// one user never inherits another's actions in the same tab.
const session = useSessionStore();
watch(
  () => session.isAuthenticated,
  (isAuthenticated) => {
    void syncPermissions(isAuthenticated);
  },
  { immediate: true },
);

installTheme();

app.mount("#app");
