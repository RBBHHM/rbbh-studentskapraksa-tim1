# Konvencije

- Backend namespace i projekti počinju sa `RBBH.ConnectedParties`; frontend je `RBBH.RelatedPartiesRegister.Web`.
- Backend i njegovi testovi su u `Backend/` i `tests/Backend.*`; frontend u `Frontend/` sa testovima `tests/Web.Blazor.*`.
- Frontend koristi tipizirane API klijente nad RBI transportom; ne postoje ručno izmišljene rute izvan ugovora `docs/BACKEND-FRONTEND-CONTRACT-FINAL.md`.
- Tajne se daju kroz secret store, user-secrets ili environment varijable; nikada kroz appsettings u repozitoriju.
- SQL Server je jedini trajni provider; InMemory je samo lokalni fallback i test host.
- Autentifikacijski fallback je dozvoljen samo u Development okruženju; izvan njega su Issuer, ClientId, ClientSecret i Audience obavezni.
- Vidljiv tekst frontenda ide kroz BS/EN resurse; testovi brane literalni tekst u markupu.
