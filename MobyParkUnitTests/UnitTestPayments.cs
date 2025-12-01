using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using MobyPark.Controllers;
using MobyPark.Services;
using MobyPark.Entities;
using MobyPark.Models;
using System.Collections.Generic;
using System.Text.Json;

namespace MobyPark.Tests
{
    public class PaymentsControllerTests
    {
        private PaymentsController CreateController(Mock<IPaymentService> paymentServiceMock, string userId = "00000000-0000-0000-0000-000000000001")
        {
            var context = new DefaultHttpContext();
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            }, "mock"));

            context.User = claimsPrincipal;

            var controller = new PaymentsController(null, paymentServiceMock.Object)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = context
                }
            };

            return controller;
        }


        [Fact]
        public async Task CompletePayment_Returns_BadRequest_WhenTDataMissing()
        {
            var serviceMock = new Mock<IPaymentService>();
            var controller = CreateController(serviceMock);
            var dto = new PaymentValidationDto { Validation = "hash123" }; // no T_Data

            var result = await controller.CompletePayment("tx123", dto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("t_data field is missing", badRequest.Value);
        }

        [Fact]
        public async Task CompletePayment_Returns_BadRequest_WhenValidationMissing()
        {
            var serviceMock = new Mock<IPaymentService>();
            var controller = CreateController(serviceMock);
            var dto = new PaymentValidationDto { T_Data = new System.Text.Json.JsonElement() }; // dummy JsonElement

            var result = await controller.CompletePayment("tx123", dto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("validation field is missing", badRequest.Value);
        }

        [Fact]
        public async Task CompletePayment_Returns_NotFound_WhenPaymentNotFound()
        {
            var serviceMock = new Mock<IPaymentService>();
            serviceMock.Setup(s => s.CompletePaymentAsync(It.IsAny<System.Guid>(), "tx123", It.IsAny<PaymentValidationDto>()))
                       .ThrowsAsync(new KeyNotFoundException("Payment not found"));

            var controller = CreateController(serviceMock);
            var dto = new PaymentValidationDto
            {
                T_Data = System.Text.Json.JsonDocument.Parse("{\"note\":\"done\"}").RootElement,
                Validation = "hash123"
            };

            var result = await controller.CompletePayment("tx123", dto);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Payment not found", notFound.Value);
        }

        [Fact]
        public async Task CompletePayment_Returns_Unauthorized_WhenHashInvalid()
        {
            var serviceMock = new Mock<IPaymentService>();
            serviceMock.Setup(s => s.CompletePaymentAsync(It.IsAny<System.Guid>(), "tx123", It.IsAny<PaymentValidationDto>()))
                       .ThrowsAsync(new UnauthorizedAccessException("Validation failed"));

            var controller = CreateController(serviceMock);
            var dto = new PaymentValidationDto
            {
                T_Data = System.Text.Json.JsonDocument.Parse("{\"note\":\"done\"}").RootElement,
                Validation = "wronghash"
            };

            var result = await controller.CompletePayment("tx123", dto);

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Validation failed", unauthorized.Value);
        }

        [Fact]
        public async Task CompletePayment_Returns_FullPayment_OnSuccess()
        {
            // Arrange
            var userId = Guid.Parse("00000000-0000-0000-0000-000000000001");

            // Create the DTO that the service will return
            var paymentDto = new PaymentResponseDto
            {
                Transaction = "tx123",
                Amount = 16.5m,
                Hash = "correcthash",
                Initiator = userId.ToString(),
                Completed = null, // Keep null to simulate not completed yet
                User = new UserReadDto
                {
                    Id = userId,
                    Username = "user1",
                    Name = "Test User",
                    Email = "test@example.com",
                    PhoneNumber = "123456789",
                    BirthYear = 1990,
                    Role = UserRole.Customer,
                    CreatedAt = DateTimeOffset.UtcNow
                }
            };

            // Mock the IPaymentService
            var serviceMock = new Mock<IPaymentService>();
            serviceMock
                .Setup(s => s.CompletePaymentAsync(userId, "tx123", It.IsAny<PaymentValidationDto>()))
                .ReturnsAsync(paymentDto);

            // Pass the mock object (.Object) to the controller
            var controller = CreateController(serviceMock);

            // Prepare input DTO
            var requestDto = new PaymentValidationDto
            {
                T_Data = JsonDocument.Parse("{\"note\":\"done\"}").RootElement,
                Validation = "correcthash"
            };

            // Act
            var result = await controller.CompletePayment("tx123", requestDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedPayment = Assert.IsType<PaymentResponseDto>(okResult.Value);

            Assert.Equal(paymentDto.Transaction, returnedPayment.Transaction);
            Assert.Equal(paymentDto.Amount, returnedPayment.Amount);
            Assert.Equal(paymentDto.Initiator, returnedPayment.Initiator);
            Assert.Equal(paymentDto.Hash, returnedPayment.Hash);

            // Check User fields
            Assert.NotNull(returnedPayment.User);
            Assert.Equal(paymentDto.User!.Id, returnedPayment.User.Id);
            Assert.Equal(paymentDto.User.Username, returnedPayment.User.Username);
            Assert.Equal(paymentDto.User.Name, returnedPayment.User.Name);
            Assert.Equal(paymentDto.User.Email, returnedPayment.User.Email);

            // Completed is still null
            Assert.Null(returnedPayment.Completed);
        }

    }
}
