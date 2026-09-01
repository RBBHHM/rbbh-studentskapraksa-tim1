import type common from "../source/en/common.json";
import type navigation from "../source/en/navigation.json";
import type forms from "../source/en/forms.json";
import type validation from "../source/en/validation.json";
import type errors from "../source/en/errors.json";
import type uiLibrary from "../source/en/ui-library.json";
import type apiDemo from "../source/en/api-demo.json";
import type accessibility from "../source/en/accessibility.json";
import type dateTime from "../source/en/date-time.json";
import type overview from "../source/en/overview.json";
import type foundations from "../source/en/foundations.json";
import type components from "../source/en/components.json";
import type patterns from "../source/en/patterns.json";
import type architecture from "../source/en/architecture.json";
import type admin from "../source/en/admin.json";
import type registry from "../source/en/registry.json";

/**
 * Type-safe translation keys derived from the SOURCE locale bundles.
 *
 * These are types only — no JSON is bundled into the application. The source
 * files exist so the compiler knows which keys exist; the values shipped to
 * users always come from the runtime release.
 */

export interface LocalizationResources {
  readonly common: typeof common;
  readonly navigation: typeof navigation;
  readonly forms: typeof forms;
  readonly validation: typeof validation;
  readonly errors: typeof errors;
  readonly "ui-library": typeof uiLibrary;
  readonly "api-demo": typeof apiDemo;
  readonly accessibility: typeof accessibility;
  readonly "date-time": typeof dateTime;
  readonly overview: typeof overview;
  readonly foundations: typeof foundations;
  readonly components: typeof components;
  readonly patterns: typeof patterns;
  readonly architecture: typeof architecture;
  readonly admin: typeof admin;
  readonly registry: typeof registry;
}

export type ResourceNamespace = keyof LocalizationResources;

/** Dotted leaf paths of a bundle: `{ a: { b: string } }` → `"a.b"`. */
export type LeafKeys<T> = T extends string
  ? never
  : {
      [K in keyof T & string]: T[K] extends string ? K : `${K}.${LeafKeys<T[K]>}`;
    }[keyof T & string];

export type TranslationKey<N extends ResourceNamespace> = LeafKeys<LocalizationResources[N]>;

/** Fully qualified key, e.g. `navigation:primary.overview`. */
export type QualifiedTranslationKey = {
  [N in ResourceNamespace]: `${N}:${TranslationKey<N>}`;
}[ResourceNamespace];
