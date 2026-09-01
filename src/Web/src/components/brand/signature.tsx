import { useTranslation } from "react-i18next";

import { cn } from "@/lib/utils";

const signatureUrl = "/assets/potpis-hana.png";

/**
 * Author signature block.
 *
 * A hand-signed credit for the person who designed and built this template.
 * The image is the real signature, so it carries an accessible name; the note
 * beside it is plain text and never relies on the image to be understood.
 */
export function AuthorSignature({
  className,
  tone = "default",
}: {
  className?: string;
  tone?: "default" | "inverse";
}) {
  const { t } = useTranslation("common");

  return (
    <figure
      className={cn(
        "m-0 flex flex-col gap-3 border-t-3 border-t-border-brand pt-6 sm:flex-row sm:items-center sm:gap-8",
        className,
      )}
    >
      <img
        src={signatureUrl}
        width={600}
        height={200}
        loading="lazy"
        alt={t("footer.signatureAlt")}
        className={cn(
          "h-32 w-auto shrink-0 object-contain sm:h-14",
          tone === "inverse" && "invert",
        )}
      />
      <figcaption className="text-sm text-text-secondary">
        <span className="block font-bold text-text-primary">{t("footer.signatureCredit")}</span>
        {t("footer.signatureNote")}
      </figcaption>
    </figure>
  );
}
