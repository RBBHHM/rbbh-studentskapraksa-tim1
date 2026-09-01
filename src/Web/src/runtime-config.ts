export interface RuntimeConfig {
  readonly API_BASE_URL?: string;
  readonly KEYCLOAK_URL?: string;
  readonly KEYCLOAK_REALM?: string;
  readonly KEYCLOAK_CLIENT_ID?: string;
  readonly LOCALIZATION_MANIFEST_URL?: string;
  readonly LOCALIZATION_REQUEST_TIMEOUT_MS?: string;
  readonly LOCALIZATION_MANIFEST_REVALIDATION_MS?: string;
  readonly ENABLE_PSEUDO_LOCALE?: string;
  readonly LOCALIZATION_DEBUG?: string;
  readonly ENVIRONMENT?: string;
}

declare global {
  interface Window {
    __APP_CONFIG__?: RuntimeConfig;
  }
}

export const runtimeConfig: RuntimeConfig = window.__APP_CONFIG__ ?? {};
