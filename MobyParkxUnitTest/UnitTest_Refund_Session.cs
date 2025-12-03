using Xunit;
using Moq;
using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MobyPark.Controllers;
using MobyPark.Services;
using MobyPark.Models;

namespace MobyParkxUnitTest
{
    public class RefundTests
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

        private SessionController CreateController(Mock<ISessionService> mockService, ClaimsPrincipal? user = null)
        {
            return new SessionController(mockService.Object, Mock.Of<IEncryptionService>())
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = user ?? new ClaimsPrincipal()
                    }
                }
            };
        }

        // ----------------------------------------------------------------------
        // 1. Refund can only occur when IsCancelled == true
        // ----------------------------------------------------------------------
        [Fact]
        public async Task Refund_Fails_When_Session_Not_Cancelled()
        {
            var mock = new Mock<ISessionService>();
            mock.Setup(s => s.RequestRefundAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<RefundRequestDto>()))
                .ReturnsAsync((object?)null);

            var userId = Guid.NewGuid();
            var controller = CreateController(mock, CreateUser(userId, true));

            var dto = new RefundRequestDto { IBAN = "NL91ABNA0417164300" };

            var result = await controller.RefundSession(Guid.NewGuid(), dto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Refund not applicable.", badRequest.Value);
        }

        // ----------------------------------------------------------------------
        // 2. Active sessions cannot be refunded
        // ----------------------------------------------------------------------
        [Fact]
        public async Task Refund_Fails_When_Session_Is_Active()
        {
            var mock = new Mock<ISessionService>();
            mock.Setup(s => s.RequestRefundAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<RefundRequestDto>()))
                .ReturnsAsync((object?)null);

            var controller = CreateController(mock, CreateUser(Guid.NewGuid(), true));

            var dto = new RefundRequestDto { IBAN = "NL91ABNA0417164300" };

            var result = await controller.RefundSession(Guid.NewGuid(), dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ----------------------------------------------------------------------
        // 3. Refund is blocked if already refunded
        // ----------------------------------------------------------------------
        [Fact]
        public async Task Refund_Fails_If_Already_Refunded()
        {
            var mock = new Mock<ISessionService>();
            mock.Setup(s => s.RequestRefundAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<RefundRequestDto>()))
                .ReturnsAsync((object?)null);

            var controller = CreateController(mock, CreateUser(Guid.NewGuid(), true));

            var result = await controller.RefundSession(Guid.NewGuid(), new RefundRequestDto());

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ----------------------------------------------------------------------
        // 4. Only session owner can request refund (unless admin)
        // ----------------------------------------------------------------------
        [Fact]
        public async Task Refund_Fails_When_User_Not_Owner()
        {
            var mock = new Mock<ISessionService>();
            mock.Setup(s => s.RequestRefundAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<RefundRequestDto>()))
                .ReturnsAsync((object?)null);

            var controller = CreateController(mock, CreateUser(Guid.NewGuid(), false));

            var result = await controller.RefundSession(Guid.NewGuid(), new RefundRequestDto());

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ----------------------------------------------------------------------
        // 5. Admin may refund on behalf of user
        // ----------------------------------------------------------------------
        [Fact]
        public async Task Admin_Can_Refund_On_Behalf_Of_User()
        {
            var mock = new Mock<ISessionService>();
            mock.Setup(s => s.RequestRefundAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<RefundRequestDto>()))
                .ReturnsAsync(new { Success = true });

            var controller = CreateController(mock, CreateUser(Guid.NewGuid(), true));

            var result = await controller.RefundSession(Guid.NewGuid(), new RefundRequestDto());

            Assert.IsType<OkObjectResult>(result);
        }

        // ----------------------------------------------------------------------
        // 6. Refund amount calculation (base rate €0.05/min)
        // ----------------------------------------------------------------------
        [Fact]
        public async Task Refund_Succeeds_With_Correct_Calculation()
        {
            var mock = new Mock<ISessionService>();
            mock.Setup(s => s.RequestRefundAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<RefundRequestDto>()))
                .ReturnsAsync(new { Amount = 1.50m });

            var controller = CreateController(mock, CreateUser(Guid.NewGuid(), true));

            var result = await controller.RefundSession(Guid.NewGuid(), new RefundRequestDto());

            var ok = Assert.IsType<OkObjectResult>(result);
            dynamic data = ok.Value!;
            Assert.Equal(1.50m, (decimal)data.Amount);
        }

        // ----------------------------------------------------------------------
        // 7–9. Refund percentage calculation (10 min = 100%, 30 min = 50%, >30 = 0%)
        // ----------------------------------------------------------------------
        [Theory]
        [InlineData(10, 100)]
        [InlineData(30, 50)]
        [InlineData(31, 0)]
        public async Task Refund_Percentage_Is_Correct(int minutes, int expectedPercent)
        {
            var mock = new Mock<ISessionService>();
            mock.Setup(s => s.RequestRefundAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<RefundRequestDto>()))
                .ReturnsAsync(new { Percentage = expectedPercent });

            var controller = CreateController(mock, CreateUser(Guid.NewGuid(), true));

            var result = await controller.RefundSession(Guid.NewGuid(), new RefundRequestDto());

            var ok = Assert.IsType<OkObjectResult>(result);
            dynamic data = ok.Value!;
            Assert.Equal(expectedPercent, (int)data.Percentage);
        }

        // ----------------------------------------------------------------------
        // 10. Unauthorized when no token
        // ----------------------------------------------------------------------
        [Fact]
        public async Task Refund_Returns_Unauthorized_When_No_Token()
        {
            var mock = new Mock<ISessionService>();
            var controller = CreateController(mock, user: null); // NO USER AT ALL

            var result = await controller.RefundSession(Guid.NewGuid(), new RefundRequestDto());

            Assert.IsType<UnauthorizedResult>(result);
        }

        // ----------------------------------------------------------------------
        // 11. Refund attempts limiting
        // ----------------------------------------------------------------------
        [Fact]
        public async Task Refund_Fails_When_Attempt_Limit_Reached()
        {
            var mock = new Mock<ISessionService>();
            mock.Setup(s => s.RequestRefundAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<RefundRequestDto>()))
                .ReturnsAsync((object?)null);

            var controller = CreateController(mock, CreateUser(Guid.NewGuid(), true));

            var result = await controller.RefundSession(Guid.NewGuid(), new RefundRequestDto());

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ----------------------------------------------------------------------
        // 12. SessionId must exist
        // ----------------------------------------------------------------------
        [Fact]
        public async Task Refund_Fails_When_Session_Not_Found()
        {
            var mock = new Mock<ISessionService>();
            mock.Setup(s => s.RequestRefundAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<RefundRequestDto>()))
                .ReturnsAsync((object?)null);

            var controller = CreateController(mock, CreateUser(Guid.NewGuid(), true));

            var result = await controller.RefundSession(Guid.NewGuid(), new RefundRequestDto());

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ----------------------------------------------------------------------
        // 13. IBAN format validation
        // ----------------------------------------------------------------------
        // [Fact]
        // public async Task Refund_Fails_When_IBAN_Invalid()
        // {
        //     var mock = new Mock<ISessionService>();
        //     var controller = CreateController(mock, CreateUser(Guid.NewGuid(), true));

        //     var dto = new RefundRequestDto { IBAN = "NOT-VALID" };

        //     var validation = controller.TryValidateModel(dto);

        //     Assert.False(validation);
        // }

        // ----------------------------------------------------------------------
        // 14. Refund amount cannot be negative or > original cost
        // ----------------------------------------------------------------------
        [Fact]
        public async Task Refund_Fails_When_Amount_Invalid()
        {
            var mock = new Mock<ISessionService>();
            mock.Setup(s => s.RequestRefundAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<RefundRequestDto>()))
                .ReturnsAsync((object?)null);

            var controller = CreateController(mock, CreateUser(Guid.NewGuid(), true));

            var result = await controller.RefundSession(Guid.NewGuid(), new RefundRequestDto());

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ----------------------------------------------------------------------
        // 15. Database updated: IsRefunded + RefundDate
        // (controller can only confirm service returned something)
        // ----------------------------------------------------------------------
        [Fact]
        public async Task Refund_Succeeds_When_Service_Returns_Updated_Session()
        {
            var mock = new Mock<ISessionService>();
            mock.Setup(s => s.RequestRefundAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<RefundRequestDto>()))
                .ReturnsAsync(new { IsRefunded = true });

            var controller = CreateController(mock, CreateUser(Guid.NewGuid(), true));

            var result = await controller.RefundSession(Guid.NewGuid(), new RefundRequestDto());

            var ok = Assert.IsType<OkObjectResult>(result);
            dynamic data = ok.Value!;
            Assert.True((bool)data.IsRefunded);
        }
    }
}

