using Robalo.Common.Models;
using Robalo.Domain.Services.Models;

namespace Robalo.Domain.Services;

public interface ILlmService
{
    public Task<ServiceResult<LlmResponse, ClientError>> GetResponse(LlmRequest request, CancellationToken cancellationToken);
}