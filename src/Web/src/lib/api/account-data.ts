import type { Account, AccountSegment } from "@/lib/api/contract";

/**
 * Reference data served by the API layer.
 *
 * In the ASP.NET Core project this is the EF Core seed set; here it is the same
 * payload shaped identically, so the front end exercises the real contract.
 */
export const accountSeed: readonly Account[] = [
  { id: "AC-1041", name: "Nordwind Logistik GmbH", iban: "AT61 1904 3002 3457 3201", currency: "EUR", segment: "Corporate", status: "active", balanceMinor: 4820133400, updatedAt: "2026-08-04T09:12:00Z" },
  { id: "AC-1042", name: "Danube Grain Trading a.s.", iban: "CZ65 0800 0000 1920 0014 5399", currency: "CZK", segment: "Corporate", status: "active", balanceMinor: 1290044100, updatedAt: "2026-08-05T14:41:00Z" },
  { id: "AC-1043", name: "Carpathia Energy S.A.", iban: "RO49 AAAA 1B31 0075 9384 0000", currency: "RON", segment: "Institutional", status: "review", balanceMinor: 7710298800, updatedAt: "2026-08-05T07:05:00Z" },
  { id: "AC-1044", name: "Adriatic Ports Holding", iban: "AT48 3200 0000 1234 5864", currency: "EUR", segment: "Institutional", status: "active", balanceMinor: 2035870000, updatedAt: "2026-08-03T16:20:00Z" },
  { id: "AC-1045", name: "Vienna Municipal Treasury", iban: "AT02 1200 0100 0023 4567", currency: "EUR", segment: "Treasury", status: "active", balanceMinor: 15984200000, updatedAt: "2026-08-06T06:55:00Z" },
  { id: "AC-1046", name: "Tatra Precision Works", iban: "CZ12 2010 0000 0022 0123 4567", currency: "CZK", segment: "Corporate", status: "blocked", balanceMinor: 44219900, updatedAt: "2026-07-29T11:31:00Z" },
  { id: "AC-1047", name: "Balaton Agrar Kft. (branch)", iban: "AT77 1420 0200 1011 0987", currency: "EUR", segment: "Corporate", status: "active", balanceMinor: 660430200, updatedAt: "2026-08-02T13:02:00Z" },
  { id: "AC-1048", name: "Bucharest Retail Park SRL", iban: "RO88 BBBB 2C44 0091 2233 0000", currency: "RON", segment: "Corporate", status: "review", balanceMinor: 3120774500, updatedAt: "2026-08-01T08:47:00Z" },
  { id: "AC-1049", name: "Alpine Reinsurance AG", iban: "AT19 1500 0011 2233 4455", currency: "EUR", segment: "Institutional", status: "active", balanceMinor: 9048115000, updatedAt: "2026-08-05T19:10:00Z" },
  { id: "AC-1050", name: "Group Liquidity Buffer", iban: "AT90 1000 0000 0000 0001", currency: "EUR", segment: "Treasury", status: "active", balanceMinor: 27600000000, updatedAt: "2026-08-06T05:00:00Z" },
  { id: "AC-1051", name: "Moravia Rail Services", iban: "CZ33 0300 0000 0100 2233 4455", currency: "CZK", segment: "Corporate", status: "active", balanceMinor: 501230000, updatedAt: "2026-07-31T10:15:00Z" },
  { id: "AC-1052", name: "Sofia Bridge Consortium", iban: "AT55 1919 0020 0122 3344", currency: "EUR", segment: "Institutional", status: "active", balanceMinor: 1780500000, updatedAt: "2026-08-04T12:38:00Z" },
];

export const accountSegments: readonly AccountSegment[] = ["Corporate", "Institutional", "Treasury"];
