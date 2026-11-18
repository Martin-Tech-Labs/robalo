using OpenAI.Chat;
using Robalo.Domain.Models;

namespace Robalo.Domain.Clients.Models;

public record ThreadMessage(string Content, MessageRole Source)
{
    public static ChatMessage ToChatMessage(ThreadMessage message)
    {
        return message.Source switch
        {
            MessageRole.User => ChatMessage.CreateUserMessage(message.Content),
            MessageRole.LLM => ChatMessage.CreateAssistantMessage(message.Content),

            _ => throw new ArgumentException(nameof(message.Source))
        };
    }

    public static ThreadMessage FromContextMessage(ContextMessage message) => new(message.Content, message.Source);
}