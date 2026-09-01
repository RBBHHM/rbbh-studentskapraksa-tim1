import {
  AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent,
  AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle,
} from "./alert-dialog";

export function ConfirmDialog({ open, title, description, cancelLabel, confirmLabel, destructive, onCancel, onConfirm }: {
  open: boolean; title: string; description: string; cancelLabel: string; confirmLabel: string;
  destructive?: boolean; onCancel: () => void; onConfirm: () => void;
}) {
  return <AlertDialog open={open} onOpenChange={(value) => !value && onCancel()}>
    <AlertDialogContent>
      <AlertDialogHeader><AlertDialogTitle>{title}</AlertDialogTitle><AlertDialogDescription>{description}</AlertDialogDescription></AlertDialogHeader>
      <AlertDialogFooter>
        <AlertDialogCancel>{cancelLabel}</AlertDialogCancel>
        <AlertDialogAction className={destructive ? "!bg-[var(--action-destructive-background)] !text-[var(--action-destructive-foreground)] hover:!bg-[var(--action-destructive-background-hover)]" : undefined} onClick={onConfirm}>{confirmLabel}</AlertDialogAction>
      </AlertDialogFooter>
    </AlertDialogContent>
  </AlertDialog>;
}
