import { installAuthModule, useSessionStore } from "@enterprise/module-auth";
import { installAuthorizationModule, syncPermissions } from "@enterprise/module-authorization";
import { installLocalizationModule, loadLocale } from "@enterprise/module-localization";
import {
  installNotificationsModule,
  useNotificationsStore,
} from "@enterprise/module-notifications";
import {
  connectRealtime,
  disconnectRealtime,
  installRealtimeModule,
} from "@enterprise/module-realtime";
import { installUsersModule } from "@enterprise/module-users";
import { VueQueryPlugin } from "@tanstack/vue-query";
import { installTheme } from "@enterprise/ui";
import { createPinia } from "pinia";
import { createApp, watch } from "vue";
import App from "@/app/App.vue";
import { createQueryClient } from "@/app/providers/queryClient";
import { api, setAuthTokenProvider, setUnauthorizedHandler } from "@/core/api/apiClient";
import { installFeatureFlags, useFeatureFlagsStore } from "@/core/features/featureFlags";
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

// Stores the composition root itself observes (safe after Pinia is installed).
const session = useSessionStore();
const notifications = useNotificationsStore();

// Feature flags are core, not a module: anything may read them, and they are
// fetched before sign-in because a flag may govern the sign-in screen itself.
installFeatureFlags(app);
void useFeatureFlagsStore().load();

// Modules (after Pinia: their stores activate at install time).
// Order matters: Auth installs the authentication guard, Authorization the
// permission guard, so an anonymous user is asked to sign in rather than told
// they lack permissions.
installAuthModule({ api, router, setAuthTokenProvider, setUnauthorizedHandler });
installAuthorizationModule({ app, router, api });
installUsersModule({ api });
installNotificationsModule({ api });
installLocalizationModule({ app, api });
// A function, not the token: SignalR calls it again on every reconnect, so a
// session that refreshed while offline reconnects with the current token.
installRealtimeModule({ getAccessToken: () => session.accessToken });

// Translations are fetched, not bundled: the app paints first and gets its
// strings a moment later.
void loadLocale();

// Per-user state follows the session: loaded on sign-in, dropped on sign-out,
// so one user never inherits another's permissions, notifications or — worse
// — an open socket still receiving their events.
watch(
  () => session.isAuthenticated,
  (isAuthenticated) => {
    void syncPermissions(isAuthenticated);
    if (isAuthenticated) {
      void notifications.load();
      void connectRealtime();
    } else {
      notifications.clear();
      void disconnectRealtime();
    }
  },
  { immediate: true },
);

installTheme();

app.mount("#app");
