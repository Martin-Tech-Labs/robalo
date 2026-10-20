using Robalo.Domain.Events;

namespace Robalo.Domain.Models;

public abstract record Message(
    int Number, 
    string Content, 
    DateTimeOffset CreatedOn);