using Microsoft.AspNetCore.Mvc;

namespace Robalo.Controller.Api.Hypermedia;

#pragma warning disable IDE1006 
public abstract record HalResource<TLinks>(TLinks _Links) where TLinks : Links
{
    public CreatedResult AsCreated() => new(_Links.Self.Href.AbsoluteUri, this);
}