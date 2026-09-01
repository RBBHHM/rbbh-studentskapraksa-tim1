/**
 * File upload pattern data: a fixed set of rows showing every state the
 * dropzone can reach — queued, uploading, done and failed — so the pattern is
 * reviewable without wiring a real transfer.
 */

export type UploadStatus = "uploading" | "done" | "error";

export interface UploadEntry {
  readonly id: string;
  readonly name: string;
  readonly sizeLabel: string;
  readonly status: UploadStatus;
  readonly progress: number;
  readonly errorKey?: string;
}

export const uploadEntries: readonly UploadEntry[] = [
  { id: "u1", name: "Q2-statement.pdf", sizeLabel: "1.2 MB", status: "done", progress: 100 },
  { id: "u2", name: "board-mandate.docx", sizeLabel: "480 KB", status: "uploading", progress: 62 },
  { id: "u3", name: "beneficial-owners.xlsx", sizeLabel: "22.4 MB", status: "error", progress: 0, errorKey: "tooLarge" },
  { id: "u4", name: "passport-scan.png", sizeLabel: "3.1 MB", status: "error", progress: 0, errorKey: "unsupportedType" },
];

export const acceptedFileTypes = ["PDF", "DOCX", "XLSX", "PNG", "JPG"] as const;
export const maxFileSizeLabel = "10 MB";
