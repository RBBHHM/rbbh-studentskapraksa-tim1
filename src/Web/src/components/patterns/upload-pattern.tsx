import { useTranslation } from "react-i18next";
import { AlertCircle, Check, FileUp, Loader2 } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Progress } from "@/components/ui/progress";
import { Text } from "@/components/ui/typography";
import { acceptedFileTypes, maxFileSizeLabel, uploadEntries } from "@/lib/patterns/upload";

/** Drag-and-drop upload: dropzone hint plus a file list covering every transfer state. */
export function UploadPattern() {
  const { t } = useTranslation("patterns");

  return (
    <div className="w-full">
      <div className="rounded-sm border-2 border-dashed border-border-default p-10 text-center">
        <FileUp aria-hidden="true" className="mx-auto size-8 text-text-tertiary" />
        <Text size="sm" className="mt-3 font-medium text-text-primary">
          {t("uploadPattern.dropzoneTitle")}
        </Text>
        <Text size="sm" tone="tertiary" className="mt-1">
          {t("uploadPattern.dropzoneHint", { types: acceptedFileTypes.join(", "), size: maxFileSizeLabel })}
        </Text>
      </div>

      <ul className="mt-4 divide-y divide-border-subtle rounded-sm border border-border-default">
        {uploadEntries.map((entry) => (
          <li key={entry.id} className="flex items-center gap-3 px-4 py-3">
            {entry.status === "done" ? (
              <Check aria-hidden="true" className="size-4 shrink-0 text-feedback-success" />
            ) : entry.status === "error" ? (
              <AlertCircle aria-hidden="true" className="size-4 shrink-0 text-feedback-danger" />
            ) : (
              <Loader2 aria-hidden="true" className="size-4 shrink-0 animate-spin text-text-tertiary" />
            )}
            <div className="min-w-0 flex-1">
              <div className="flex items-center justify-between gap-2">
                <span className="truncate text-sm font-medium text-text-primary">{entry.name}</span>
                <span className="shrink-0 font-mono text-2xs text-text-tertiary">{entry.sizeLabel}</span>
              </div>
              {entry.status === "uploading" ? (
                <Progress value={entry.progress} className="mt-2 h-1.5" />
              ) : entry.status === "error" ? (
                <Text size="sm" tone="danger" className="mt-1">
                  {t(`uploadPattern.errors.${entry.errorKey}` as never) as string}
                </Text>
              ) : (
                <Badge tone="success" className="mt-1">
                  {t("uploadPattern.uploaded")}
                </Badge>
              )}
            </div>
          </li>
        ))}
      </ul>
    </div>
  );
}
