import { Toaster as Sonner } from "sonner";

type ToasterProps = React.ComponentProps<typeof Sonner>;

const Toaster = ({ ...props }: ToasterProps) => {
  return (
    <Sonner
      className="toaster group"
      toastOptions={{
        classNames: {
          toast:
            "group toast group-[.toaster]:rounded-sm group-[.toaster]:border group-[.toaster]:border-border-default group-[.toaster]:bg-surface group-[.toaster]:font-brand group-[.toaster]:text-text-primary group-[.toaster]:shadow-lg",
          description: "group-[.toast]:text-text-secondary",
          actionButton: "group-[.toast]:bg-surface-brand group-[.toast]:text-text-on-brand",
          cancelButton: "group-[.toast]:bg-surface-muted group-[.toast]:text-text-secondary",
        },
      }}
      {...props}
    />
  );
};

export { Toaster };
