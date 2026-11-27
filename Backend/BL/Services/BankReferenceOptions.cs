namespace RBBH.ConnectedParties.BL.Services;

public sealed class BankReferenceOptions
{
    public bool Enabled { get; set; }
    public string ServerName { get; set; } = "";
    public string Database { get; set; } = "";
    public BankReferenceMapping<CustomerReferenceColumns> Customers { get; set; } = new();
    public BankReferenceMapping<GccReferenceColumns> Gcc { get; set; } = new();
}

public sealed class BankReferenceMapping<TColumns> where TColumns : new()
{
    public string Table { get; set; } = "";
    public TColumns Columns { get; set; } = new();
}

public sealed class CustomerReferenceColumns
{
    public string TaxNumber { get; set; } = "";
    public string RegistrationNumber { get; set; } = "";
    public string Name { get; set; } = "";
}

public sealed class GccReferenceColumns
{
    public string Number { get; set; } = "";
    public string Name { get; set; } = "";
}
