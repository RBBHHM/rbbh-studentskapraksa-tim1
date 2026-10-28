# RBI.Forms

## Purpose

Optional nonvisual EditContext server-validation mapping and duplicate-submission protection; no DesignSystem/ApiCore dependency.

## Source integration

This repository builds this library through ProjectReference; see ../SOURCE.md.

## Minimal setup

```csharp
using var validation = new RbiServerValidation(editContext);
var submission = new RbiFormSubmissionState();
await submission.SubmitAsync(async () => validation.DisplayErrors(await SaveAsync()));
```
editContext, SaveAsync and the Dictionary<string, string[]> result belong to the application. No DI registration is needed.

## Key contract

RbiServerValidation and RbiFormSubmissionState supplement normal Blazor forms without owning models or business validation.

## Production caveats

Use top-level model property names and an empty key for form-level errors. Dispose validation subscriptions. Dirty-state/navigation decisions remain standard EditContext/NavigationLock behavior.

See [repository development guidance](https://github.com/mahmutovichana/rbi-design-core/blob/main/Docs/DEVELOPMENT.md), [deployment](https://github.com/mahmutovichana/rbi-design-core/blob/main/Docs/DEPLOYMENT.md) and [public API review](https://github.com/mahmutovichana/rbi-design-core/blob/main/Docs/PUBLIC-API.md).
