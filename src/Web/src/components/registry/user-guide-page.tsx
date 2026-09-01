import {
  BookOpenCheck,
  Building2,
  ChevronDown,
  ClipboardCheck,
  FileSpreadsheet,
  Scale,
  UserCog,
  UsersRound,
} from "lucide-react";
import { useTranslation } from "react-i18next";

import { IconIndicator } from "@/components/registry/icon-indicator";
import { Heading, Text } from "@/components/ui/typography";
import {
  activeApplicationAccesses,
  hasAllApplicationAccesses,
  type ApplicationAccessRole,
} from "@/lib/auth/application-access";

type GuideSection = {
  readonly id: string;
  readonly role?: ApplicationAccessRole;
  readonly admin?: boolean;
  readonly icon: typeof UsersRound;
  readonly titleBs: string;
  readonly titleEn: string;
  readonly summaryBs: string;
  readonly summaryEn: string;
  readonly pointsBs: readonly string[];
  readonly pointsEn: readonly string[];
};

const sections: readonly GuideSection[] = [
  {
    id: "individuals", role: "physical-persons", icon: UsersRound,
    titleBs: "Fizička lica i stablo povezanosti", titleEn: "Individuals and relationship tree",
    summaryBs: "Jedinstven unos zaposlenika, povezanih lica i članova porodice.", summaryEn: "A unified record for employees, related persons and family members.",
    pointsBs: [
      "Novi zapis prolazi kroz dva kratka koraka: identitet, zatim povezanost i period važenja.",
      "Za rezidenta je obavezan ispravan JMBG, a za nerezidenta broj pasoša. JMBG, pasoš i FBA ID ne mogu biti duplirani među aktivnim zapisima.",
      "Odabirom osnova „Član uže porodice povezanog lica“ poslovne DA/NE vrijednosti se automatski postavljaju i zaključavaju. Promjenom osnova ponovo se oslobađaju i resetuju.",
      "Ikonica štita u tabeli verificira nacrt. Nakon uspjeha status se odmah mijenja i akcija verifikacije nestaje.",
      "Stablo povezanosti prikazuje evidentirane porodične i druge veze za bilo koje lice, uključujući zaposlenika.",
      "Excel uvoz prvo provjerava strukturu, kolone i svaki red; izvoz preuzima trenutno evidentirane zapise. Brisanje je soft-delete i zapis više nije u aktivnom pregledu.",
    ],
    pointsEn: [
      "A new record uses two short steps: identity, then relationship and validity period.",
      "A valid national ID is required for residents and a passport number for non-residents. National ID, passport and FBA ID cannot be duplicated across active records.",
      "Selecting ‘Immediate family member of a related person’ pre-fills and locks the business yes/no values. Selecting another basis resets and unlocks them.",
      "The shield action verifies a draft. After success the status refreshes immediately and the verification action disappears.",
      "The relationship tree displays recorded family and other links for any individual, including an employee.",
      "Excel import first validates structure, columns and each row; export downloads recorded data. Delete is a soft delete and removes the record from the active view.",
    ],
  },
  {
    id: "legal", role: "legal-persons", icon: Building2,
    titleBs: "Pravna lica", titleEn: "Legal entities",
    summaryBs: "Evidencija, provjera i razmjena podataka o povezanim pravnim licima.", summaryEn: "Recording, verification and data exchange for related legal entities.",
    pointsBs: [
      "Naziv i osnov povezanosti su obavezni; identifikatori i datumi se provjeravaju prema rezidentnosti i poslovnim pravilima.",
      "Nacrt se može urediti, verificirati ili obrisati. Nakon svake uspješne akcije tabela se automatski osvježava.",
      "Excel uvoz prihvata samo propisani predložak i prikazuje grešku po redu i koloni; izvoz preuzima kompletan pregled.",
    ],
    pointsEn: [
      "Name and relationship basis are required; identifiers and dates are checked against residency and business rules.",
      "A draft can be edited, verified or deleted. The table refreshes automatically after every successful action.",
      "Excel import accepts only the prescribed template and reports errors by row and column; export downloads the complete view.",
    ],
  },
  {
    id: "limits", role: "limits", icon: Scale,
    titleBs: "Limiti i kapital", titleEn: "Limits and capital",
    summaryBs: "Odvojeni ekrani nad povezanim finansijskim podacima.", summaryEn: "Separate screens over connected financial data.",
    pointsBs: [
      "Limiti čuvaju iznos, iskorištenost, korekciju, rok i komentar. Brojčana polja ne prihvataju tekst niti negativne vrijednosti gdje nisu dozvoljene.",
      "Raspoloživi limit se izračunava iz evidentiranih vrijednosti i ne unosi se ručno.",
      "Kapital je izdvojen radi jasnijeg rada, ali regulatorni i osnovni kapital ostaju povezani sa istim poslovnim podacima.",
      "Izvoz u Excel koristi podatke prikazane u modulu.",
    ],
    pointsEn: [
      "Limits store amount, utilisation, adjustment, deadline and comment. Numeric fields reject text and disallowed negative values.",
      "Available limit is calculated from recorded values and is not entered manually.",
      "Capital has a separate screen for clarity, while regulatory and core capital remain connected to the same business data.",
      "Excel export uses the data shown in the module.",
    ],
  },
  {
    id: "reporting", role: "regulatory-reporting", icon: FileSpreadsheet,
    titleBs: "Regulatorno izvještavanje i period", titleEn: "Regulatory reporting and period",
    summaryBs: "Generisanje, preuzimanje i kontrola izvještajnog perioda.", summaryEn: "Report generation, download and reporting-period control.",
    pointsBs: [
      "Dnevni i mjesečni izvještaji nastaju iz evidentiranih podataka i nakon generisanja se mogu preuzeti iz pregleda.",
      "Notifikacije objašnjavaju poslovnu svrhu, primaoce, status dostave i vrijeme evidentiranja poruke.",
      "Zaključan period sprečava poslovne izmjene. Zahtjev za otključavanje mora sadržavati razlog; administrator ga može odobriti, vratiti na dopunu ili odbiti uz napomenu.",
    ],
    pointsEn: [
      "Daily and monthly reports are produced from recorded data and can be downloaded from the list after generation.",
      "Notifications show business purpose, recipients, delivery status and the time a message was recorded.",
      "A locked period blocks business changes. An unlock request must contain a reason; an administrator can approve it, request more information or reject it with a note.",
    ],
  },
  {
    id: "administration", admin: true, icon: UserCog,
    titleBs: "Administracija", titleEn: "Administration",
    summaryBs: "Korisnici, pristupi, šifrarnici i audit trag.", summaryEn: "Users, access, code lists and audit trail.",
    pointsBs: [
      "Korisnik može imati jedan ili više od četiri nezavisna funkcionalna pristupa: Fizička lica, Pravna lica, Limiti i Regulatorno izvještavanje.",
      "Novi korisnik mora imati jedinstveno korisničko ime i e-mail adresu koja završava sa @raiffeisengroup.ba. Deaktivacija privremeno ukida pristup, a brisanje uklanja korisnički zapis prema potvrđenom toku.",
      "Šifrarnici pune izbore u formama. Aktivna vrijednost se može birati; neaktivna ostaje radi historije. Vrijednost ili cijeli šifrarnik nije moguće obrisati dok ga poslovni podatak koristi.",
      "Audit evidencija pokazuje ko je, kada i nad kojim područjem izvršio promjenu, uključujući čitljiv sažetak prije i poslije izmjene.",
    ],
    pointsEn: [
      "A user can hold one or more of four independent functional accesses: Individuals, Legal entities, Limits and Regulatory reporting.",
      "A new user needs a unique username and an email ending in @raiffeisengroup.ba. Deactivation temporarily removes access; deletion removes the user record through a confirmed flow.",
      "Code lists populate form choices. Active values can be selected; inactive values remain for history. A value or entire list cannot be deleted while business data uses it.",
      "The audit log shows who changed what, when and in which area, including a readable before-and-after summary.",
    ],
  },
];

