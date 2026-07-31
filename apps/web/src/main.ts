import { VueQueryPlugin } from "@tanstack/vue-query";
import { createPinia } from "pinia";
import { createApp } from "vue";
import App from "@/app/App.vue";
import { createQueryClient } from "@/app/providers/queryClient";
import { logger } from "@/core/logger/logger";
import { createAppRouter } from "@/router";
import { installTheme } from "@/shared/composables/useTheme";
import "@/assets/styles/main.css";

/**
 * Application bootstrap (composition root).
 *
 * The ONLY place that wires plugins together: Pinia for client state, TanStack
 * Query for server state, the router, the theme engine and the global error
 * handler. Nothing else in the app creates these.
 */
const app = createApp(App);

app.config.errorHandler = (error, _instance, info) => {
  logger.error("Unhandled Vue error", { error, info });
};

app.use(createPinia());
app.use(createAppRouter());
app.use(VueQueryPlugin, { queryClient: createQueryClient() });

installTheme();

app.mount("#app");
