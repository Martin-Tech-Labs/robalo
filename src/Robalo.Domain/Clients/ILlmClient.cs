using Robalo.Common.Models;
using Robalo.Domain.Clients.Models;

namespace Robalo.Domain.Clients;

public interface ILlmClient
{
    public Task<ServiceResult<LlmResponse, ClientError>> GetResponse(LlmRequest request, CancellationToken cancellationToken);
}