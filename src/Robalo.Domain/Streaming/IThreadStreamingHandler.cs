using Robalo.Domain.Models.MessageBus;

namespace Robalo.Domain.Services;

public interface IStreamThreadHandler
{
    void Handle(ThreadUpdated threadUpdated);
}