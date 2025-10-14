using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerUp.Controllers;
using PowerUp.Data;
using PowerUp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PowerUp.Tests
{
    public class UserSubscriptionControllerTests
    {
        private readonly PowerUpDbContext _context;

        public UserSubscriptionControllerTests()
        {
            var options = new DbContextOptionsBuilder<PowerUpDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new PowerUpDbContext(options);
        }

        private UserSubscriptionController CreateController()
        {
            return new UserSubscriptionController(_context);
        }

        // --- GET ALL ---
        [Fact]
        public async Task GetUserSubscriptions_ShouldReturnAllRecords()
        {
            var subs = new List<UserSubscription>
            {
                new UserSubscription { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), SubscriptionId = Guid.NewGuid() },
                new UserSubscription { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), SubscriptionId = Guid.NewGuid() }
            };
            _context.UserSubscriptions.AddRange(subs);
            await _context.SaveChangesAsync();

            var controller = CreateController();
            var result = await controller.GetUserSubscriptions();

            var actionResult = Assert.IsType<ActionResult<IEnumerable<UserSubscription>>>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<UserSubscription>>(actionResult.Value);
            Assert.Equal(2, list.Count());
        }

        // --- GET BY ID ---
        [Fact]
        public async Task GetUserSubscription_ShouldReturnRecord_WhenExists()
        {
            var sub = new UserSubscription { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), SubscriptionId = Guid.NewGuid() };
            _context.UserSubscriptions.Add(sub);
            await _context.SaveChangesAsync();

            var controller = CreateController();
            var result = await controller.GetUserSubscription(sub.Id);

            var actionResult = Assert.IsType<ActionResult<UserSubscription>>(result);
            Assert.Equal(sub.Id, actionResult.Value.Id);
        }

        [Fact]
        public async Task GetUserSubscription_ShouldReturnNotFound_WhenMissing()
        {
            var controller = CreateController();
            var result = await controller.GetUserSubscription(Guid.NewGuid());
            Assert.IsType<NotFoundResult>(result.Result);
        }

        // --- CREATE ---
        [Fact]
        public async Task CreateUserSubscription_ShouldReturnCreatedRecord()
        {
            var controller = CreateController();
            var newSub = new UserSubscription
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                SubscriptionId = Guid.NewGuid()
            };

            var result = await controller.CreateUserSubscription(newSub);
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);

            var returned = Assert.IsType<UserSubscription>(created.Value);
            Assert.Equal(newSub.Id, returned.Id);

            var saved = await _context.UserSubscriptions.FindAsync(newSub.Id);
            Assert.NotNull(saved);
        }

        // --- UPDATE ---
        [Fact]
        public async Task UpdateUserSubscription_ShouldReturnNoContent_WhenValid()
        {
            var sub = new UserSubscription { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), SubscriptionId = Guid.NewGuid() };
            _context.UserSubscriptions.Add(sub);
            await _context.SaveChangesAsync();

            var controller = CreateController();
            sub.SubscriptionId = Guid.NewGuid(); // simulate change

            var result = await controller.UpdateUserSubscription(sub.Id, sub);
            Assert.IsType<NoContentResult>(result);

            var updated = await _context.UserSubscriptions.FindAsync(sub.Id);
            Assert.Equal(sub.SubscriptionId, updated.SubscriptionId);
        }

        [Fact]
        public async Task UpdateUserSubscription_ShouldReturnBadRequest_WhenIdMismatch()
        {
            var sub = new UserSubscription { Id = Guid.NewGuid() };
            var controller = CreateController();

            var result = await controller.UpdateUserSubscription(Guid.NewGuid(), sub);
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task UpdateUserSubscription_ShouldReturnNotFound_WhenRecordDeletedBeforeUpdate()
        {
            var sub = new UserSubscription { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), SubscriptionId = Guid.NewGuid() };
            _context.UserSubscriptions.Add(sub);
            await _context.SaveChangesAsync();

            var controller = CreateController();

            _context.UserSubscriptions.Remove(sub);
            await _context.SaveChangesAsync();

            var result = await controller.UpdateUserSubscription(sub.Id, sub);
            Assert.IsType<NotFoundResult>(result);
        }

        // --- DELETE ---
        [Fact]
        public async Task DeleteUserSubscription_ShouldSoftDelete_WhenExists()
        {
            var sub = new UserSubscription { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), SubscriptionId = Guid.NewGuid() };
            _context.UserSubscriptions.Add(sub);
            await _context.SaveChangesAsync();

            var controller = CreateController();
            var result = await controller.DeleteUserSubscription(sub.Id);

            Assert.IsType<NoContentResult>(result);
            var deleted = await _context.UserSubscriptions.FindAsync(sub.Id);
            Assert.True(deleted.IsDeleted);
            Assert.NotNull(deleted.DeletedAt);
        }

        [Fact]
        public async Task DeleteUserSubscription_ShouldReturnNotFound_WhenMissing()
        {
            var controller = CreateController();
            var result = await controller.DeleteUserSubscription(Guid.NewGuid());
            Assert.IsType<NotFoundResult>(result);
        }

        // --- EXISTS HELPER ---
        [Fact]
        public void UserSubscriptionExists_ShouldReturnTrue_WhenExists()
        {
            var sub = new UserSubscription { Id = Guid.NewGuid() };
            _context.UserSubscriptions.Add(sub);
            _context.SaveChanges();

            var controller = CreateController();
            var exists = controller
                .GetType()
                .GetMethod("UserSubscriptionExists", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(controller, new object[] { sub.Id });

            Assert.True((bool)exists!);
        }
    }
}
