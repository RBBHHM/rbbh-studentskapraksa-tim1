import {
  Bell,
  Landmark,
  BookOpen,
  Building2,
  CalendarClock,
  FileChartColumn,
  Gauge,
  Scale,
  ScrollText,
  Users,
  UserRound,
  type LucideIcon,
} from "lucide-react";

export interface RegistryResource {
  readonly key: string;
  readonly path: string;
  readonly endpoint?: string;
  readonly icon: LucideIcon;
  readonly area: "work" | "administration";
  readonly capabilities?: ResourceCapabilities;
  readonly displayColumns?: readonly string[];
  readonly accessRole?: "physical-persons" | "legal-persons" | "limits" | "regulatory-reporting";
  readonly requiresAllAccesses?: boolean;
}

export interface ResourceField {
  readonly key: string;
  readonly labelBs: string;
  readonly labelEn: string;
  readonly type?: "text" | "number" | "date" | "checkbox" | "select" | "segmented" | "textarea";
  readonly required?: boolean;
  readonly minLength?: number;
  readonly maxLength?: number;
  readonly pattern?: string;
  readonly codeListCategory?: string;
  readonly lookupEndpoint?: string;
  readonly valueKind?: "string" | "number";
  readonly options?: readonly {
    readonly value: string | number | boolean;
    readonly labelBs: string;
    readonly labelEn: string;
    readonly sets?: Readonly<Record<string, unknown>>;
    readonly setsBs?: Readonly<Record<string, unknown>>;
    readonly setsEn?: Readonly<Record<string, unknown>>;
  }[];
}

export interface ResourceCapabilities {
  readonly fields: readonly ResourceField[];
  readonly create?: boolean;
  readonly update?: boolean;
  readonly remove?: boolean;
  readonly verifyPath?: string;
  readonly mutationEndpoint?: string;
}

const yesNoOptions = [
  { value: true, labelBs: "Da", labelEn: "Yes" },
  { value: false, labelBs: "Ne", labelEn: "No" },
] as const;

const connectionBases = [
  ["ZOB-2-U-2", "Zakon o bankama, član 2, paragraf u, odjeljak 2", "Banking Act, Article 2, paragraph u, section 2", "Lice sa najmanje 5% učešća u banci ili članu bankarske grupe i članovi njegove uže porodice.", "A person holding at least 5% in the bank or a banking-group member, including members of their immediate family."],
  ["ZOB-2-V-1", "Zakon o bankama, član 2, paragraf v, odjeljak 1", "Banking Act, Article 2, paragraph v, section 1", "Član bankarske grupe u kojoj je banka.", "A member of the banking group to which the bank belongs."],
  ["ZOB-2-V-3", "Zakon o bankama, član 2, paragraf v, odjeljak 3", "Banking Act, Article 2, paragraph v, section 3", "Pravno lice u kojem banka ima kvalifikovano učešće.", "A legal entity in which the bank has a qualifying holding."],
  ["ZOB-2-V-4", "Zakon o bankama, član 2, paragraf v, odjeljak 4", "Banking Act, Article 2, paragraph v, section 4", "Pravno lice u kojem član uprave, nadzornog odbora ili prokurista banke, odnosno član njegove uže porodice, ima kvalifikovano učešće.", "A legal entity in which a management or supervisory board member, authorised representative, or their immediate family member has a qualifying holding."],
  ["ZOB-2-V-5", "Zakon o bankama, član 2, paragraf v, odjeljak 5", "Banking Act, Article 2, paragraph v, section 5", "Član nadzornog odbora ili uprave banke, nosilac ključne funkcije, prokurista i članovi njihove uže porodice.", "A supervisory or management board member, key-function holder, authorised representative, and members of their immediate family."],
  ["ZOB-2-V-7", "Zakon o bankama, član 2, paragraf v, odjeljak 7", "Banking Act, Article 2, paragraph v, section 7", "Član organa upravljanja ili rukovođenja i prokurista člana bankarske grupe te članovi njihove uže porodice.", "A governing or management body member or authorised representative of a banking-group member, including their immediate family."],
  ["ZOB-2-V-8", "Zakon o bankama, član 2, paragraf v, odjeljak 8", "Banking Act, Article 2, paragraph v, section 8", "Lice čiji radni ili drugi odnos omogućava značajan uticaj na poslovanje banke ili predstavlja sukob interesa.", "A person whose employment or other relationship enables significant influence over the bank or creates a conflict of interest."],
] as const;

