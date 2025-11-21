using Microsoft.AspNetCore.Mvc;
using Moq;
using MobyPark.Controllers;
using MobyPark.Models;
using MobyPark.Services;
using MobyPark.Entities;

namespace MobyParkUnitTests
{
    public class UnitTestLogin
    {
        [Fact]
        public async Task Login_ReturnsOk_WhenCredentialsAreValid()
        {
            // Arrange
            var loginRequest = new LoginRequestDto
            {
                Username = "testuser",
                Password = "password123"
            };

            var expectedToken = new TokenResponseDto
            {
                AccessToken = "fake-jwt-token",
                RefreshToken = ""
            };

            var mockService = new Mock<IAuthService>();
            mockService.Setup(s => s.LoginAsync(loginRequest))
                       .ReturnsAsync(expectedToken);

            var controller = new AuthController(mockService.Object);

            // Act
            var result = await controller.Login(loginRequest);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<TokenResponseDto>(ok.Value);
            Assert.Equal("fake-jwt-token", dto.AccessToken);
        }

        [Fact]
        public async Task Login_ReturnsUnauthorized_WhenCredentialsAreInvalid()
        {
            // Arrange
            var loginRequest = new LoginRequestDto
            {
                Username = "wrong",
                Password = "wrong"
            };

            var mockService = new Mock<IAuthService>();
            mockService.Setup(s => s.LoginAsync(loginRequest))
                       .ReturnsAsync((TokenResponseDto?)null);

            var controller = new AuthController(mockService.Object);

            // Act
            var result = await controller.Login(loginRequest);

            // Assert
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            Assert.Equal("Invalid username or password.", unauthorized.Value);
        }
    }
}
