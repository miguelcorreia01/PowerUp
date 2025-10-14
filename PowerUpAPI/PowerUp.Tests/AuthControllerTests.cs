using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerUp.Controllers;
using PowerUp.Data;
using PowerUp.Models;
using PowerUp.Models.DTO;
using PowerUp.Services;
using System;
using System.Threading.Tasks;

namespace PowerUp.Tests
{
    public class AuthControllerTests
    {
        private readonly PowerUpDbContext _context;
        private readonly Mock<IJwtService> _jwtServiceMock;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            // Setup in-memory DB
            var options = new DbContextOptionsBuilder<PowerUpDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new PowerUpDbContext(options);
            _jwtServiceMock = new Mock<IJwtService>();

            _controller = new AuthController(_context, _jwtServiceMock.Object);
        }

        [Fact]
        public async Task Register_ShouldCreateUser_AndReturnToken()
        {
            // Arrange
            var request = new RegisterRequest
            {
                Name = "John Doe",
                Email = "john@example.com",
                Password = "Password123!",
                PhoneNumber = "123456789"
            };

            _jwtServiceMock.Setup(x => x.GenerateToken(It.IsAny<User>()))
                .Returns("fake-jwt-token");

            // Act
            var result = await _controller.Register(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<LoginResponse>(okResult.Value);

            Assert.Equal(request.Email, response.Email);
            Assert.Equal("Member", response.Role);
            Assert.Equal("fake-jwt-token", response.Token);

            var userInDb = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            Assert.NotNull(userInDb);
            Assert.NotEqual(request.Password, userInDb.Password); // should be hashed
        }

        [Fact]
        public async Task Register_ShouldReturnConflict_WhenEmailAlreadyExists()
        {
            // Arrange
            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                Name = "Jane Doe",
                Email = "jane@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = UserRole.Member
            };
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var request = new RegisterRequest
            {
                Name = "Another User",
                Email = "jane@example.com",
                Password = "Password123!"
            };

            // Act
            var result = await _controller.Register(request);

            // Assert
            var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
            Assert.Equal("Email is already in use.", conflict.Value);
        }

        [Fact]
        public async Task Login_ShouldReturnToken_WhenCredentialsAreValid()
        {
            // Arrange
            var password = "Password123!";
            var hashed = BCrypt.Net.BCrypt.HashPassword(password);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = "John Doe",
                Email = "john@example.com",
                Password = hashed,
                Role = UserRole.Admin
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _jwtServiceMock.Setup(x => x.GenerateToken(user)).Returns("valid-token");

            var request = new LoginRequest
            {
                Email = "john@example.com",
                Password = password
            };

            // Act
            var result = await _controller.Login(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<LoginResponse>(okResult.Value);

            Assert.Equal("valid-token", response.Token);
            Assert.Equal(user.Email, response.Email);
            Assert.Equal("Admin", response.Role);
        }

        [Fact]
        public async Task Login_ShouldReturnUnauthorized_WhenInvalidPassword()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = "John Doe",
                Email = "john@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("CorrectPassword"),
                Role = UserRole.Member
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var request = new LoginRequest
            {
                Email = "john@example.com",
                Password = "WrongPassword"
            };

            // Act
            var result = await _controller.Login(request);

            // Assert
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            Assert.Equal("Invalid email or password.", unauthorized.Value);
        }

        [Fact]
        public async Task Login_ShouldReturnUnauthorized_WhenEmailNotFound()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "missing@example.com",
                Password = "DoesNotMatter"
            };

            // Act
            var result = await _controller.Login(request);

            // Assert
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            Assert.Equal("Invalid email or password.", unauthorized.Value);
        }

        [Fact]
        public void Logout_ShouldReturnSuccessMessage()
        {
            // Act
            var result = _controller.Logout();
            var okResult = Assert.IsType<OkObjectResult>(result);
            var value = okResult.Value;
            string message = value?.GetType().GetProperty("message")?.GetValue(value)?.ToString()
                            ?? value?.GetType().GetProperty("Message")?.GetValue(value)?.ToString();
            Assert.Equal("Logged out successfully", message);
        }
    }
}
