using RBBH.RelatedPartiesRegister.Web.Api;

namespace RBBH.RelatedPartiesRegister.Web.Components.Registry;

// Existing application fallback vocabulary, not a replacement for live codebooks.
public static class PhysicalPersonOptions
{
    public static IReadOnlyList<ReferenceOption> Bases(AppText text) => new[]
    {
        ("ZOB-2-U-2", "zob2u2"), ("ZOB-2-V-1", "zob2v1"), ("ZOB-2-V-3", "zob2v3"),
        ("ZOB-2-V-4", "zob2v4"), ("ZOB-2-V-5", "zob2v5"), ("ZOB-2-V-7", "zob2v7"), ("ZOB-2-V-8", "zob2v8")
    }.Select(item => new ReferenceOption(item.Item1, text[$"basis.{item.Item2}.label"], text[$"basis.{item.Item2}.description"])).ToArray();
    public static IReadOnlyList<ReferenceOption> Special(AppText text) => new[]
    {
        ("NADZORNI_ODBOR", "supervisory"), ("UPRAVA", "management"), ("NKF", "key-function"),
        ("PROKURISTA", "representative"), ("B1", "b1"), ("UZA_PORODICA", "family")
    }.Select(item => new ReferenceOption(item.Item1, text[$"special.{item.Item2}"], null)).ToArray();
}