const legalConnectionOptions = connectionBases.map(([value, labelBs, labelEn, descriptionBs, descriptionEn]) => ({
  value,
  labelBs,
  labelEn,
  setsBs: { connectionDescription: descriptionBs },
  setsEn: { connectionDescription: descriptionEn },
}));
const physicalConnectionOptions = connectionBases.map(([value, labelBs, labelEn, descriptionBs, descriptionEn]) => ({
  value,
  labelBs,
  labelEn,
  setsBs: { relationDescription: descriptionBs },
  setsEn: { relationDescription: descriptionEn },
}));
const specialRelationshipOptions = [
  { value: "NADZORNI_ODBOR", labelBs: "Član nadzornog odbora banke", labelEn: "Bank supervisory board member" },
  { value: "UPRAVA", labelBs: "Član uprave banke", labelEn: "Bank management board member" },
  { value: "NKF", labelBs: "Nosilac ključne funkcije (NKF)", labelEn: "Key-function holder" },
  { value: "PROKURISTA", labelBs: "Prokurista banke", labelEn: "Bank authorised representative" },
  { value: "B1", labelBs: "B1", labelEn: "B1" },
  { value: "UZA_PORODICA", labelBs: "Član uže porodice povezanog lica", labelEn: "Immediate family member of a related party" },
] as const;

