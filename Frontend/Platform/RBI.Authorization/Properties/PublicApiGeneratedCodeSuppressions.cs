using System.Diagnostics.CodeAnalysis;

// Razor emits these framework overrides under #nullable disable. Preserve their
// exact oblivious (~) signatures in PublicAPI.Unshipped.txt; waive only RS0041's
// annotation-style warning for these specific generated members. All signature
// additions/removals/nullability mismatches remain build errors (RS0016/RS0017).
[assembly: SuppressMessage("ApiDesign", "RS0041:Public API files should contain only API with fully annotated types", Scope = "member", Target = "~M:RBI.Authorization.RbiAuthorize.BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder)", Justification = "Razor-generated framework override; exact oblivious signature is tracked.")]
