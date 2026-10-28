# RBI.DesignSystem

## Purpose

Reusable RBI-branded Blazor primitives, semantic tokens and static assets. No HTTP, auth, localization provider or business DTO dependencies.

## Source integration

This repository builds this library through ProjectReference; see ../SOURCE.md.

## Minimal setup

Load `_content/RBI.DesignSystem/rbi-design-system.css` and the package theme script. Import the component namespaces and compose normal Razor markup:

```razor
@using RBI.DesignSystem.Components.Button
<RbiButton>Save</RbiButton>
```

## Key contract

Typed inputs participate in standard EditForm/EditContext; controls expose semantic labels and controlled X/XChanged state. Tabs use a keyboard model; pagination is UI-only.

## Production caveats

`RbiDrawer` is a bottom modal with its own native pointer/touch drag and fractional snap state; it is not an alias for Sheet. Supply localized title/description, trigger and close/footer fragments. Background scaling is optional: only a caller-marked `[data-rbi-drawer-background]` element is transformed, and previous inline styles are restored. `Open`/`OpenChanged` and `ActiveSnapIndex`/`ActiveSnapIndexChanged` support controlled ownership; do not put application submission or persistence in the drawer.

`RbiSidebar` owns layout state, not application navigation. Compose semantic `RbiSidebarPart` lists/groups, `RbiSidebarItem`, actions, toggle/rail, bound `RbiSidebarInput` and skeleton; supply application content through `MainContent` (including your own semantic `main` if appropriate). It switches to a mobile Sheet below 768px and supports Ctrl/Cmd+B. The optional `StateCookieName` is a seven-day UI preference write, not authentication/business storage; null disables it. Applications supply initial `Open` if they want to read that preference. Controlled callbacks remain authoritative: only accepted desktop state is saved, never a rejected proposal. This correction to Legacy behavior was explicitly approved on 2026-10-15; Legacy remains unchanged. Full source-state closure remains tracked in the migration matrix.

Charts are presentation-only: compose `RbiChart`, `RbiChartLegend` and `RbiChartTooltip` from `Components.Display`. Applications supply SVG geometry, accessible chart name/data description, formatted values and any custom entry/label templates; no chart runtime or business data model is included.

Notifications use a caller-owned `RbiToastQueue` and `RbiToaster` from `Components.Feedback`. Place one host in the desired application layout and supply localized `AccessibleLabel`/`CloseLabel`. Publish immutable `RbiToastMessage` values; the default duration is four seconds, `Duration = null` is sticky, and optional actions belong to the caller. Dispose the queue with its owning component/circuit scope, never share a UI queue as a cross-user singleton. The host unsubscribes/releases its pause lease on disposal without disposing a shared caller queue. Timers pause while the document is hidden or pointer/focus remains inside; hiding does not expand the stack. Focus protection also survives Escape collapsing the stack: this was explicitly approved on 2026-10-15 instead of Legacy's expiry under focused actions. With multiple hosts, Alt+T keeps the focused host or otherwise uses the first connected eligible host, without moving focus twice. No persistence or application notification service is provided. Full legacy notification-state parity is still tracked in the migration matrix.

Applications own localization, accessibility labels and business behavior. Test native dialog/popover behavior in your supported browser. Optional JS modules are loaded by the components; do not copy their source.

See [repository development guidance](https://github.com/mahmutovichana/rbi-design-core/blob/main/Docs/DEVELOPMENT.md), [deployment](https://github.com/mahmutovichana/rbi-design-core/blob/main/Docs/DEPLOYMENT.md) and [public API review](https://github.com/mahmutovichana/rbi-design-core/blob/main/Docs/PUBLIC-API.md).
