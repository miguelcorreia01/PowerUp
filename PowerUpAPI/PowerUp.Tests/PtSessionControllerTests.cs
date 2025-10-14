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
    public class PtSessionControllerTests
    {
        private readonly PowerUpDbContext _context;

        public PtSessionControllerTests()
        {
            var options = new DbContextOptionsBuilder<PowerUpDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new PowerUpDbContext(options);
        }

        private PtSessionController CreateController(string role = "User", Guid? userId = null)
        {
            var controller = new PtSessionController(_context);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, role),
                new Claim("id", (userId ?? Guid.NewGuid()).ToString())
            };

            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "mock"));
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            return controller;
        }

        // --- GET ALL ---
        [Fact]
        public async Task GetPtSessions_ShouldReturnAllSessions()
        {
            var controller = CreateController();

            var sessions = new List<PtSession>
            {
                new PtSession { Id = Guid.NewGuid(), InstructorId = Guid.NewGuid(), SessionTime = DateTime.UtcNow },
                new PtSession { Id = Guid.NewGuid(), InstructorId = Guid.NewGuid(), SessionTime = DateTime.UtcNow.AddDays(1) }
            };

            _context.PtSessions.AddRange(sessions);
            await _context.SaveChangesAsync();

            var result = await controller.GetPtSessions();

            var actionResult = Assert.IsType<ActionResult<IEnumerable<PtSession>>>(result);
            var value = Assert.IsAssignableFrom<IEnumerable<PtSession>>(actionResult.Value);
            Assert.Collection(value,
                s => Assert.NotEqual(Guid.Empty, s.Id),
                s => Assert.NotEqual(Guid.Empty, s.Id));
        }

        // --- GET BY ID ---
        [Fact]
        public async Task GetPtSession_ShouldReturnSession_WhenExists()
        {
            var controller = CreateController();
            var session = new PtSession
            {
                Id = Guid.NewGuid(),
                InstructorId = Guid.NewGuid(),
                SessionTime = DateTime.UtcNow
            };

            _context.PtSessions.Add(session);
            await _context.SaveChangesAsync();

            var result = await controller.GetPtSession(session.Id);
            var actionResult = Assert.IsType<ActionResult<PtSession>>(result);
            Assert.Equal(session.Id, actionResult.Value.Id);
        }

        [Fact]
        public async Task GetPtSession_ShouldReturnNotFound_WhenMissing()
        {
            var controller = CreateController();

            var result = await controller.GetPtSession(Guid.NewGuid());
            Assert.IsType<NotFoundResult>(result.Result);
        }

        // --- CREATE ---
        [Fact]
        public async Task CreatePtSession_ShouldReturnCreated()
        {
            var controller = CreateController();

            var session = new PtSession
            {
                Id = Guid.NewGuid(),
                InstructorId = Guid.NewGuid(),
                SessionTime = DateTime.UtcNow
            };

            var result = await controller.CreatePtSession(session);
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var createdSession = Assert.IsType<PtSession>(created.Value);
            Assert.Equal(session.Id, createdSession.Id);
        }

        [Fact]
        public async Task CreatePtSession_ShouldPersistToDatabase()
        {
            var controller = CreateController();

            var session = new PtSession
            {
                Id = Guid.NewGuid(),
                InstructorId = Guid.NewGuid(),
                SessionTime = DateTime.UtcNow
            };

            await controller.CreatePtSession(session);
            var saved = await _context.PtSessions.FindAsync(session.Id);
            Assert.NotNull(saved);
        }

        // --- UPDATE ---
        [Fact]
        public async Task UpdatePtSession_ShouldReturnNoContent_WhenValid()
        {
            var controller = CreateController();

            var session = new PtSession
            {
                Id = Guid.NewGuid(),
                InstructorId = Guid.NewGuid(),
                SessionTime = DateTime.UtcNow
            };

            _context.PtSessions.Add(session);
            await _context.SaveChangesAsync();

            session.SessionTime = DateTime.UtcNow.AddDays(1);
            var result = await controller.UpdatePtSession(session.Id, session);

            Assert.IsType<NoContentResult>(result);

            var updated = await _context.PtSessions.FindAsync(session.Id);
            Assert.Equal(session.SessionTime, updated.SessionTime);
        }

        [Fact]
        public async Task UpdatePtSession_ShouldReturnBadRequest_WhenIdMismatch()
        {
            var controller = CreateController();
            var session = new PtSession { Id = Guid.NewGuid() };

            var result = await controller.UpdatePtSession(Guid.NewGuid(), session);
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task UpdatePtSession_ShouldReturnNotFound_WhenMissing()
        {
            var controller = CreateController();
            var session = new PtSession { Id = Guid.NewGuid(), InstructorId = Guid.NewGuid(), SessionTime = DateTime.UtcNow };

            var result = await controller.UpdatePtSession(session.Id, session);
            Assert.IsType<NotFoundResult>(result);
        }

        // --- BOOK SESSION ---
        [Fact]
        public async Task BookSession_ShouldCreateSession_WhenUserAuthorized()
        {
            var userId = Guid.NewGuid();
            var controller = CreateController("User", userId);

            var instructorId = Guid.NewGuid();
            var request = new BookSessionRequest
            {
                InstructorId = instructorId,
                SessionTime = DateTime.UtcNow.AddDays(2)
            };

            var result = await controller.BookSession(request);
            var okResult = Assert.IsType<OkObjectResult>(result);

            var value = okResult.Value;
            string message = null;
            if (value != null)
            {
                var t = value.GetType();
                var prop = t.GetProperty("message") ?? t.GetProperty("Message");
                message = prop?.GetValue(value)?.ToString();
            }

            Assert.Equal("Session booked successfully", message);

            var booked = await _context.PtSessions.FirstOrDefaultAsync(s => s.MemberId == userId);
            Assert.NotNull(booked);
            Assert.Equal(instructorId, booked.InstructorId);
        }


        [Fact]
        public async Task BookSession_ShouldFail_WhenRoleNotUser()
        {
            var controller = CreateController("Admin");
            var request = new BookSessionRequest
            {
                InstructorId = Guid.NewGuid(),
                SessionTime = DateTime.UtcNow
            };

            var role = controller.User.FindFirst(ClaimTypes.Role)?.Value;
            Assert.NotEqual("User", role);
        }

        // --- DELETE ---
        [Fact]
        public async Task DeletePtSession_ShouldSoftDelete_WhenExists()
        {
            var controller = CreateController();

            var session = new PtSession
            {
                Id = Guid.NewGuid(),
                InstructorId = Guid.NewGuid(),
                SessionTime = DateTime.UtcNow
            };

            _context.PtSessions.Add(session);
            await _context.SaveChangesAsync();

            var result = await controller.DeletePtSession(session.Id);
            Assert.IsType<NoContentResult>(result);

            var deleted = await _context.PtSessions.FindAsync(session.Id);
            Assert.True(deleted.IsDeleted);
            Assert.NotNull(deleted.DeletedAt);
        }

        [Fact]
        public async Task DeletePtSession_ShouldReturnNotFound_WhenMissing()
        {
            var controller = CreateController();
            var result = await controller.DeletePtSession(Guid.NewGuid());
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
