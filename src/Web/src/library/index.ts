/**
 * `@rbi/design-system` — public library surface.
 *
 * This barrel is the *convenience* entry point. Every module is also reachable
 * through a deep import so consuming apps only pull in what they render:
 *
 *   import { Button } from "@rbi/design-system";              // barrel
 *   import { Button } from "@rbi/design-system/ui/button";    // single component
 *   import { spacingScale } from "@rbi/design-system/tokens"; // tokens only
 *
 * The build is side-effect free and emits one ES module per source file
 * (`preserveModules`), so bundlers tree-shake the barrel down to what is used.
 * Nothing site-specific (routes, localization wiring, catalogue pages, the
 * author signature) is exported — the library is UI + tokens only.
 */

/* -------------------------------------------------------------------------- */
/* Foundations                                                                */
/* -------------------------------------------------------------------------- */

export * from "@/design-system/tokens";
export * from "@/design-system/theme";
export * from "@/design-system/foundations/bento-presets";
export * from "@/design-system/foundations/logo-assets";
export * from "@/design-system/foundations/palette";
export * from "@/lib/color/contrast";
export { cn } from "@/lib/utils";
export { useIsMobile } from "@/hooks/use-mobile";

/* -------------------------------------------------------------------------- */
/* Brand                                                                      */
/* -------------------------------------------------------------------------- */

export * from "@/components/brand/rbi-logo";
export * from "@/components/brand/slogan";

/* -------------------------------------------------------------------------- */
/* Layout                                                                     */
/* -------------------------------------------------------------------------- */

export * from "@/components/layout/dock";
export * from "@/components/layout/primitives";

/* -------------------------------------------------------------------------- */
/* Patterns                                                                   */
/* -------------------------------------------------------------------------- */

export * from "@/components/patterns/app-page-patterns";

/* -------------------------------------------------------------------------- */
/* Components                                                                 */
/* -------------------------------------------------------------------------- */

export * from "@/components/ui/accordion";
export * from "@/components/ui/alert";
export * from "@/components/ui/alert-dialog";
export * from "@/components/ui/aspect-ratio";
export * from "@/components/ui/avatar";
export * from "@/components/ui/badge";
export * from "@/components/ui/bento-grid";
export * from "@/components/ui/breadcrumb";
export * from "@/components/ui/button";
export * from "@/components/ui/calendar";
export * from "@/components/ui/callout";
export * from "@/components/ui/card";
export * from "@/components/ui/carousel";
export * from "@/components/ui/chart";
export * from "@/components/ui/checkbox";
export * from "@/components/ui/collapsible";
export * from "@/components/ui/command";
export * from "@/components/ui/context-menu";
export * from "@/components/ui/dialog";
export * from "@/components/ui/drawer";
export * from "@/components/ui/dropdown-menu";
export * from "@/components/ui/form";
export * from "@/components/ui/hover-card";
export * from "@/components/ui/input";
export * from "@/components/ui/input-otp";
export * from "@/components/ui/label";
export * from "@/components/ui/menubar";
export * from "@/components/ui/navigation-menu";
export * from "@/components/ui/pagination";
export * from "@/components/ui/popover";
export * from "@/components/ui/progress";
export * from "@/components/ui/radio-group";
export * from "@/components/ui/resizable";
export * from "@/components/ui/scroll-area";
export * from "@/components/ui/select";
export * from "@/components/ui/separator";
export * from "@/components/ui/sheet";
export * from "@/components/ui/side-nav";
export * from "@/components/ui/sidebar";
export * from "@/components/ui/skeleton";
export * from "@/components/ui/slider";
export * from "@/components/ui/sonner";
export * from "@/components/ui/stat";
export * from "@/components/ui/switch";
export * from "@/components/ui/table";
export * from "@/components/ui/tabs";
export * from "@/components/ui/textarea";
export * from "@/components/ui/toggle";
export * from "@/components/ui/toggle-group";
export * from "@/components/ui/tooltip";
export * from "@/components/ui/typography";
