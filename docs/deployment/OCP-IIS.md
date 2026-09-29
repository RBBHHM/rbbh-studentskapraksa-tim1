# OCP backend i IIS frontend

Backend i frontend su dva nezavisna deployment artefakta. OCP image sadrži samo backend iz `RelatedPartiesRegister/`, dok se statički React paket gradi iz `src/Web` i predaje IIS administratoru kao ZIP.

Tri fajla u `.github/workflows` ostaju byte-po-byte jednaka DataProducts workflowima. U svakom GitHub Environmentu postavite:

| Varijabla | Vrijednost |
|---|---|
| `PROJECT_PATH` | `RelatedPartiesRegister` |
| `SOLUTION_PATH` | `RelatedPartiesRegister.csproj` |
| `NUGET_CONFIG_PATH` | `nuget.config` |

Build workflow koristi `RelatedPartiesRegister/Dockerfile` sa korijenom repozitorija kao Docker contextom. CodeQL izvršava restore i build nad `RelatedPartiesRegister/RelatedPartiesRegister.csproj`. Connection string, Keycloak, certifikati i ostale tajne dolaze iz OCP Secret/ConfigMap resursa i ne upisuju se u image ili Git.

Frontend paket se pravi iz `src/Web`, uz obavezni backend i Keycloak origin:

```powershell
pnpm publish:iis -- --backend-origin <BACKEND-ORIGIN> --keycloak-origin <KEYCLOAK-PUBLIC-ORIGIN>
```

`--backend-origin` može biti ili direktna backend OCP Route adresa (bez 3scale
path prefiksa) ili puna 3scale APIcast adresa sa path prefiksom — zavisi od
okruženja (vidi primjere ispod). `--keycloak-origin` je javni Keycloak origin
(samo scheme+host, bez `/auth/realms/...` putanje) koji IIS koristi da vrati
tačan javni host u `Location` header pri Keycloak redirect-u.

UAT (direktna Route, bez 3scale, 2 proxy hop-a: IIS → OCP Route → app):

```powershell
pnpm publish:iis -- --backend-origin http://related-parties-register-rbbh-uat-t24.apps.ocp4test2.mbdom.rbbh --keycloak-origin https://keycloak-test-rhsso.apps.ocp4test2.mbdom.rbbh
```

PROD (kroz 3scale APIcast, 3 proxy hop-a: IIS → 3scale → OCP Route → app):

```powershell
pnpm publish:iis -- --backend-origin https://related-parties-register-front-3scale-apicast-production.apps.ocp4prod.mbdom.rbbh/related-parties-register --keycloak-origin https://keycloak-prod-rhsso.apps.ocp4prod.mbdom.rbbh
```

Skripta gradi production localization manifest, generiše same-origin
`app-config.js` i ugrađuje oba origina u paketovani `web.config`. IIS
raspakuje ZIP, nudi HTTPS i SPA fallback te prosljeđuje relativne `/api`,
`/health`, `/openapi`, `/authentication` i OIDC callback rute prema
`--backend-origin` (direktno ka OCP-u ili kroz 3scale, zavisno od okruženja).
Backend obavlja confidential-client OIDC code exchange i vraća HttpOnly
cookie.

Na IIS serveru moraju biti omogućeni ARR Proxy i URL Rewrite server varijable
`HTTP_X_FORWARDED_HOST` i `HTTP_X_FORWARDED_PROTO`.

