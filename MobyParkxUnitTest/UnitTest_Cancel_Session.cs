using Xunit;
using Moq;
using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MobyPark.Controllers;
using MobyPark.Services;
using MobyPark.Models;
using MobyPark.Entities;

namespace MobyParkxUnitTest
{
    public class CancelSessionTests
    {
        private ClaimsPrincipal CreateUser(Guid id, bool isAdmin)
        {
            var claims = new[]
            {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim(ClaimTypes.Role, isAdmin ? "Admin" : "Customer")
        };

            return new ClaimsPrincipal(new ClaimsIdentity(claims, "mock"));
        }

        private SessionController CreateController(Mock<ISessionService> mock, ClaimsPrincipal user)
        {
            return new SessionController(mock.Object, Mock.Of<IEncryptionService>())
            {
                ControllerContext = new()
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = user
                    }
                }
            };
        }

        // ---------------------------------------------------------------
        // 1. User cannot cancel someone else's session
        // ---------------------------------------------------------------
        [Fact]
        public async Task Cancel_Fails_When_User_Not_Owner()
        {
            var mock = new Mock<ISessionService>();
            mock.Setup(s => s.CancelSessionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancelSessionDto>()))
                .ReturnsAsync((Session?)null);  // service refuses

            var controller = CreateController(mock, CreateUser(Guid.NewGuid(), false));

            var result = await controller.CancelSession(Guid.NewGuid(), new CancelSessionDto());

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ---------------------------------------------------------------
        // 2. Cannot cancel completed sessions (Stopped != null)
        // ---------------------------------------------------------------
        [Fact]
        public async Task Cancel_Fails_When_Session_Already_Completed()
        {
            var mock = new Mock<ISessionService>();
            mock.Setup(s => s.CancelSessionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancelSessionDto>()))
                .ReturnsAsync((Session?)null);

            var controller = CreateController(mock, CreateUser(Guid.NewGuid(), false));

            var result = await controller.CancelSession(Guid.NewGuid(), new CancelSessionDto());

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ---------------------------------------------------------------
        // 3. Cannot cancel if already cancelled
        // ---------------------------------------------------------------
        [Fact]
        public async Task Cancel_Fails_When_Already_Cancelled()
        {
            var mock = new Mock<ISessionService>();
            mock.Setup(s => s.CancelSessionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancelSessionDto>()))
                .ReturnsAsync((Session?)null);

            var controller = CreateController(mock, CreateUser(Guid.NewGuid(), false));

            var result = await controller.CancelSession(Guid.NewGuid(), new CancelSessionDto());

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ---------------------------------------------------------------
        // 4. Successful cancel → Session returned with updated fields
        // ---------------------------------------------------------------
        [Fact]
        public async Task Cancel_Succeeds_And_Returns_Updated_Session()
        {
            var updated = new Session
            {
                Id = Guid.NewGuid(),
                IsCancelled = true,
                CancelledAt = DateTimeOffset.UtcNow
            };

            var mock = new Mock<ISessionService>();
            mock.Setup(s => s.CancelSessionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancelSessionDto>()))
                .ReturnsAsync(updated);

            var controller = CreateController(mock, CreateUser(Guid.NewGuid(), true));

            var result = await controller.CancelSession(Guid.NewGuid(), new CancelSessionDto());

            var ok = Assert.IsType<OkObjectResult>(result);
            dynamic value = ok.Value!;
            Assert.True(value.IsCancelled);
        }

        // ---------------------------------------------------------------
        // 5. Unauthenticated → Unauthorized
        // ---------------------------------------------------------------
        [Fact]
        public async Task Cancel_Fails_When_Not_Authenticated()
        {
            var mock = new Mock<ISessionService>();
            var controller = new SessionController(mock.Object, Mock.Of<IEncryptionService>())
            {
                ControllerContext = new()
                {
                    HttpContext = new DefaultHttpContext()  // no user
                }
            };

            var result = await controller.CancelSession(Guid.NewGuid(), new CancelSessionDto());

            Assert.IsType<UnauthorizedResult>(result);
        }

        // ---------------------------------------------------------------
        // 6. Admin override with reason
        // ---------------------------------------------------------------
        [Fact]
        public async Task Admin_Can_Cancel_With_Reason()
        {
            var mock = new Mock<ISessionService>();

            mock.Setup(s => s.CancelSessionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancelSessionDto>()))
                .ReturnsAsync(new Session { Id = Guid.NewGuid(), IsCancelled = true });

            var controller = CreateController(mock, CreateUser(Guid.NewGuid(), true));

            var dto = new CancelSessionDto { Reason = "Admin override" };

            var result = await controller.CancelSession(Guid.NewGuid(), dto);

            Assert.IsType<OkObjectResult>(result);
        }

        // ---------------------------------------------------------------
        // 7. Success → Confirmation (Ok result)
        // ---------------------------------------------------------------
        [Fact]
        public async Task Cancel_Returns_Ok_On_Success()
        {
            var mock = new Mock<ISessionService>();

            mock.Setup(s => s.CancelSessionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancelSessionDto>()))
                .ReturnsAsync(new Session { Id = Guid.NewGuid(), IsCancelled = true });

            var controller = CreateController(mock, CreateUser(Guid.NewGuid(), true));

            var result = await controller.CancelSession(Guid.NewGuid(), new CancelSessionDto());

            Assert.IsType<OkObjectResult>(result);
        }
    }
}

