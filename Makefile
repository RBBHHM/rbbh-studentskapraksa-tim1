# Optional convenience only; canonical Windows commands are scripts/*.cmd.
restore:
	dotnet restore RelatedPartiesRegister/RelatedPartiesRegister.sln --configfile RelatedPartiesRegister/nuget.config
	cd src/Web && pnpm.cmd install --ignore-scripts
build:
	dotnet build RelatedPartiesRegister/RelatedPartiesRegister.sln -c Release --no-restore
	cd src/Web && pnpm.cmd build
test:
	dotnet test RelatedPartiesRegister/RelatedPartiesRegister.sln --no-restore
	cd src/Web && pnpm.cmd lint && pnpm.cmd localization:validate && pnpm.cmd test
