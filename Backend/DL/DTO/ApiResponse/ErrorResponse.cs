namespace RBBH.ConnectedParties.DL.DTO.ApiResponse
{
    public class ErrorResponse
    {
        public List<ErrorDetail> Errors { get; set; }

        public ErrorResponse()
        {
            Errors = new List<ErrorDetail>();
        }

        public ErrorResponse(string field, string message)
        {
            Errors = new List<ErrorDetail>
            {
                new ErrorDetail { Field = field, Message = message }
            };
        }

        public ErrorResponse(List<ErrorDetail> errors)
        {
            Errors = errors;
        }
    }

    public class ErrorDetail
    {
        public string? Field { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
