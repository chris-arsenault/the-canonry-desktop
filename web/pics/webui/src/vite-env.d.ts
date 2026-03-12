/// <reference types="vite/client" />

/** Intentional optionality — env var may not be set. See ADR-045. */
type Optional<T> = T | undefined;

interface ImportMetaEnv {
  readonly VITE_CATALOG_URL: Optional<string>;
}
