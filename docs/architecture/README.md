# Arhitektura

Tok zahtjeva: `Browser → Blazor Server frontend (RBI sesija, cookie 8 h bez produžavanja) → tipizirani API klijenti (RBI transport, bearer token) → .NET API → EF Core → SQL Server`.

* **Autentifikacija i autorizacija.** Keycloak samo autenticira. Uloge i dozvole su u bazi (normalizirane tabele korisnika i uloga); provider claimovi za uloge/dozvole se ignorišu i računaju ponovo. Nepoznata autenticirana osoba dobija virtualnu ulogu Gost bez zapisa. Frontend skriva akcije prema profilu, ali API ponovo provjerava svaku akciju.
* **Šifarnici.** Poslovne vrijednosti za padajuće menije dolaze iz SQL šifarnika (`CodeLists`, `CodeListDefinitions`), ne iz koda frontenda.
* **Greške i lokalizacija.** Backend vraća sigurne ProblemDetails greške; frontend prikazuje lokalizirane poruke (BS/EN kataloge) i, gdje postoji, runtime jezičke pakete s `api/localization`.
* **Perzistencija.** SQL Server je jedini trajni provider; InMemory služi samo za lokalni razvoj i testni BrowserHost.

Detalji: `docs/BACKEND-FRONTEND-CONTRACT-FINAL.md` i `docs/FRONTEND-INTEGRATION-FINAL.md`.
