# Konfiguracija okruženja

Lokalno kopirajte `.env.example` u `.env`. Fajl `.env` je gitignored, a
`scripts/dev-api.cmd` ga učitava prije pokretanja backenda. Stvarne lozinke ne
upisujte u `appsettings.json`, dokumentaciju ni primjer konfiguracije.

`pnpm dev:api` prvo pokušava SQL Server ako su njegove vrijednosti unesene. Ako
lokalni Development ne može uspostaviti vezu, prikazuje upozorenje i koristi
seedovanu InMemory bazu. Ovaj fallback nije dozvoljen u UAT/Production režimu.

## Backend/OCP

| Varijabla | Obavezna | Svrha |
|---|---:|---|
| `ASPNETCORE_ENVIRONMENT` | da | `Development`, `Staging` ili `Production`. |
| `Database__ServerName` | produkcija | SQL Server/instance; port ide u istoj vrijednosti. |
| `Database__Name` | produkcija | Naziv baze, npr. `RPR`; nije zakucan u kodu. |
| `Database__IntegratedSecurity` | ne | `true` za Windows identitet, inače `false`. |
| `Database__User` / `Database__Password` | SQL auth | OCP Secret; ne koriste se uz integrated security. |
| `KeycloakSettings__Enabled` | produkcija | Uključuje JWT validaciju. |
| `KeycloakSettings__Issuer` | uz Keycloak | Authority/issuer, npr. `https://id/realms/rbbh`. Iz njega se izvode realm i admin base URL. |
| `KeycloakSettings__PublicIssuer` | samo split URL | Dodatni javni issuer ako backend i browser vide Keycloak pod različitim adresama. |
| `KeycloakSettings__Audience` | uz Keycloak | Očekivani API audience. |
| `KeycloakSettings__AdminClientId` / `AdminClientSecret` | samo upravljanje korisnicima | Povjerljivi service account sa minimalnim realm-management pravima. |
| `Cors__AllowedOrigins__N` | samo cross-origin | IIS origin kada browser direktno zove OCP; nije potreban uz IIS ARR same-origin proxy. |

## Frontend/IIS

Frontend koristi Webpack runtime `app-config.js`; **ne koristi Vite niti VITE
varijable**. `API_BASE_URL` ostavite prazan uz preporučeni IIS ARR proxy. Ako
browser mora direktno zvati OCP, postavite javni API URL i odgovarajući CORS.

`KEYCLOAK_URL`, `KEYCLOAK_REALM` i `KEYCLOAK_CLIENT_ID` su javne PKCE postavke
koje browser mora znati. Nisu tajne. Backend ih ne može automatski dijeliti sa
statičkim IIS procesom jer su IIS i OCP odvojene deploy jedinice. Client secret
se nikada ne stavlja u frontend. `LOCALIZATION_MANIFEST_URL` i
`APP_ENVIRONMENT` su javne opcione runtime postavke.

```text
Browser → IIS (HTML/CSS/JS + app-config.js)
        → /api preko IIS ARR → OCP backend
        → Keycloak PKCE redirect koristeći javni client ID
OCP backend → validira bearer token → SQL Server
```

GitHub workflow varijable `PROJECT_PATH=RelatedPartiesRegister` i
`SOLUTION_PATH=RelatedPartiesRegister.csproj` služe samo build putanjama; nisu
application settings. Tajne pripadaju GitHub/OCP Secretima, ne repozitoriju.
