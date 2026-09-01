import { GaugeCircle, PlugZap, type LucideIcon } from "lucide-react";

/**
 * The two application examples, described once.
 *
 * Both screens render the same account domain, which is exactly the point: one
 * owns its data locally and mutates it, the other only reads over the wire. The
 * grouped route, its switcher and the comparison table all read this list, so
 * the distinction is stated in a single place.
 *
 * Labels and copy are resolved through the "admin" localization namespace at
 * render time (`views.<id>.*`); this module only carries the stable ids.
 */

export interface ApplicationView {
  readonly to: "/applications/admin" | "/applications/api";
  readonly id: "admin" | "api";
  readonly icon: LucideIcon;
}

export const applicationViews: readonly ApplicationView[] = [
  {
    to: "/applications/admin",
    id: "admin",
    icon: GaugeCircle,
  },
  {
    to: "/applications/api",
    id: "api",
    icon: PlugZap,
  },
];
