namespace Robalo.Controller.Api.Hypermedia;

#pragma warning disable IDE1006 
public abstract record HalResource<TLinks>(TLinks _Links) where TLinks : Links;