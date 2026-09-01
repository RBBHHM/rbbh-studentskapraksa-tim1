import Keycloak from "keycloak-js";
import { runtimeConfig } from "../../runtime-config";

export const isAuthenticationConfigured = Boolean(
  runtimeConfig.KEYCLOAK_URL && runtimeConfig.KEYCLOAK_REALM && runtimeConfig.KEYCLOAK_CLIENT_ID,
);

export const keycloak = new Keycloak({
  url: runtimeConfig.KEYCLOAK_URL || "http://localhost",
  realm: runtimeConfig.KEYCLOAK_REALM || "not-configured",
  clientId: runtimeConfig.KEYCLOAK_CLIENT_ID || "not-configured",
});

let initialization: Promise<boolean> | undefined;
export function initializeAuthentication(): Promise<boolean> {
  if (!isAuthenticationConfigured) {
    console.warn("Keycloak nije konfigurisan; frontend radi u lokalnom razvojnom režimu.");
    return Promise.resolve(true);
  }
  initialization ??= keycloak.init({
    onLoad: "login-required",
    pkceMethod: "S256",
    checkLoginIframe: false,
  });
  return initialization as Promise<boolean>;
}
export async function getAccessToken() {
  if (!isAuthenticationConfigured) return undefined;
  await initializeAuthentication();
  await keycloak.updateToken(30);
  if (!keycloak.token) {
    await keycloak.login({ redirectUri: window.location.href });
    throw new Error("Authentication redirect started.");
  }
  return keycloak.token;
}