export const registryResources: readonly RegistryResource[] = [
  { key: "dashboard", path: "/app", icon: Gauge, area: "work" },
  {
    key: "legalPersons",
    path: "/app/legal-persons",
    endpoint: "/api/legal-entities",
    displayColumns: ["name", "isResident", "taxNumber", "maticniBroj", "fbaId", "basisOfConnection", "status"],
    icon: Building2,
    area: "work",
    accessRole: "legal-persons",
    capabilities: {
      create: true,
      update: true,
      remove: true,
      verifyPath: "/api/verification/legal-person/{id}",
      fields: [
        { key: "name", labelBs: "Naziv", labelEn: "Name", required: true, maxLength: 100 },
        { key: "isResident", labelBs: "Rezidentnost", labelEn: "Residency", type: "segmented", required: true, options: [
          { value: true, labelBs: "Rezident", labelEn: "Resident" },
          { value: false, labelBs: "Nerezident", labelEn: "Non-resident" },
        ] },
        { key: "taxNumber", labelBs: "Porezni broj", labelEn: "Tax number", pattern: "[0-9]{13}", minLength: 13, maxLength: 13 },
        { key: "maticniBroj", labelBs: "Matični broj", labelEn: "Registration number", pattern: "[0-9]*" },
        { key: "fbaId", labelBs: "FBA ID", labelEn: "FBA ID", pattern: "[0-9]{1,10}", maxLength: 10 },
        { key: "gccNumber", labelBs: "GCC broj", labelEn: "GCC number", required: true, pattern: "[0-9]+" },
        { key: "gccName", labelBs: "GCC naziv", labelEn: "GCC name", required: true },
        {
          key: "basisOfConnection",
          labelBs: "Osnov povezanosti",
          labelEn: "Connection basis",
          type: "select",
          required: true,
          codeListCategory: "OsnovPovezanosti",
          options: legalConnectionOptions,
        },
        {
          key: "connectionDescription",
          labelBs: "Opis povezanosti",
          labelEn: "Connection description",
          type: "textarea",
          required: true,
        },
        {
          key: "connectedWithBank",
          labelBs: "Povezan s bankom",
          labelEn: "Connected with bank",
          type: "segmented",
          required: true,
          options: yesNoOptions,
        },
        { key: "dateFrom", labelBs: "Datum od", labelEn: "Date from", type: "date", required: true },
        { key: "dateTo", labelBs: "Datum do", labelEn: "Date to", type: "date" },
      ],
    },
  },
  {
    key: "physicalPersons",
    path: "/app/physical-persons",
    endpoint: "/api/related-persons/detailed",
    displayColumns: ["personTypeLabel", "firstName", "lastName", "residencyLabel", "jmbg", "passportNumber", "relationBasis", "statusLabel"],
    icon: UserRound,
    area: "work",
    accessRole: "physical-persons",
    capabilities: {
      mutationEndpoint: "/api/related-persons",
      create: true,
      update: true,
      remove: true,
      verifyPath: "/api/verification/physical-person/{id}",
      fields: [
        { key: "firstName", labelBs: "Ime", labelEn: "First name", required: true, maxLength: 100 },
        { key: "lastName", labelBs: "Prezime", labelEn: "Last name", required: true, maxLength: 100 },
        {
          key: "residency",
          labelBs: "Rezidentnost",
          labelEn: "Residency",
          type: "segmented",
          required: true,
          options: [
            { value: 1, labelBs: "Rezident", labelEn: "Resident" },
            { value: 2, labelBs: "Nerezident", labelEn: "Non-resident" },
          ],
        },
        { key: "jmbg", labelBs: "JMBG", labelEn: "National ID", pattern: "[0-9]{13}", minLength: 13, maxLength: 13 },
        { key: "passportNumber", labelBs: "Broj pasoša", labelEn: "Passport number", maxLength: 50 },
        { key: "fbaId", labelBs: "FBA ID", labelEn: "FBA ID", pattern: "[0-9]{1,10}", maxLength: 10 },
        { key: "gccNumber", labelBs: "GCC broj", labelEn: "GCC number", required: true, pattern: "[0-9]+" },
        { key: "gccName", labelBs: "GCC naziv", labelEn: "GCC name", required: true },
        { key: "relationBasis", labelBs: "Osnov povezanosti", labelEn: "Relation basis", type: "select", required: true, codeListCategory: "OsnovPovezanosti", options: physicalConnectionOptions },
        {
          key: "relationDescription",
          labelBs: "Opis povezanosti",
          labelEn: "Relation description",
          type: "textarea",
          required: true,
        },
        { key: "specialRelationBasis", labelBs: "Osnov posebnog odnosa", labelEn: "Special relationship basis", type: "select", required: true, codeListCategory: "OsnovPosebnogOdnosa", options: specialRelationshipOptions },
        { key: "isIdentifiedStaff", labelBs: "Identifikovani zaposlenik", labelEn: "Identified staff", type: "segmented", required: true, options: yesNoOptions },
        {
          key: "connectedWithBank",
          labelBs: "Povezan s bankom",
          labelEn: "Connected with bank",
          type: "segmented",
          required: true,
          options: yesNoOptions,
        },
        { key: "specialRelationshipWithBank", labelBs: "U posebnom odnosu s bankom", labelEn: "Special relationship with bank", type: "segmented", required: true, options: yesNoOptions },
        { key: "specialContract", labelBs: "Poseban ugovor", labelEn: "Special contract", type: "segmented", required: true, options: yesNoOptions },
        { key: "malusClawback", labelBs: "Malus & Clawback", labelEn: "Malus & Clawback", type: "segmented", required: true, options: yesNoOptions },
        { key: "dateFrom", labelBs: "Datum od", labelEn: "Date from", type: "date", required: true },
        { key: "dateTo", labelBs: "Datum do", labelEn: "Date to", type: "date", required: true },
        {
          key: "declarationNoFamilyMembers",
          labelBs: "Nema članova porodice",
          labelEn: "No family members",
          type: "segmented",
          required: true,
          options: yesNoOptions,
        },
        {
          key: "relatedToPersonId",
          labelBs: "Povezano s fizičkim licem",
          labelEn: "Related to individual",
          type: "select",
          required: true,
          lookupEndpoint: "/api/related-persons",
        },
        {
          key: "familyRelationshipType",
          labelBs: "Porodični odnos",
          labelEn: "Family relationship",
          type: "select",
          required: true,
          codeListCategory: "Srodstvo",
          valueKind: "number",
        },
      ],
    },
  },
  {
    key: "limits",
    path: "/app/limits",
    endpoint: "/api/limiti",
    icon: Scale,
    area: "work",
    accessRole: "limits",
    capabilities: {
      create: true,
      update: true,
      remove: true,
      fields: [
        { key: "naziv", labelBs: "Naziv", labelEn: "Name", required: true },
        { key: "tipLimita", labelBs: "Tip limita", labelEn: "Limit type", type: "select", required: true, codeListCategory: "VrstaLimita", options: [
          { value: "REG", labelBs: "Regulatorni limit", labelEn: "Regulatory limit" },
          { value: "INT", labelBs: "Interni limit", labelEn: "Internal limit" },
        ] },
        { key: "iznosLimita", labelBs: "Iznos limita", labelEn: "Limit amount", type: "number" },
        { key: "utilizacija", labelBs: "Utilizacija", labelEn: "Utilisation", type: "number" },
        {
          key: "korigovaniLimit",
          labelBs: "Korigovani limit",
          labelEn: "Adjusted limit",
          type: "number",
        },
        {
          key: "rokUtilizacije",
          labelBs: "Rok utilizacije",
          labelEn: "Utilisation deadline",
          type: "date",
        },
        { key: "komentar", labelBs: "Komentar", labelEn: "Comment" },
      ],
    },
  },
  {
    key: "capital",
    path: "/app/capital",
    endpoint: "/api/limiti",
    icon: Landmark,
    area: "work",
    accessRole: "limits",
  },
  {
    key: "reports",
    path: "/app/reports",
    endpoint: "/api/reports/monthly",
    icon: FileChartColumn,
    area: "work",
    accessRole: "regulatory-reporting",
  },
  {
    key: "notifications",
    path: "/app/notifications",
    endpoint: "/api/email-log",
    displayColumns: ["purpose", "to", "subject", "deliveryStatus", "sentAt"],
    icon: Bell,
    area: "work",
    accessRole: "regulatory-reporting",
  },
  {
    key: "users",
    path: "/app/admin/users",
    endpoint: "/api/users",
    icon: Users,
    area: "administration",
    requiresAllAccesses: true,
  },
  {
    key: "period",
    path: "/app/admin/period",
    endpoint: "/api/period-lock/status",
    icon: CalendarClock,
    area: "administration",
    accessRole: "regulatory-reporting",
  },
  {
    key: "codeLists",
    path: "/app/admin/code-lists",
    endpoint: "/api/code-lists",
    icon: BookOpen,
    area: "administration",
    requiresAllAccesses: true,
  },
  {
    key: "audit",
    path: "/app/admin/audit",
    endpoint: "/api/audit-logs?page=1&pageSize=200",
    displayColumns: ["timestamp", "actionDisplay", "areaDisplay", "changeSummary", "username", "ipAddress"],
    icon: ScrollText,
    area: "administration",
    requiresAllAccesses: true,
  },
];

export const resourcesByKey = new Map(
  registryResources.map((resource) => [resource.key, resource]),
);