export function UserGuidePage() {
  const { i18n } = useTranslation();
  const bs = i18n.language.startsWith("bs");
  const accesses = activeApplicationAccesses();
  const admin = hasAllApplicationAccesses();
  const visible = sections.filter((section) => (!section.role || accesses.has(section.role)) && (!section.admin || admin));

  return (
    <section className="mx-auto max-w-6xl">
      <div className="overflow-hidden rounded-sm border border-border-subtle bg-surface-raised shadow-sm">
        <div className="grid gap-6 bg-surface-brand-subtle p-6 md:grid-cols-[auto_1fr] md:p-8">
          <span className="flex size-14 items-center justify-center rounded-full bg-surface-brand text-text-on-brand">
            <BookOpenCheck className="size-7" aria-hidden="true" />
          </span>
          <div>
            <p className="text-eyebrow text-text-tertiary">{bs ? "Pomoć u radu" : "Help centre"}</p>
            <Heading level={1} size={4} className="mt-2">{bs ? "Korisnički vodič" : "User guide"}</Heading>
            <Text tone="secondary" className="mt-2 max-w-3xl">
              {bs ? "Ovdje su prikazana samo područja kojima imate pristup. Otvorite cjelinu kada trebate značenje statusa, pravilo unosa ili objašnjenje poslovnog toka." : "Only areas you can access are shown. Open a section for status meanings, input rules and business-flow guidance."}
            </Text>
          </div>
        </div>
      </div>

      <div className="mt-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <Legend kind="draft" label={bs ? "Nacrt" : "Draft"} description={bs ? "Unesen zapis čeka provjeru." : "The record is awaiting review."} />
        <Legend kind="verified" label={bs ? "Verificirano" : "Verified"} description={bs ? "Ovlaštena osoba potvrdila je podatke." : "An authorised user confirmed the data."} />
        <Legend kind="rejected" label={bs ? "Odbijeno" : "Rejected"} description={bs ? "Zapis nije prihvaćen; provjerite razlog." : "The record was not accepted; check the reason."} />
        <Legend kind="inactive" label={bs ? "Neaktivno" : "Inactive"} description={bs ? "Podatak ostaje u historiji, ali se ne koristi." : "The value remains in history but is not used."} />
      </div>

      <div className="mt-8 space-y-4">
        {visible.map((section, index) => {
          const Icon = section.icon;
          return (
            <details key={section.id} open={index === 0} className="group rounded-sm border border-border-subtle bg-surface-default shadow-sm">
              <summary className="flex cursor-pointer list-none items-center gap-4 p-5 marker:hidden">
                <span className="flex size-11 shrink-0 items-center justify-center rounded-full border border-border-brand bg-surface-brand-subtle text-text-brand-accent"><Icon className="size-5" /></span>
                <span className="min-w-0 flex-1"><strong className="block text-lg">{bs ? section.titleBs : section.titleEn}</strong><span className="mt-1 block text-sm text-text-secondary">{bs ? section.summaryBs : section.summaryEn}</span></span>
                <ChevronDown className="size-5 shrink-0 transition-transform group-open:rotate-180" aria-hidden="true" />
              </summary>
              <div className="border-t border-border-subtle px-5 py-5 md:pl-20">
                <ol className="space-y-3">
                  {(bs ? section.pointsBs : section.pointsEn).map((point, pointIndex) => (
                    <li key={point} className="flex gap-3 text-sm leading-6 text-text-secondary"><span className="mt-1 flex size-5 shrink-0 items-center justify-center rounded-full bg-surface-brand text-xs font-bold text-text-on-brand">{pointIndex + 1}</span><span>{point}</span></li>
                  ))}
                </ol>
              </div>
            </details>
          );
        })}
      </div>

      <div className="mt-8 grid gap-4 rounded-sm border border-border-subtle bg-surface-subtle p-5 md:grid-cols-[auto_1fr]">
        <ClipboardCheck className="size-7 text-text-brand-accent" />
        <div><h2 className="font-bold">{bs ? "Prije potvrde promjene" : "Before confirming a change"}</h2><Text tone="secondary" className="mt-1">{bs ? "Provjerite obavezna polja, identifikatore, datume i poslovni osnov. Aplikacija prikazuje detaljnu poruku ako nešto nije ispravno; podatak se ne čuva dok se greška ne otkloni." : "Check required fields, identifiers, dates and business basis. The application shows a detailed message when something is invalid; data is not saved until the issue is resolved."}</Text></div>
      </div>
    </section>
  );
}

function Legend({ kind, label, description }: { readonly kind: "draft" | "verified" | "rejected" | "inactive"; readonly label: string; readonly description: string }) {
  return <div className="flex items-start gap-3 rounded-sm border border-border-subtle bg-surface-default p-4"><IconIndicator kind={kind} label={label} /><div><strong className="text-sm">{label}</strong><p className="mt-1 text-xs leading-5 text-text-secondary">{description}</p></div></div>;
}
