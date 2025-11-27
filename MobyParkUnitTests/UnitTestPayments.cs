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
            var serviceMock = new Mock<IPaymentService>();
            var payment = new Payment
            {
                Transaction = "tx123",
                Amount = 16.5m,
                Hash = "correcthash",
                Initiator = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Created_At = DateTimeOffset.Parse("2020-03-26T05:40:15Z"),
                Completed = null,
                T_Data = "{\"note\":\"done\"}",
                Session_Id = "1",
                Parking_Lot_Id = "1"
            };


            serviceMock.Setup(s => s.CompletePaymentAsync(It.IsAny<System.Guid>(), "tx123", It.IsAny<PaymentValidationDto>()))
                       .ReturnsAsync(payment);

            var controller = CreateController(serviceMock);
            var dto = new PaymentValidationDto
            {
                T_Data = System.Text.Json.JsonDocument.Parse("{\"note\":\"done\"}").RootElement,
                Validation = "correcthash"
            };

            var result = await controller.CompletePayment("tx123", dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedPayment = Assert.IsType<Payment>(okResult.Value);

            Assert.Equal(payment.Transaction, returnedPayment.Transaction);
            Assert.Equal(payment.Amount, returnedPayment.Amount);
            Assert.Equal(payment.Initiator, returnedPayment.Initiator);
            Assert.Equal(payment.T_Data, returnedPayment.T_Data);
            Assert.Null(returnedPayment.Completed);
        }
    }
}
