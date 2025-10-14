using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using PowerUp.Controllers;
using PowerUp.Data;
using PowerUp.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;

namespace PowerUp.Tests
{
    public class SubscriptionsControllerTests
    {
        private readonly PowerUpDbContext _context;

        public SubscriptionsControllerTests()
        {
            var options = new DbContextOptionsBuilder<PowerUpDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new PowerUpDbContext(options);
        }

        private SubscriptionsController CreateController(string role = "Admin", Guid? userId = null)
        {
            var controller = new SubscriptionsController(_context);

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
        public async Task GetSubscriptions_ShouldReturnAll()
        {
            var controller = CreateController();

            var subscriptions = new List<Subscription>
            {
                new Subscription { Id = Guid.NewGuid(), Type = SubscriptionType.Monthly, TotalPrice = 10 },
                new Subscription { Id = Guid.NewGuid(), Type = SubscriptionType.Yearly, TotalPrice = 100 }
            };
            _context.Subscriptions.AddRange(subscriptions);
            await _context.SaveChangesAsync();

            var result = await controller.GetSubscriptions();

            var actionResult = Assert.IsType<ActionResult<IEnumerable<Subscription>>>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<Subscription>>(actionResult.Value);
            Assert.Collection(list,
                s => Assert.Equal(SubscriptionType.Monthly, s.Type),
                s => Assert.Equal(SubscriptionType.Yearly, s.Type));
        }

        // --- GET BY ID ---
        [Fact]
        public async Task GetSubscription_ShouldReturnSubscription_WhenExists()
        {
            var controller = CreateController();
            var subscription = new Subscription { Id = Guid.NewGuid(), Type = SubscriptionType.Monthly, TotalPrice = 30 };
            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            var result = await controller.GetSubscription(subscription.Id);
            var actionResult = Assert.IsType<ActionResult<Subscription>>(result);
            Assert.Equal(subscription.Id, actionResult.Value.Id);
        }

        [Fact]
        public async Task GetSubscription_ShouldReturnNotFound_WhenMissing()
        {
            var controller = CreateController();

            var result = await controller.GetSubscription(Guid.NewGuid());
            Assert.IsType<NotFoundResult>(result.Result);
        }

        // --- CREATE ---
        [Fact]
        public async Task CreateSubscription_ShouldAddAndReturnCreated()
        {
            var controller = CreateController();
            var subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                Type = SubscriptionType.Semestral,
                TotalPrice = 50
            };

            var result = await controller.CreateSubscription(subscription);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var createdSub = Assert.IsType<Subscription>(created.Value);
            Assert.Equal(SubscriptionType.Semestral, createdSub.Type);
        }

        // --- UPDATE ---
        [Fact]
        public async Task UpdateSubscription_ShouldReturnNoContent_WhenValid()
        {
            var controller = CreateController();
            var subscription = new Subscription { Id = Guid.NewGuid(), Type = SubscriptionType.Monthly, TotalPrice = 10 };
            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            subscription.TotalPrice = 20;
            var result = await controller.UpdateSubscription(subscription.Id, subscription);

            Assert.IsType<NoContentResult>(result);
            var updated = await _context.Subscriptions.FindAsync(subscription.Id);
            Assert.Equal(20, updated.TotalPrice);
        }

        [Fact]
        public async Task UpdateSubscription_ShouldReturnBadRequest_WhenIdMismatch()
        {
            var controller = CreateController();
            var subscription = new Subscription { Id = Guid.NewGuid(), Type = SubscriptionType.Monthly, TotalPrice = 5 };

            var result = await controller.UpdateSubscription(Guid.NewGuid(), subscription);
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task UpdateSubscription_ShouldReturnNotFound_WhenNotExists()
        {
            var controller = CreateController();
            var subscription = new Subscription { Id = Guid.NewGuid(), Type = SubscriptionType.Monthly, TotalPrice = 5 };

            var result = await controller.UpdateSubscription(subscription.Id, subscription);
            Assert.IsType<NotFoundResult>(result);
        }

        // --- GET MY SUBSCRIPTION (USER ROLE) ---
        [Fact]
        public async Task GetMySubscription_ShouldReturnSubscription_WhenExists()
        {
            var userId = Guid.NewGuid();
            var controller = CreateController("User", userId);

            var userSub = new UserSubscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SubscriptionId = Guid.NewGuid()

            };

            _context.UserSubscriptions.Add(userSub);
            await _context.SaveChangesAsync();

            var result = await controller.GetMySubscription();
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<UserSubscription>(okResult.Value);
            Assert.Equal(userId, returned.UserId);
        }

        [Fact]
        public async Task GetMySubscription_ShouldReturnNotFound_WhenNoSubscription()
        {
            var controller = CreateController("User");

            var result = await controller.GetMySubscription();
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetMySubscription_ShouldFail_WhenRoleNotUser()
        {
            var controller = CreateController("Admin");
            var role = controller.User.FindFirst(ClaimTypes.Role)?.Value;
            Assert.NotEqual("User", role);
        }

        // --- DELETE ---
        [Fact]
        public async Task DeleteSubscription_ShouldSoftDelete_WhenExists()
        {
            var controller = CreateController();
            var subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                Type = SubscriptionType.Monthly,
                TotalPrice = 25
            };
            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            var result = await controller.DeleteSubscription(subscription.Id);
            Assert.IsType<NoContentResult>(result);

            var deleted = await _context.Subscriptions.FindAsync(subscription.Id);
            Assert.True(deleted.IsDeleted);
            Assert.NotNull(deleted.DeletedAt);
        }

        [Fact]
        public async Task DeleteSubscription_ShouldReturnNotFound_WhenMissing()
        {
            var controller = CreateController();
            var result = await controller.DeleteSubscription(Guid.NewGuid());
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
