// using Moq;
// using Robalo.Common.Services;
// using Robalo.Controller.Api.Controllers;
// using Robalo.Domain.Repositories;
// using Serilog;

// namespace Robalo.Controller.UnitTests.Controllers;

// public class ThreadsController_CreateThread
// {
//     ThreadsController _sut;
//     Mock<IDateTimeOffsetProvider> _dateTimeOffsetProvider;
//     Mock<IThreadRepository> _repository;

//     public ThreadsController_CreateThread()
//     {
//         _sut = new(Log.Logger, _repository, _dateTimeOffsetProvider)
//     }

//     public Task ShouldCreateThread()
//     {
//     }

// }