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
    public class MemberControllerTests
    {
        private readonly PowerUpDbContext _context;

        public MemberControllerTests()
        {
            var options = new DbContextOptionsBuilder<PowerUpDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new PowerUpDbContext(options);
        }

        private MemberController CreateControllerWithUser(string role = "Admin")
        {
            var controller = new MemberController(_context);

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

        // --- GET ALL MEMBERS ---
        [Fact]
        public async Task GetMembers_ShouldReturnAllActiveMembers_WhenAdmin()
        {
            var controller = CreateControllerWithUser("Admin");

            var instructorUser = new User { Id = Guid.NewGuid(), Name = "Coach", Email = "coach@example.com", Password = "Test123!"};
            var instructor = new Instructor { Id = Guid.NewGuid(), UserId = instructorUser.Id, User = instructorUser };
            var user1 = new User { Id = Guid.NewGuid(), Name = "John", Email = "john@example.com", Password = "Test123!" };
            var user2 = new User { Id = Guid.NewGuid(), Name = "Jane", Email = "jane@example.com", Password = "Test123!", IsDeleted = true };
            var member1 = new Member { Id = Guid.NewGuid(), UserId = user1.Id, User = user1, Instructor = instructor };
            var member2 = new Member { Id = Guid.NewGuid(), UserId = user2.Id, User = user2, Instructor = instructor };

            _context.Users.AddRange(user1, user2, instructorUser);
            _context.Instructors.Add(instructor);
            _context.Members.AddRange(member1, member2);
            await _context.SaveChangesAsync();

            var result = await controller.GetMembers();

            var actionResult = Assert.IsType<ActionResult<IEnumerable<object>>>(result);
            var members = Assert.IsAssignableFrom<IEnumerable<object>>(actionResult.Value);
            Assert.Single(members); // only user1 (active)
        }

        [Fact]
        public async Task GetMembers_ShouldFail_WhenNotAdmin()
        {
            var controller = CreateControllerWithUser("Member");
            var role = controller.User.FindFirst(ClaimTypes.Role)?.Value;
            Assert.NotEqual("Admin", role);
        }

        // --- GET MEMBER BY ID ---
        [Fact]
        public async Task GetMember_ShouldReturnMember_WhenExists()
        {
            var controller = CreateControllerWithUser("Admin");

            var instructorUser = new User { Id = Guid.NewGuid(), Name = "Coach", Email = "coach@example.com", Password = "Test123!" };
            var instructor = new Instructor { Id = Guid.NewGuid(), UserId = instructorUser.Id, User = instructorUser };
            var user = new User { Id = Guid.NewGuid(), Name = "Sam", Email = "sam@example.com", Password = "Test123!" };
            var member = new Member { Id = Guid.NewGuid(), UserId = user.Id, User = user, Instructor = instructor };

            _context.Users.AddRange(user, instructorUser);
            _context.Instructors.Add(instructor);
            _context.Members.Add(member);
            await _context.SaveChangesAsync();

            var result = await controller.GetMember(member.Id);
            var response = Assert.IsType<ActionResult<object>>(result);
            Assert.NotNull(response.Value);
        }

        [Fact]
        public async Task GetMember_ShouldReturnNotFound_WhenDeletedUser()
        {
            var controller = CreateControllerWithUser("Admin");
            var user = new User { Id = Guid.NewGuid(), Name = "DeletedUser", Email = "deleted@example.com", Password = "Test123!", IsDeleted = true };
            var member = new Member { Id = Guid.NewGuid(), UserId = user.Id, User = user };
            _context.Users.Add(user);
            _context.Members.Add(member);
            await _context.SaveChangesAsync();

            var result = await controller.GetMember(member.Id);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        // --- CREATE MEMBER ---
        [Fact]
        public async Task CreateMember_ShouldReturnCreated_WhenAdmin()
        {
            var controller = CreateControllerWithUser("Admin");

            var user = new User { Id = Guid.NewGuid(), Name = "Lisa", Email = "lisa@example.com", Password = "Test123!" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var request = new CreateMemberRequest { UserId = user.Id };

            var result = await controller.CreateMember(request);
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var member = Assert.IsType<Member>(created.Value);
            Assert.Equal(user.Id, member.UserId);
        }

        [Fact]
        public async Task CreateMember_ShouldFail_WhenUserDeleted()
        {
            var controller = CreateControllerWithUser("Admin");

            var user = new User { Id = Guid.NewGuid(), Name = "Bob", Email = "bob@example.com", Password = "Test123!", IsDeleted = true };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var request = new CreateMemberRequest { UserId = user.Id };
            var result = await controller.CreateMember(request);

            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal("User not found", notFound.Value);
        }

        [Fact]
        public async Task CreateMember_ShouldFail_WhenNotAdmin()
        {
            var controller = CreateControllerWithUser("Member");
            var user = new User { Id = Guid.NewGuid(), Name = "NonAdmin", Email = "nonadmin@example.com", Password = "Test123!" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var request = new CreateMemberRequest { UserId = user.Id };
            var role = controller.User.FindFirst(ClaimTypes.Role)?.Value;

            Assert.NotEqual("Admin", role);
        }

        // --- UPDATE MEMBER ---
        [Fact]
        public async Task UpdateMember_ShouldUpdateInstructorAndStatus()
        {
            var controller = CreateControllerWithUser("Member");

            var instructor1 = new Instructor { Id = Guid.NewGuid(), User = new User { Id = Guid.NewGuid(), Name = "Coach1", Email = "c1@example.com", Password = "Test123!" } };
            var instructor2 = new Instructor { Id = Guid.NewGuid(), User = new User { Id = Guid.NewGuid(), Name = "Coach2", Email = "c2@example.com", Password = "Test123!" } };
            var user = new User { Id = Guid.NewGuid(), Name = "Member1", Email = "m1@example.com", Password = "Test123!" };
            var member = new Member { Id = Guid.NewGuid(), UserId = user.Id, User = user, InstructorId = instructor1.Id, IsActive = true };

            _context.Users.Add(user);
            _context.Instructors.AddRange(instructor1, instructor2);
            _context.Members.Add(member);
            await _context.SaveChangesAsync();

            var request = new UpdateMemberRequest { InstructorId = instructor2.Id, IsActive = false };

            var result = await controller.UpdateMember(member.Id, request);
            Assert.IsType<NoContentResult>(result);

            var updated = await _context.Members.FindAsync(member.Id);
            Assert.Equal(instructor2.Id, updated.InstructorId);
            Assert.False(updated.IsActive);
        }

        [Fact]
        public async Task UpdateMember_ShouldReturnNotFound_WhenMissing()
        {
            var controller = CreateControllerWithUser("Member");
            var request = new UpdateMemberRequest { IsActive = true };
            var result = await controller.UpdateMember(Guid.NewGuid(), request);
            Assert.IsType<NotFoundResult>(result);
        }

        // --- DELETE MEMBER ---
        [Fact]
        public async Task DeleteMember_ShouldSoftDeleteUser_WhenAdmin()
        {
            var controller = CreateControllerWithUser("Admin");

            var user = new User { Id = Guid.NewGuid(), Name = "DeleteMe", Email = "del@example.com", Password = "Test123!" };
            var member = new Member { Id = Guid.NewGuid(), UserId = user.Id, User = user };
            _context.Users.Add(user);
            _context.Members.Add(member);
            await _context.SaveChangesAsync();

            var result = await controller.DeleteMember(member.Id);

            Assert.IsType<NoContentResult>(result);
            var updatedUser = await _context.Users.FindAsync(user.Id);
            Assert.True(updatedUser.IsDeleted);
            Assert.NotNull(updatedUser.DeletedAt);
        }

        [Fact]
        public async Task DeleteMember_ShouldFail_WhenNotAdmin()
        {
            var controller = CreateControllerWithUser("Member");
            var user = new User { Id = Guid.NewGuid(), Name = "NonAdminDel", Email = "na@example.com", Password = "Test123!" };
            var member = new Member { Id = Guid.NewGuid(), UserId = user.Id, User = user };
            _context.Users.Add(user);
            _context.Members.Add(member);
            await _context.SaveChangesAsync();

            var role = controller.User.FindFirst(ClaimTypes.Role)?.Value;
            Assert.NotEqual("Admin", role);
        }
    }
}
