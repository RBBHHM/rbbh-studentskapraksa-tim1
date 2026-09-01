const semantic = {
  surface: "surface-default", "surface-subtle": "surface-subtle", "surface-muted": "surface-muted",
  "surface-raised": "surface-raised", "surface-sunken": "surface-sunken", "surface-inverse": "surface-inverse",
  "surface-brand": "surface-brand", "surface-brand-subtle": "surface-brand-subtle", "surface-corporate": "surface-corporate",
  "text-primary": "text-primary", "text-secondary": "text-secondary", "text-tertiary": "text-tertiary",
  "text-inverse": "text-inverse", "text-on-brand": "text-on-brand", "text-link": "text-link",
  "text-brand-accent": "text-brand-accent",
  "border-default": "border-default", "border-subtle": "border-subtle", "border-strong": "border-strong",
  "border-brand": "border-brand",
  "feedback-info": "feedback-info-foreground", "feedback-info-bg": "feedback-info-background", "feedback-info-border": "feedback-info-border",
  "feedback-success": "feedback-success-foreground", "feedback-success-bg": "feedback-success-background", "feedback-success-border": "feedback-success-border",
  "feedback-danger": "feedback-danger-foreground", "feedback-danger-bg": "feedback-danger-background", "feedback-danger-border": "feedback-danger-border",
  "feedback-warning": "feedback-warning-foreground", "feedback-warning-bg": "feedback-warning-background", "feedback-warning-border": "feedback-warning-border",
  background: "background", foreground: "foreground", card: "card", "card-foreground": "card-foreground",
  popover: "popover", "popover-foreground": "popover-foreground", primary: "primary",
  "primary-foreground": "primary-foreground", secondary: "secondary", "secondary-foreground": "secondary-foreground",
  muted: "muted", "muted-foreground": "muted-foreground", accent: "accent", "accent-foreground": "accent-foreground",
  destructive: "destructive", "destructive-foreground": "destructive-foreground", border: "border", input: "input", ring: "ring",
  sidebar: "sidebar", "sidebar-foreground": "sidebar-foreground", "sidebar-primary": "sidebar-primary",
  "sidebar-primary-foreground": "sidebar-primary-foreground", "sidebar-accent": "sidebar-accent",
  "sidebar-accent-foreground": "sidebar-accent-foreground", "sidebar-border": "sidebar-border", "sidebar-ring": "sidebar-ring",
};

module.exports = {
  content: ["./src/**/*.{ts,tsx}", "./public/index.html"],
  darkMode: ["class", '[data-theme="dark"]'],
  theme: {
    extend: {
      colors: Object.fromEntries(Object.entries(semantic).map(([name, variable]) => [name, `var(--${variable})`])),
      fontFamily: { sans: ["Amalia", "Segoe UI", "sans-serif"], brand: ["Amalia", "Segoe UI", "sans-serif"] },
      borderRadius: { xs: "var(--radii-xs)", sm: "var(--radii-sm)", md: "var(--radii-md)", lg: "var(--radii-lg)", pill: "var(--radii-pill)" },
      boxShadow: { xs: "var(--elevation-xs)", sm: "var(--elevation-sm)", md: "var(--elevation-md)", lg: "var(--elevation-lg)", overlay: "var(--elevation-overlay)" },
    },
  },
  plugins: [],
};