**Broj proxy hop-ova i `ForwardLimit`:** `Program.cs` postavlja
`ForwardedHeadersOptions.ForwardLimit` na broj reverse-proxy skokova između
klijenta i aplikacije, jer su `KnownProxies`/`KnownIPNetworks` namjerno
isključeni (OCP/IIS/3scale adrese su dinamične). Kad se promijeni broj hop-ova
u lancu (npr. dodavanje ili uklanjanje 3scale-a iz IIS-ove interne rute),
`ForwardLimit` u `Program.cs` mora se ažurirati da odgovara stvarnom broju
skokova, inače se javni host/protokol netačno "odmotava" i vraća se bug sa
pogrešnim javnim URL-om kroz proxy. Trenutno: `ForwardLimit = 3` (IIS → 3scale
→ OCP Route → app, PROD topologija). Ako neko okruženje ne ide kroz 3scale
(npr. UAT, direktna Route — 2 hop-a), taj broj hop-ova je manji od onog za
koji je middleware trenutno podešen; middleware sa `ForwardLimit` većim od
stvarnog broja hop-ova je siguran (samo neće imati dovoljno unosa da ih sve
potroši), dok manji `ForwardLimit` od stvarnog broja hop-ova GUBI originalni
javni host — zato se `ForwardLimit` mora postaviti na najveći broj hop-ova
koji se koristi u bilo kojem okruženju koje ovaj deployment servisira.

ZIP nema `.dll` jer React nije serverski proces: IIS vraća već izgrađeni HTML,
CSS, JavaScript, fontove i slike. `app-config.js` sadrži samo javne runtime
vrijednosti i može se promijeniti bez rebuilda. Na IIS-u nisu potrebni Node,
pnpm ni .NET. Backend `.dll` postoji samo unutar OCP imagea.

```text
Browser → IIS statički frontend / SPA fallback
        → /authentication i /api kroz IIS ARR → OCP RelatedPartiesRegister
        → Keycloak kroz serverski Authorization Code tok → SQL Server
```

OCP database ConfigMap/Secret koristi `Database__ServerName`,
`Database__Name`, `Database__IntegratedSecurity`, `Database__User` i
`Database__Password`; puni connection string se ne postavlja.

## OCP, Keycloak i 3scale

Backend OCP Route mora nuditi plain HTTP prema aplikaciji. Aplikacija ne radi
TLS terminaciju i ne preusmjerava Swagger/API zahtjeve na HTTPS; javni HTTPS i
certifikat završavaju na 3scale ruti. U UAT-u ovu 3scale rutu koriste samo
eksterni API konzumenti (IIS ide direktno na backend Route, van 3scale — vidi
primjer iznad); u PROD-u kroz istu 3scale rutu ide i IIS-ov interni
reverse proxy (`--backend-origin` u `pnpm publish:iis` komandi iznad je ista
adresa):

- UAT: `https://related-parties-register-front-3scale-apicast-production.apps.ocp4test2.mbdom.rbbh/related-parties-register`
- PROD: `https://related-parties-register-front-3scale-apicast-production.apps.ocp4prod.mbdom.rbbh/related-parties-register`

U OCP ConfigMap postavite `KeycloakSettings__Enabled=true`,
`KeycloakSettings__Audience` i `KeycloakSettings__AdminClientId` na stvarni
OIDC client ID, i `KeycloakSettings__Issuer` na `<KEYCLOAK-ORIGIN>/auth/realms/rbbh`
gdje je `<KEYCLOAK-ORIGIN>` isti origin koji se koristi kao `--keycloak-origin`
iznad:

- UAT: `KeycloakSettings__Issuer=https://keycloak-test-rhsso.apps.ocp4test2.mbdom.rbbh/auth/realms/rbbh`
- PROD: `KeycloakSettings__Issuer=https://keycloak-prod-rhsso.apps.ocp4prod.mbdom.rbbh/auth/realms/rbbh`

Vrijednost `KeycloakSettings__AdminClientSecret` postavlja se isključivo kroz
OCP Secret, po okruženju.

OIDC client ID i secret postoje samo na backendu kao
`KeycloakSettings__ClientId` i `KeycloakSettings__ClientSecret`; secret se
postavlja kroz OCP Secret. React ne dobija Keycloak token niti secret. Keycloak
client mora dozvoliti javni IIS URL sa `/signin-oidc` callback putanjom —
`https://rpregister.cbwebuat.mbdom.rbbh/signin-oidc` za UAT,
`https://rpregister.mbdom.rbbh/signin-oidc` za PROD.
