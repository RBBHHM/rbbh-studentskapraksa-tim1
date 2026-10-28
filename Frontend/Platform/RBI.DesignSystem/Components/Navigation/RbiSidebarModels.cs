using Microsoft.AspNetCore.Components;

namespace RBI.DesignSystem.Components.Navigation;

public enum RbiSidebarSide { Left, Right }
public enum RbiSidebarVariant { Sidebar, Floating, Inset }
public enum RbiSidebarCollapse { Offcanvas, Icon, None }
public enum RbiSidebarItemSize { Default, Small, Large }
public enum RbiSidebarItemVariant { Default, Outline }
public enum RbiSidebarPartKind { Header, Footer, Content, Group, GroupLabel, GroupContent, Menu, MenuItem, SubMenu, SubMenuItem, Badge, Separator }

// One native layout owner; descendants receive immutable presentation, never a public React-like store.
internal sealed record RbiSidebarState(bool Collapsed, bool Mobile, bool Expanded, string PanelId, string ToggleLabel, EventCallback Toggle);
