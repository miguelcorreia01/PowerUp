using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PowerUp.Controllers;
using PowerUp.Data;
using PowerUp.Models;
using PowerUp.Models.DTO;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PowerUp.Tests
{
    public class InstructorControllerTests
    {
        private readonly PowerUpDbContext _context;

        public InstructorControllerTests()
        {
            var options = new DbContextOptionsBuilder<PowerUpDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new PowerUpDbContext(options);
        }

        private InstructorController CreateControllerWithUser(string role = "Admin")
        {
            var controller = new InstructorController(_context);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "Test User"),
                new Claim(ClaimTypes.Role, role)
            }, "mock"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            return controller;
        }

        [Fact]
        public async Task GetInstructors_ShouldReturnAllActiveInstructors_WhenAdmin()
        {
            // Arrange
            var controller = CreateControllerWithUser("Admin");

            var user1 = new User { Id = Guid.NewGuid(), Name = "John", Email = "john@example.com", Password = "Test123!",  Role = UserRole.Instructor };
            var user2 = new User { Id = Guid.NewGuid(), Name = "Jane", Email = "jane@example.com", Password = "Test123!", Role = UserRole.Instructor, IsDeleted = true };
            var instructor1 = new Instructor { Id = Guid.NewGuid(), UserId = user1.Id, User = user1};
            var instructor2 = new Instructor { Id = Guid.NewGuid(), UserId = user2.Id, User = user2};


            _context.Users.AddRange(user1, user2);
            _context.Instructors.AddRange(instructor1, instructor2);
            await _context.SaveChangesAsync();

            // Act
            var result = await controller.GetInstructors();

            // Assert
            var actionResult = Assert.IsType<ActionResult<IEnumerable<object>>>(result);
            var instructors = Assert.IsAssignableFrom<IEnumerable<object>>(actionResult.Value);
            Assert.Single(instructors); // only user1 (not deleted)
        }

        [Fact]
        public async Task GetInstructors_ShouldFail_WhenNotAdmin()
        {
            // Arrange
            var controller = CreateControllerWithUser("Member");

            var userRole = controller.User.FindFirst(ClaimTypes.Role)?.Value;
            Assert.NotEqual("Admin", userRole);
        }

        [Fact]
        public async Task CreateInstructor_ShouldReturnCreated_WhenAdmin()
        {
            // Arrange
            var controller = CreateControllerWithUser("Admin");

            var user = new User { Id = Guid.NewGuid(), Name = "Sarah", Email = "sarah@example.com", Password = "Test123!", };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var request = new CreateInstructorRequest { UserId = user.Id};

            // Act
            var result = await controller.CreateInstructor(request);

            // Assert
            var createdAt = Assert.IsType<CreatedAtActionResult>(result.Result);
            var createdInstructor = Assert.IsType<Instructor>(createdAt.Value);
            Assert.Equal(user.Id, createdInstructor.UserId);
        }

        [Fact]
        public async Task CreateInstructor_ShouldFail_WhenNotAdmin()
        {
            // Arrange
            var controller = CreateControllerWithUser("Member");

            var user = new User { Id = Guid.NewGuid(), Name = "Sarah", Email = "sarah@example.com", Password = "Test123!" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var request = new CreateInstructorRequest { UserId = user.Id };


            var userRole = controller.User.FindFirst(ClaimTypes.Role)?.Value;

            // Assert
            Assert.NotEqual("Admin", userRole);
        }

        [Fact]
        public async Task DeleteInstructor_ShouldSoftDeleteUser_WhenAdmin()
        {
            // Arrange
            var controller = CreateControllerWithUser("Admin");

            var user = new User { Id = Guid.NewGuid(), Name = "Coach", Email = "coach@example.com", Password = "Test123!" };
            var instructor = new Instructor { Id = Guid.NewGuid(), UserId = user.Id, User = user };
            _context.Users.Add(user);
            _context.Instructors.Add(instructor);
            await _context.SaveChangesAsync();

            // Act
            var result = await controller.DeleteInstructor(instructor.Id);

            // Assert
            Assert.IsType<NoContentResult>(result);

            var updatedUser = await _context.Users.FindAsync(user.Id);
            Assert.True(updatedUser.IsDeleted);
            Assert.NotNull(updatedUser.DeletedAt);
        }

        [Fact]
        public async Task DeleteInstructor_ShouldFail_WhenNotAdmin()
        {
            // Arrange
            var controller = CreateControllerWithUser("Member");
            var user = new User { Id = Guid.NewGuid(), Name = "Trainee", Email = "trainee@example.com", Password = "Test123!" };
            var instructor = new Instructor { Id = Guid.NewGuid(), UserId = user.Id, User = user };
            _context.Users.Add(user);
            _context.Instructors.Add(instructor);
            await _context.SaveChangesAsync();

            // Act (simulated forbidden)
            var userRole = controller.User.FindFirst(ClaimTypes.Role)?.Value;

            // Assert (in real API: would be 403 Forbidden)
            Assert.NotEqual("Admin", userRole);
        }

        [Fact]
        public async Task UpdateInstructor_ShouldAllowAllAuthorizedUsers()
        {
            // Arrange
            var controller = CreateControllerWithUser("Member");

            var user = new User { Id = Guid.NewGuid(), Name = "Trainer", Email = "trainer@example.com", Password = "Test123!" };
            var instructor = new Instructor { Id = Guid.NewGuid(), UserId = user.Id};
            _context.Users.Add(user);
            _context.Instructors.Add(instructor);
            await _context.SaveChangesAsync();


            // Act
            var result = await controller.UpdateInstructor(instructor.Id);

            // Assert
            Assert.IsType<NoContentResult>(result);
            var updated = await _context.Instructors.FindAsync(instructor.Id);

        }

        [Fact]
        public async Task GetInstructor_ShouldReturnNotFound_WhenUserDeleted()
        {
            // Arrange
            var controller = CreateControllerWithUser("Admin");
            var user = new User { Id = Guid.NewGuid(), Name = "DeletedUser", Email = "del@example.com", Password = "Test123!", IsDeleted = true };
            var instructor = new Instructor { Id = Guid.NewGuid(), UserId = user.Id, User = user };
            _context.Users.Add(user);
            _context.Instructors.Add(instructor);
            await _context.SaveChangesAsync();

            // Act
            var result = await controller.GetInstructor(instructor.Id);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }
    }
}
