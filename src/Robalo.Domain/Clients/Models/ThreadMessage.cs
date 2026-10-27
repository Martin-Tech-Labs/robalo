using OpenAI.Chat;
using Robalo.Domain.Models;

namespace Robalo.Domain.Clients.Models;

public record ThreadMessage(string Content, MessageSource Source)
{
    public static ChatMessage ToChatMessage(ThreadMessage message)
    {
        return message.Source switch
        {
            MessageSource.User => ChatMessage.CreateUserMessage(message.Content),
            MessageSource.LLM => ChatMessage.CreateAssistantMessage(message.Content),

            _ => throw new ArgumentException(nameof(message.Source))
        };
    }

    public static ThreadMessage FromMessage(Message message) => new(message.Content, message.Source);
}