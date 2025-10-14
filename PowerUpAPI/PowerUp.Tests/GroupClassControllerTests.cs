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
    public class GroupClassControllerTests
    {
        private readonly PowerUpDbContext _context;
        private readonly GroupClassController _controller;

        public GroupClassControllerTests()
        {
            var options = new DbContextOptionsBuilder<PowerUpDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new PowerUpDbContext(options);
            _controller = new GroupClassController(_context);
        }

        [Fact]
        public async Task GetGroupClasses_ShouldReturnAllClasses()
        {
            // Arrange
            var classes = new List<GroupClass>
            {
                new GroupClass { Id = Guid.NewGuid(), Name = "Yoga", Description = "Morning yoga class" },
                new GroupClass { Id = Guid.NewGuid(), Name = "Pilates", Description = "Evening pilates class" }
            };
            _context.GroupClasses.AddRange(classes);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetGroupClasses();

            // Assert
            var actionResult = Assert.IsType<ActionResult<IEnumerable<GroupClass>>>(result);
            var value = Assert.IsType<List<GroupClass>>(actionResult.Value);
            Assert.Equal(2, value.Count);
        }

        [Fact]
        public async Task GetGroupClass_ShouldReturnClass_WhenExists()
        {
            // Arrange
            var groupClass = new GroupClass
            {
                Id = Guid.NewGuid(),
                Name = "Spin Class",
                Description = "Indoor cycling"
            };
            _context.GroupClasses.Add(groupClass);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetGroupClass(groupClass.Id);

            // Assert
            var actionResult = Assert.IsType<ActionResult<GroupClass>>(result);
            var value = Assert.IsType<GroupClass>(actionResult.Value);
            Assert.Equal(groupClass.Name, value.Name);
        }

        [Fact]
        public async Task GetGroupClass_ShouldReturnNotFound_WhenNotExists()
        {
            // Act
            var result = await _controller.GetGroupClass(Guid.NewGuid());

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task CreateGroupClass_ShouldAddClass_AndReturnCreated()
        {
            // Arrange
            var groupClass = new GroupClass
            {
                Id = Guid.NewGuid(),
                Name = "Zumba",
                Description = "Dance fitness"
            };

            // Act
            var result = await _controller.CreateGroupClass(groupClass);

            // Assert
            var createdAtAction = Assert.IsType<CreatedAtActionResult>(result.Result);
            var createdClass = Assert.IsType<GroupClass>(createdAtAction.Value);
            Assert.Equal(groupClass.Name, createdClass.Name);

            var inDb = await _context.GroupClasses.FindAsync(groupClass.Id);
            Assert.NotNull(inDb);
        }

        [Fact]
        public async Task UpdateGroupClass_ShouldUpdateAndReturnNoContent()
        {
            // Arrange
            var groupClass = new GroupClass
            {
                Id = Guid.NewGuid(),
                Name = "CrossFit",
                Description = "Strength training"
            };
            _context.GroupClasses.Add(groupClass);
            await _context.SaveChangesAsync();

            groupClass.Name = "Updated CrossFit";

            // Act
            var result = await _controller.UpdateGroupClass(groupClass.Id, groupClass);

            // Assert
            Assert.IsType<NoContentResult>(result);

            var updated = await _context.GroupClasses.FindAsync(groupClass.Id);
            Assert.Equal("Updated CrossFit", updated.Name);
        }

        [Fact]
        public async Task UpdateGroupClass_ShouldReturnBadRequest_WhenIdsDoNotMatch()
        {
            // Arrange
            var groupClass = new GroupClass { Id = Guid.NewGuid(), Name = "Mismatch Class" };

            // Act
            var result = await _controller.UpdateGroupClass(Guid.NewGuid(), groupClass);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task UpdateGroupClass_ShouldReturnNotFound_WhenClassDoesNotExist()
        {
            // Arrange
            var groupClass = new GroupClass
            {
                Id = Guid.NewGuid(),
                Name = "Nonexistent",
                Description = "Does not exist"
            };

            // Act
            var result = await _controller.UpdateGroupClass(groupClass.Id, groupClass);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteGroupClass_ShouldSoftDeleteClass_AndReturnNoContent()
        {
            // Arrange
            var groupClass = new GroupClass
            {
                Id = Guid.NewGuid(),
                Name = "Bootcamp",
                Description = "Outdoor fitness"
            };
            _context.GroupClasses.Add(groupClass);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteGroupClass(groupClass.Id);

            // Assert
            Assert.IsType<NoContentResult>(result);

            var deleted = await _context.GroupClasses.FindAsync(groupClass.Id);
            Assert.True(deleted.IsDeleted);
            Assert.NotNull(deleted.DeletedAt);
        }

        [Fact]
        public async Task DeleteGroupClass_ShouldReturnNotFound_WhenNotExists()
        {
            // Act
            var result = await _controller.DeleteGroupClass(Guid.NewGuid());

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
