# Konvencije

- Namespace i projekti počinju sa `RBBH.ConnectedParties`.
- Backend i njegovi testovi su u `RelatedPartiesRegister`, a frontend je odvojen u `src/Web`.
- API ugovor je OpenAPI; frontend koristi generirane tipove i zajednički HTTP klijent.
- Tajne se daju kroz secret store ili environment varijable.
- SQL Server je jedini trajni provider; InMemory je samo lokalni fallback.
- Autentifikacijski fallback dozvoljen je samo u Development/Testing okruženju.
