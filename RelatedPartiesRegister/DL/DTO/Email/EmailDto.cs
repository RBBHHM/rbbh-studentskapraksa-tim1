namespace RBBH.ConnectedParties.DL.DTO.Email;

/// <summary>Shape expected by the shared bank-wide email microservice (api/Email/SendEmailWithHtmlBody).</summary>
public class EmailDto
{
    public string From { get; set; } = null!;
    public string To { get; set; } = null!;
    public string? Cc { get; set; }
    public string Subject { get; set; } = null!;
    public string Text { get; set; } = null!;
}
