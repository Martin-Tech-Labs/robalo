using Robalo.Domain.Models.MessageBus;

namespace Robalo.Domain.Services;

public interface IAnswerThreadHandler
{
    void Handle(ThreadUpdated threadUpdated);
}
