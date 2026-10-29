namespace Robalo.Common.Coordination;

public record SubscriptionId<TMessage>(string Id) where TMessage : notnull;