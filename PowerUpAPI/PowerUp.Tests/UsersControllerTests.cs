using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using PowerUp.Controllers;
using PowerUp.Data;
using PowerUp.Models;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;

namespace PowerUp.Tests
{
    public class UsersControllerTests
    {
        private readonly PowerUpDbContext _context;

        public UsersControllerTests()
        {
            var options = new DbContextOptionsBuilder<PowerUpDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new PowerUpDbContext(options);
        }

        private UsersController CreateController(string role = "Admin", Guid? userId = null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.NameIdentifier, (userId ?? Guid.NewGuid()).ToString())
            };

            var controller = new UsersController(_context)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(claims, "mock"))
                    }
                }
            };

            return controller;
        }

        // --- GET ALL ---
        [Fact]
        public async Task GetUsers_ShouldReturnActiveUsers_OnlyForAdmin()
        {
            var controller = CreateController("Admin");

            var users = new List<User>
            {
                new User { Id = Guid.NewGuid(), Name = "Alice", Email = "alice@test.com", Password = "12345", IsDeleted = false },
                new User { Id = Guid.NewGuid(), Name = "Bob", Email = "bob@test.com", Password = "12345", IsDeleted = true }
            };
            _context.Users.AddRange(users);
            await _context.SaveChangesAsync();

            var result = await controller.GetUsers();

            var actionResult = Assert.IsType<ActionResult<IEnumerable<User>>>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<User>>(actionResult.Value);
            Assert.Single(list);
            Assert.Equal("Alice", list.First().Name);
        }

        // --- GET BY ID ---
        [Fact]
        public async Task GetUser_ShouldReturnSelfProfile_WhenUserAccessesOwnAccount()
        {
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Name = "Selfie", Email = "selfie@test.com", Password = "12345" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var controller = CreateController("User", userId);

            var result = await controller.GetUser(userId);
            var actionResult = Assert.IsType<ActionResult<User>>(result);
            Assert.Equal("Selfie", actionResult.Value.Name);
        }

        [Fact]
        public async Task GetUser_ShouldForbid_WhenUserAccessesAnotherProfile()
        {
            var user1 = new User { Id = Guid.NewGuid(), Name = "Alice", Email = "alice@test.com", Password = "12345" };
            var user2 = new User { Id = Guid.NewGuid(), Name = "Bob", Email = "bob@test.com", Password = "12345" };
            _context.Users.AddRange(user1, user2);
            await _context.SaveChangesAsync();

            var controller = CreateController("User", user1.Id);
            var result = await controller.GetUser(user2.Id);

            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task GetUser_ShouldReturnNotFound_WhenMissing()
        {
            var controller = CreateController("Admin");
            var result = await controller.GetUser(Guid.NewGuid());
            Assert.IsType<NotFoundResult>(result.Result);
        }

        // --- CREATE ---
        [Fact]
        public async Task CreateUser_ShouldReturnCreated_WhenAdmin()
        {
            var controller = CreateController("Admin");
            var newUser = new User { Id = Guid.NewGuid(), Name = "NewUser", Email = "new@test.com", Password = "12345" };

            var result = await controller.CreateUser(newUser);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returned = Assert.IsType<User>(created.Value);
            Assert.Equal("NewUser", returned.Name);
            Assert.Equal(string.Empty, returned.Password);
        }

        // --- UPDATE ---
        [Fact]
        public async Task UpdateUser_ShouldReturnNoContent_WhenUserUpdatesSelf()
        {
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Name = "Old", Email = "old@test.com", Password = "12345" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var controller = CreateController("User", userId);
            user.Name = "Updated";

            var result = await controller.UpdateUser(userId, user);
            Assert.IsType<NoContentResult>(result);

            var updated = await _context.Users.FindAsync(userId);
            Assert.Equal("Updated", updated.Name);
        }

        [Fact]
        public async Task UpdateUser_ShouldForbid_WhenNonAdminUpdatesOtherUser()
        {
            var user1 = new User { Id = Guid.NewGuid(), Name = "A", Email = "a@test.com", Password = "12345" };
            var user2 = new User { Id = Guid.NewGuid(), Name = "B", Email = "b@test.com", Password = "12345" };
            _context.Users.AddRange(user1, user2);
            await _context.SaveChangesAsync();

            var controller = CreateController("User", user1.Id);
            var result = await controller.UpdateUser(user2.Id, user2);
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task UpdateUser_ShouldReturnBadRequest_WhenIdMismatch()
        {
            var user = new User { Id = Guid.NewGuid(), Name = "Mismatch", Email = "m@test.com", Password = "12345" };
            var controller = CreateController("Admin");
            var result = await controller.UpdateUser(Guid.NewGuid(), user);
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task UpdateUser_ShouldReturnNotFound_WhenMissing()
        {
            var user = new User { Id = Guid.NewGuid(), Name = "Ghost", Email = "ghost@test.com", Password = "12345" };
            var controller = CreateController("Admin");
            var result = await controller.UpdateUser(user.Id, user);
            Assert.IsType<NotFoundResult>(result);
        }

        // --- PROMOTE ---
        [Fact]
        public async Task PromoteToInstructor_ShouldSetRole_WhenUserExists()
        {
            var user = new User { Id = Guid.NewGuid(), Role = UserRole.Member, Name = "New Instructor", Email = "member@test.com", Password = "12345" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var controller = CreateController("Admin");
            var result = await controller.PromoteToInstructor(user.Id);

            Assert.IsType<OkObjectResult>(result);
        }

        // --- DISTRIBUTION ---
        [Fact]
        public void GetUserDistribution_ShouldReturnGroupedCounts()
        {
            _context.Users.AddRange(
                new User { Id = Guid.NewGuid(), Role = UserRole.Member, Name = "Member 1", Email = "m1@test.com", Password = "12345" },
                new User { Id = Guid.NewGuid(), Role = UserRole.Member, Name = "Member 2", Email = "m2@test.com", Password = "12345" },
                new User { Id = Guid.NewGuid(), Role = UserRole.Instructor, Name = "Instructor 1", Email = "i1@test.com", Password = "12345" }
            );
            _context.SaveChanges();

            var controller = CreateController("Admin");
            var result = controller.GetUserDistribution();
            var okResult = Assert.IsType<OkObjectResult>(result);

            var distribution = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            Assert.Contains(distribution, d => d.ToString().Contains("Member"));
        }

        // --- DELETE ---
        [Fact]
        public async Task DeleteUser_ShouldSoftDelete_WhenAdmin()
        {
            var user = new User { Id = Guid.NewGuid(), Name = "DeleteMe", Email = "delete@test.com", Password = "12345" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var controller = CreateController("Admin");
            var result = await controller.DeleteUser(user.Id);
            Assert.IsType<NoContentResult>(result);

            var deleted = await _context.Users.FindAsync(user.Id);
            Assert.True(deleted.IsDeleted);
            Assert.NotNull(deleted.DeletedAt);
        }

        [Fact]
        public async Task DeleteUser_ShouldReturnNotFound_WhenMissing()
        {
            var controller = CreateController("Admin");
            var result = await controller.DeleteUser(Guid.NewGuid());
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
