using Robalo.Domain.Models;

namespace Robalo.Domain.UnitTests.Models;


public class MessageIdentifierTests : BaseIdentifierTests<MessageIdentifier>
{
    public MessageIdentifierTests() : base(
        () => MessageIdentifier.NewIdentifier(),
        MessageIdentifier.FromUuid,
        MessageIdentifier.FromIdString,
        "msg_")
    {
    }
}