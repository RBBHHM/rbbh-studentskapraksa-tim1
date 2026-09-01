/**
 * Contribution conventions, as data.
 *
 * These are the answers a developer needs in their first hour: where a new file
 * goes, what may live inside it, and what is forbidden there. Both the
 * architecture page and the documentation page render from this file, so the
 * shape of the rules is stated once and cannot drift between them.
 *
 * Every field here is either a stable structural id (resolved to copy via
 * `t()` at render time, in each page's own namespace) or a code identifier —
 * a file path, a naming pattern or an example file — which is never
 * translated.
 */

export interface PlacementRule {
  /** Translation key segment: `placementRules.<key>.*` */
  readonly key: string;
  /** The directory the rule governs. */
  readonly path: string;
  /** Naming pattern new files must follow. Technical, not translated. */
  readonly naming: string;
  /** A real file to copy the shape from. */
  readonly example: string;
}

export const placementRules: readonly PlacementRule[] = [
  {
    key: "styles",
    path: "src/styles.css",
    naming: "--rbi-* for primitives, --surface|text|border|action|feedback-* for semantics",
    example: "src/styles.css",
  },
  {
    key: "designSystem",
    path: "src/design-system/",
    naming: "tokens/<topic>.ts · foundations/<topic>.ts",
    example: "src/design-system/tokens/colors.ts",
  },
  {
    key: "uiComponents",
    path: "src/components/ui/",
    naming: "kebab-case file, PascalCase export, variants via cva",
    example: "src/components/ui/stat.tsx",
  },
  {
    key: "domainComponents",
    path: "src/components/<domain>/",
    naming: "<domain>/<thing>-<role>.tsx, e.g. admin/account-form-dialog.tsx",
    example: "src/components/admin/account-form-dialog.tsx",
  },
  {
    key: "layoutComponents",
    path: "src/components/layout/",
    naming: "One structural concern per file",
    example: "src/components/layout/app-shell.tsx",
  },
  {
    key: "libDomain",
    path: "src/lib/<domain>/",
    naming: "use-<thing>.ts for hooks, <thing>-presentation.ts for labels and tones",
    example: "src/lib/admin/use-account-directory.ts",
  },
  {
    key: "libApi",
    path: "src/lib/api/",
    naming: "generated/api.ts comes from OpenAPI; <domain>-client.ts exposes readable operations",
    example: "src/lib/api/example-client.ts",
  },
  {
    key: "localization",
    path: "src/localization/",
    naming: "source/<locale>/<namespace>.json for wording, index.ts is the only public import",
    example: "src/localization/source/en/common.json",
  },
  {
    key: "routes",
    path: "src/routes/",
    naming: "dots become slashes: applications.admin.tsx → /applications/admin",
    example: "src/routes/applications.admin.tsx",
  },
  {
    key: "serverEndpoints",
    path: "Server/Examples/",
    naming: "<Feature>Endpoints.cs, grouped by neutral example feature",
    example: "Server/Examples/AccountExampleEndpoints.cs",
  },
];

export interface WorkflowStep {
  /** Translation key segment: `addComponentSteps.<key>.*` */
  readonly key: string;
}

/** The recipe for adding a component, in the order the files should be touched. */
export const addComponentSteps: readonly WorkflowStep[] = [
  { key: "checkTokens" },
  { key: "createPrimitive" },
  { key: "keepCopyOut" },
  { key: "moveLogicToLib" },
  { key: "documentInCatalog" },
  { key: "checkBeforeReview" },
];

export interface ConventionRule {
  /** Translation key segment: `codeConventions.<key>.*` */
  readonly key: string;
}

/** Non-negotiables. Consistency is the feature — every file reads the same way. */
export const codeConventions: readonly ConventionRule[] = [
  { key: "semanticTokensOnly" },
  { key: "noHardcodedCopy" },
  { key: "noBusinessLogicInUi" },
  { key: "singleSourceOfTruth" },
  { key: "dataFirstMarkupSecond" },
  { key: "deterministicFormatting" },
  { key: "mechanicalNaming" },
  { key: "accessibilityPartOfDone" },
];

export interface SetupStep {
  /** Translation key segment: `sections.setup.steps.<key>.*` */
  readonly key: string;
  /** The command or the file to touch. Technical, not translated. */
  readonly target: string;
}

/**
 * First-hour checklist after a `git clone`, in order. Deliberately short: the
 * long form lives in docs/onboarding.md.
 */
export const setupSteps: readonly SetupStep[] = [
  { key: "install", target: "pnpm install" },
  { key: "brandColours", target: "src/styles.css" },
  { key: "brandAssets", target: "src/assets/fonts/ · src/assets/logos/" },
  { key: "wording", target: "src/localization/source/en/*.json" },
  { key: "backend", target: "Server/ · src/lib/api/generated/ · pnpm openapi:generate" },
  { key: "removeDemos", target: "src/routes/applications*.tsx · src/components/catalog/" },
  { key: "verify", target: "pnpm check && dotnet build" },
];

export interface MobileTrack {
  /** Translation key segment: `sections.mobile.tracks.<key>.*` */
  readonly key: string;
  /** Packages or files the track adds. Technical, not translated. */
  readonly adds: string;
}

/** What to add when the target is a mobile app rather than a website. */
export const mobileTracks: readonly MobileTrack[] = [
  { key: "responsive", adds: "already included" },
  { key: "pwa", adds: "public/manifest.webmanifest · service worker · safe-area padding" },
  { key: "native", adds: "expo · nativewind · expo-router · expo-localization" },
];
