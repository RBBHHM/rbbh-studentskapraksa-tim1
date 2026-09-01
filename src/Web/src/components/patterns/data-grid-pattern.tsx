import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { ArrowDown, ArrowUp, ChevronLeft, ChevronRight, Search } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Text } from "@/components/ui/typography";
import { fetchAccounts } from "@/lib/api/accounts-client";
import { accountStatusMeta, formatMinor } from "@/lib/admin/account-presentation";
import type { AccountSortField } from "@/lib/api/contract";

const PAGE_SIZE = 5;
const sortableColumns: readonly { field: AccountSortField; labelKey: string }[] = [
  { field: "name", labelKey: "table.name" },
  { field: "segment", labelKey: "table.segment" },
  { field: "status", labelKey: "table.status" },
  { field: "balanceMinor", labelKey: "table.balance" },
];

/** Server-side paged, sorted and searched data grid, built on the accounts API contract. */
export function DataGridPattern() {
  const { t } = useTranslation("patterns");
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [sortBy, setSortBy] = useState<AccountSortField>("name");
  const [sortDir, setSortDir] = useState<"asc" | "desc">("asc");

  const query = useMemo(
    () => ({ page, pageSize: PAGE_SIZE, search, sortBy, sortDir }),
    [page, search, sortBy, sortDir],
  );

  const accounts = useQuery({
    queryKey: ["patterns-accounts", query],
    queryFn: ({ signal }) => fetchAccounts(query, signal),
    placeholderData: keepPreviousData,
  });

  function toggleSort(field: AccountSortField) {
    if (field === sortBy) setSortDir((d) => (d === "asc" ? "desc" : "asc"));
    else {
      setSortBy(field);
      setSortDir("asc");
    }
    setPage(1);
  }

  const data = accounts.data;

  return (
    <div className="w-full">
      <div className="relative max-w-xs">
        <Search aria-hidden="true" className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-text-tertiary" />
        <Input
          value={search}
          onChange={(event) => {
            setSearch(event.target.value);
            setPage(1);
          }}
          placeholder={t("dataGridPattern.searchPlaceholder")}
          className="pl-9"
        />
      </div>

      <div className="mt-4 overflow-x-auto rounded-sm border border-border-default">
        <Table>
          <TableHeader>
            <TableRow>
              {sortableColumns.map((column) => (
                <TableHead key={column.field}>
                  <button
                    type="button"
                    className="inline-flex cursor-pointer items-center gap-1 font-inherit"
                    onClick={() => toggleSort(column.field)}
                  >
                    {t(`dataGridPattern.${column.labelKey}` as never) as string}
                    {sortBy === column.field ? (
                      sortDir === "asc" ? (
                        <ArrowUp aria-hidden="true" className="size-3" />
                      ) : (
                        <ArrowDown aria-hidden="true" className="size-3" />
                      )
                    ) : null}
                  </button>
                </TableHead>
              ))}
            </TableRow>
          </TableHeader>
          <TableBody>
            {!data
              ? Array.from({ length: 4 }).map((_, index) => (
                  <TableRow key={index}>
                    <TableCell colSpan={4}>
                      <Skeleton className="h-6 w-full" />
                    </TableCell>
                  </TableRow>
                ))
              : data.items.map((account) => (
                  <TableRow key={account.id}>
                    <TableCell className="font-medium text-text-primary">{account.name}</TableCell>
                    <TableCell className="text-text-secondary">{account.segment}</TableCell>
                    <TableCell>
                      <Badge tone={accountStatusMeta[account.status].tone} withDot>
                        {account.status}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-right font-bold tabular-nums text-text-primary">
                      {formatMinor(account.balanceMinor, account.currency)}
                    </TableCell>
                  </TableRow>
                ))}
          </TableBody>
        </Table>

        {data ? (
          <nav className="flex items-center justify-between gap-3 border-t border-border-subtle px-4 py-3">
            <Text size="sm" tone="secondary">
              {t("dataGridPattern.pageOf", { page: data.page, totalPages: data.totalPages })}
            </Text>
            <div className="flex gap-2">
              <Button variant="secondary" size="sm" disabled={data.page <= 1} onClick={() => setPage((p) => p - 1)}>
                <ChevronLeft aria-hidden="true" />
              </Button>
              <Button
                variant="secondary"
                size="sm"
                disabled={data.page >= data.totalPages}
                onClick={() => setPage((p) => p + 1)}
              >
                <ChevronRight aria-hidden="true" />
              </Button>
            </div>
          </nav>
        ) : null}
      </div>
    </div>
  );
}
