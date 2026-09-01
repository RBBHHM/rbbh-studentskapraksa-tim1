using RBBH.ConnectedParties.Helpers.Constants;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RBBH.ConnectedParties.IoC.Extensions.Swagger
{
    public class AddCorrelationIdHeader : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = AppConstants.CORRELATION_ID,
                In = ParameterLocation.Header,
                Required = false,
                Description = "A unique request identifier",
                Schema = new OpenApiSchema { Type = "string" }
            });
        }
    }
}
