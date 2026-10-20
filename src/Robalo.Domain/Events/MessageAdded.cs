using Robalo.Domain.Models;

namespace Robalo.Domain.Events;

public abstract record MessageAdded(
    int MessageNumber,
    string Content,
    DateTimeOffset AddedOn);