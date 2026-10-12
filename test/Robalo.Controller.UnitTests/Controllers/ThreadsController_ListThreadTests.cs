using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Moq;
using Robalo.Common.Models;
using Robalo.Common.Services;
using Robalo.Controller.Api.Controllers;
using Robalo.Controller.Api.Hypermedia.Resources;
using Robalo.Controller.Api.Requests;
using Robalo.Domain.Events;
using Robalo.Domain.Models;
using Robalo.Domain.Repositories;
using Serilog;

namespace Robalo.Controller.UnitTests.Controllers;

public class ThreadsController_ListThreadTests : ThreadsControllerTests
{
    [Fact]
    public async Task ShouldReturnOk_WithThreads()
    {
        var threadInfos = _fixture.CreateMany<ThreadInfo>(
            _fixture.Create<int>()).OrderBy(t => t.CreatedOn).ToList();

        _repository
            .Setup(r => r.ListThreads(TestContext.Current.CancellationToken))
            .ReturnsAsync(threadInfos.ToList());

        var result = await _sut.ListThreads(TestContext.Current.CancellationToken);

        result.ShouldBeOfType<OkObjectResult>();
        ((OkObjectResult)result).Value.ShouldBeOfType<ThreadListResource>();

        var resource = ((OkObjectResult)result).Value as ThreadListResource;
        resource.ShouldNotBeNull();

        ValidateAllNotNullLinks(resource);
        resource.Embedded.ShouldNotBeNull();
        resource.Embedded.Threads.ShouldNotBeNull();

        resource.Embedded.Threads.Count.ShouldBe(threadInfos.Count);

        var threadResources = resource.Embedded.Threads.OrderBy(t => t.CreatedOn).ToList();

        for (var i = 0; i < threadInfos.Count; i++)
        {
            var threadInfo = threadInfos[i];
            var threadResource = threadResources[i];

            ValidateAllNotNullLinks(threadResource);

            threadResource.Title.ShouldBe(threadInfo.Title);
            threadResource.CreatedOn.ShouldBe(threadInfo.CreatedOn);
            threadResource.ModifiedOn.ShouldBe(threadInfo.ModifiedOn);
            threadResource.ThreadId.ShouldBe(threadInfo.Id);
        }
    }

    [Fact]
    public async Task ShouldReturnOk_WithNoThreads()
    {
        _repository
            .Setup(r => r.ListThreads(TestContext.Current.CancellationToken))
            .ReturnsAsync(new List<ThreadInfo>());

        var result = await _sut.ListThreads(TestContext.Current.CancellationToken);

        result.ShouldBeOfType<OkObjectResult>();
        ((OkObjectResult)result).Value.ShouldBeOfType<ThreadListResource>();

        var resource = ((OkObjectResult)result).Value as ThreadListResource;
        resource.ShouldNotBeNull();

        ValidateAllNotNullLinks(resource);
        resource.Embedded.ShouldNotBeNull();
        resource.Embedded.Threads.ShouldNotBeNull();

        resource.Embedded.Threads.Count.ShouldBe(0);
    }
}