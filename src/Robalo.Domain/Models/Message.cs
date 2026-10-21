using Robalo.Common.Models;
using Robalo.Domain.Events;

namespace Robalo.Domain.Models;

public record Message(
    int Number,
    MessageSource Source,
    string Content,
    Option<int> RepliedToMessage,
    DateTimeOffset CreatedOn);