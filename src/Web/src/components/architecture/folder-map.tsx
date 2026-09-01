import { Folder, FileCode2 } from "lucide-react";

import { useSafeTranslation } from "@/localization";
import { folders } from "@/lib/architecture/structure";

/**
 * Folder map — the repository drawn as a tree.
 *
 * Presentation only: the nodes come from src/lib/architecture/structure.ts and
 * every description is resolved from the "architecture" namespace.
 */
export function FolderMap() {
  const { text } = useSafeTranslation("architecture");

  return (
    <div className="overflow-hidden rounded-sm border border-border-default">
      <ul className="divide-y divide-border-subtle">
        {folders.map((entry, index) => {
          const isFile = entry.path.includes(".");
          const Icon = isFile ? FileCode2 : Folder;
          return (
            <li
              key={`${entry.path}-${index}`}
              className="flex flex-col gap-1 px-4 py-2.5 sm:flex-row sm:items-baseline sm:gap-6"
            >
              <span
                className="flex min-w-0 items-center gap-2 sm:w-80 sm:shrink-0"
                style={{ paddingInlineStart: `${entry.depth * 1.25}rem` }}
              >
                {entry.depth > 0 ? (
                  <span aria-hidden="true" className="font-mono text-2xs text-text-tertiary">
                    └
                  </span>
                ) : null}
                <Icon aria-hidden="true" className="size-3.5 shrink-0 text-text-tertiary" />
                <code className="truncate font-mono text-xs font-bold text-text-primary">
                  {entry.path}
                </code>
              </span>
              <span className="text-sm text-text-secondary">
                {text(`structure.folders.${entry.key}`)}
              </span>
            </li>
          );
        })}
      </ul>
    </div>
  );
}
