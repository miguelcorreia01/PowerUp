using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerUp.Controllers;
using PowerUp.Data;
using PowerUp.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PowerUp.Tests
{
    public class PaymentControllerTests
    {
        private readonly PowerUpDbContext _context;

        public PaymentControllerTests()
        {
            var options = new DbContextOptionsBuilder<PowerUpDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new PowerUpDbContext(options);
        }

        private PaymentController CreateController() => new PaymentController(_context);

        private UserSubscription CreateUserSubscription()
        {
            var userSub = new UserSubscription
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                SubscriptionId = Guid.NewGuid(),
                IsDeleted = false
            };

            _context.UserSubscriptions.Add(userSub);
            _context.SaveChanges();
            return userSub;
        }

        // --- GET ALL PAYMENTS ---
        [Fact]
        public async Task GetPayments_ShouldReturnAllPayments()
        {
            var controller = CreateController();
            var userSub = CreateUserSubscription();

            var payments = new List<Payment>
            {
                new Payment { Id = Guid.NewGuid(), Amount = 50, PaymentDate = DateTime.UtcNow, UserSubscriptionId = userSub.Id, UserSubscription = userSub },
                new Payment { Id = Guid.NewGuid(), Amount = 75, PaymentDate = DateTime.UtcNow, UserSubscriptionId = userSub.Id, UserSubscription = userSub }
            };

            _context.Payments.AddRange(payments);
            await _context.SaveChangesAsync();

            var result = await controller.GetPayments();
            var actionResult = Assert.IsType<ActionResult<IEnumerable<Payment>>>(result);
            var value = Assert.IsAssignableFrom<IEnumerable<Payment>>(actionResult.Value);
            Assert.Collection(value,
                p => Assert.Equal(50, p.Amount),
                p => Assert.Equal(75, p.Amount));
        }

        // --- GET PAYMENT BY ID ---
        [Fact]
        public async Task GetPayment_ShouldReturnPayment_WhenExists()
        {
            var controller = CreateController();
            var userSub = CreateUserSubscription();
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                Amount = 100,
                PaymentDate = DateTime.UtcNow,
                UserSubscriptionId = userSub.Id,
                UserSubscription = userSub
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            var result = await controller.GetPayment(payment.Id);
            var actionResult = Assert.IsType<ActionResult<Payment>>(result);
            Assert.Equal(payment.Id, actionResult.Value.Id);
        }

        [Fact]
        public async Task GetPayment_ShouldReturnNotFound_WhenNotExists()
        {
            var controller = CreateController();

            var result = await controller.GetPayment(Guid.NewGuid());
            Assert.IsType<NotFoundResult>(result.Result);
        }

        // --- CREATE PAYMENT ---
        [Fact]
        public async Task CreatePayment_ShouldReturnCreatedPayment()
        {
            var controller = CreateController();
            var userSub = CreateUserSubscription();

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                Amount = 120,
                PaymentDate = DateTime.UtcNow,
                UserSubscriptionId = userSub.Id,
                UserSubscription = userSub
            };

            var result = await controller.CreatePayment(payment);
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var createdPayment = Assert.IsType<Payment>(created.Value);
            Assert.Equal(120, createdPayment.Amount);
        }

        [Fact]
        public async Task CreatePayment_ShouldPersistToDatabase()
        {
            var controller = CreateController();
            var userSub = CreateUserSubscription();

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                Amount = 200,
                PaymentDate = DateTime.UtcNow,
                UserSubscriptionId = userSub.Id,
                UserSubscription = userSub
            };

            await controller.CreatePayment(payment);
            var saved = await _context.Payments.FindAsync(payment.Id);
            Assert.NotNull(saved);
            Assert.Equal(200, saved.Amount);
        }

        // --- UPDATE PAYMENT ---
        [Fact]
        public async Task UpdatePayment_ShouldReturnNoContent_WhenValid()
        {
            var controller = CreateController();
            var userSub = CreateUserSubscription();

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                Amount = 90,
                PaymentDate = DateTime.UtcNow,
                UserSubscriptionId = userSub.Id,
                UserSubscription = userSub
            };
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            payment.Amount = 150;

            var result = await controller.UpdatePayment(payment.Id, payment);
            Assert.IsType<NoContentResult>(result);

            var updated = await _context.Payments.FindAsync(payment.Id);
            Assert.Equal(150, updated.Amount);
        }

        [Fact]
        public async Task UpdatePayment_ShouldReturnBadRequest_WhenIdMismatch()
        {
            var controller = CreateController();
            var userSub = CreateUserSubscription();

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                Amount = 50,
                PaymentDate = DateTime.UtcNow,
                UserSubscriptionId = userSub.Id,
                UserSubscription = userSub
            };

            var result = await controller.UpdatePayment(Guid.NewGuid(), payment);
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task UpdatePayment_ShouldReturnNotFound_WhenMissing()
        {
            var controller = CreateController();
            var userSub = CreateUserSubscription();

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                Amount = 70,
                PaymentDate = DateTime.UtcNow,
                UserSubscriptionId = userSub.Id,
                UserSubscription = userSub
            };

            var result = await controller.UpdatePayment(payment.Id, payment);
            Assert.IsType<NotFoundResult>(result);
        }

        // --- DELETE PAYMENT ---
        [Fact]
        public async Task DeletePayment_ShouldSoftDelete_WhenExists()
        {
            var controller = CreateController();
            var userSub = CreateUserSubscription();

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                Amount = 99,
                PaymentDate = DateTime.UtcNow,
                UserSubscriptionId = userSub.Id,
                UserSubscription = userSub
            };
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            var result = await controller.DeletePayment(payment.Id);
            Assert.IsType<NoContentResult>(result);

            var deleted = await _context.Payments.FindAsync(payment.Id);
            Assert.True(deleted.IsDeleted);
            Assert.NotNull(deleted.DeletedAt);
        }

        [Fact]
        public async Task DeletePayment_ShouldReturnNotFound_WhenMissing()
        {
            var controller = CreateController();
            var result = await controller.DeletePayment(Guid.NewGuid());
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
