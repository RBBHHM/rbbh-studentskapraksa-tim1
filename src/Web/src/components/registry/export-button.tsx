import { Download } from "lucide-react";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import { apiClient, apiErrorMessage } from "@/lib/api/http-client";

export function ExportButton({ endpoint, fileName }: { readonly endpoint: string; readonly fileName: string }) {
  const { i18n } = useTranslation();
  const bs = i18n.language.startsWith("bs");
  const [pending, setPending] = useState(false);

  async function exportFile() {
    try {
      setPending(true);
      const response = await apiClient.download(endpoint);
      const blob = await response.blob();
      const url = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = fileName;
      document.body.append(link);
      link.click();
      link.remove();
      URL.revokeObjectURL(url);
      toast.success(bs ? "Excel izvoz je preuzet." : "Excel export downloaded.");
    } catch (error) {
      toast.error(apiErrorMessage(error, bs ? "Excel izvoz nije uspio." : "Excel export failed."));
    } finally {
      setPending(false);
    }
  }

  return <Button type="button" variant="secondary" disabled={pending} onClick={exportFile}><Download className="size-4" />{pending ? (bs ? "Izvoz…" : "Exporting…") : (bs ? "Izvezi Excel" : "Export Excel")}</Button>;
}
