using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Infrastructure.Persistence;
using MoneyTracker.Infrastructure.Services;
using Moq;

namespace MoneyTracker.Tests.Services
{
    public class UserProfileServiceTests
    {
        [Fact]
        public async Task GetCurrentProfileAsync_WhenUserExists_ReturnsProfileData()
        {
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var dbName = Guid.NewGuid().ToString();

            var services = BuildServices(tenantId, dbName);
            await SeedUserAsync(services, userId, tenantId, "Test User", "user@test.local", true);
            await SeedAvatarAsync(services, userId, tenantId, new byte[] { 1, 2, 3 });

            var currentUser = new Mock<ICurrentUserService>();
            currentUser.SetupGet(x => x.UserId).Returns(userId);
            currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);

            var tenantContext = new Mock<ITenantContext>();
            tenantContext.SetupGet(x => x.TenantId).Returns(tenantId);

            var service = new UserProfileService(
                services.GetRequiredService<IServiceScopeFactory>(),
                currentUser.Object,
                tenantContext.Object);

            var result = await service.GetCurrentProfileAsync();

            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.DisplayName.Should().Be("Test User");
            result.Data.Email.Should().Be("user@test.local");
            result.Data.TenantId.Should().Be(tenantId.ToString());
            result.Data.TwoFactorEnabled.Should().BeTrue();
            result.Data.AvatarDataUrl.Should().StartWith("data:image/png;base64,");
        }

        [Fact]
        public async Task UpdateDisplayNameAsync_WhenUserExists_UpdatesValue()
        {
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var dbName = Guid.NewGuid().ToString();

            var services = BuildServices(tenantId, dbName);
            await SeedUserAsync(services, userId, tenantId, "Old", "user@test.local", false);

            var currentUser = new Mock<ICurrentUserService>();
            currentUser.SetupGet(x => x.UserId).Returns(userId);

            var tenantContext = new Mock<ITenantContext>();
            tenantContext.SetupGet(x => x.TenantId).Returns(tenantId);

            var service = new UserProfileService(
                services.GetRequiredService<IServiceScopeFactory>(),
                currentUser.Object,
                tenantContext.Object);

            var result = await service.UpdateDisplayNameAsync("  New Name  ");

            result.Success.Should().BeTrue();

            await using var verificationScope = services.CreateAsyncScope();
            var db = verificationScope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
            var user = await db.Users.AsNoTracking().SingleAsync(x => x.Id == userId);
            user.DisplayName.Should().Be("New Name");
        }

        [Fact]
        public async Task RemoveAvatarAsync_WhenAvatarExists_RemovesRecord()
        {
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var dbName = Guid.NewGuid().ToString();

            var services = BuildServices(tenantId, dbName);
            await SeedUserAsync(services, userId, tenantId, "User", "user@test.local", false);
            await SeedAvatarAsync(services, userId, tenantId, new byte[] { 9, 9, 9 });

            var currentUser = new Mock<ICurrentUserService>();
            currentUser.SetupGet(x => x.UserId).Returns(userId);

            var tenantContext = new Mock<ITenantContext>();
            tenantContext.SetupGet(x => x.TenantId).Returns(tenantId);

            var service = new UserProfileService(
                services.GetRequiredService<IServiceScopeFactory>(),
                currentUser.Object,
                tenantContext.Object);

            var result = await service.RemoveAvatarAsync();

            result.Success.Should().BeTrue();

            await using var verificationScope = services.CreateAsyncScope();
            var db = verificationScope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
            var avatar = await db.UserAvatars.AsNoTracking().SingleOrDefaultAsync(a => a.UserId == userId);
            avatar.Should().BeNull();
        }

        [Fact]
        public async Task UpdateAvatarAsync_WhenUserAndTenantExist_CreatesAvatarAndReturnsDataUrl()
        {
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var dbName = Guid.NewGuid().ToString();

            var services = BuildServices(tenantId, dbName);
            await SeedUserAsync(services, userId, tenantId, "User", "user@test.local", false);

            var currentUser = new Mock<ICurrentUserService>();
            currentUser.SetupGet(x => x.UserId).Returns(userId);

            var tenantContext = new Mock<ITenantContext>();
            tenantContext.SetupGet(x => x.TenantId).Returns(tenantId);

            var service = new UserProfileService(
                services.GetRequiredService<IServiceScopeFactory>(),
                currentUser.Object,
                tenantContext.Object);

            var result = await service.UpdateAvatarAsync("image/png", new byte[] { 10, 20, 30 });

            result.Success.Should().BeTrue();
            result.Data.Should().StartWith("data:image/png;base64,");

            await using var verificationScope = services.CreateAsyncScope();
            var db = verificationScope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();
            var avatar = await db.UserAvatars.AsNoTracking().SingleOrDefaultAsync(a => a.UserId == userId);
            avatar.Should().NotBeNull();
            avatar!.ContentType.Should().Be("image/png");
            avatar.Content.Should().Equal(new byte[] { 10, 20, 30 });
        }

        [Fact]
        public async Task UpdateAvatarAsync_WhenUnauthorized_ReturnsFailResult()
        {
            var tenantId = Guid.NewGuid();
            var dbName = Guid.NewGuid().ToString();
            var services = BuildServices(tenantId, dbName);

            var currentUser = new Mock<ICurrentUserService>();
            currentUser.SetupGet(x => x.UserId).Returns((Guid?)null);

            var tenantContext = new Mock<ITenantContext>();
            tenantContext.SetupGet(x => x.TenantId).Returns(tenantId);

            var service = new UserProfileService(
                services.GetRequiredService<IServiceScopeFactory>(),
                currentUser.Object,
                tenantContext.Object);

            var result = await service.UpdateAvatarAsync("image/png", new byte[] { 1 });

            result.Success.Should().BeFalse();
            result.Message.Should().Be("Unauthorized.");
        }

        private static ServiceProvider BuildServices(Guid tenantId, string dbName)
        {
            var tenantContext = new Mock<ITenantContext>();
            tenantContext.SetupGet(x => x.TenantId).Returns(tenantId);

            var services = new ServiceCollection();
            services.AddSingleton(tenantContext.Object);
            services.AddDbContext<MoneyTrackerDbContext>(options =>
                options.UseInMemoryDatabase(dbName));

            return services.BuildServiceProvider();
        }

        private static async Task SeedUserAsync(
            IServiceProvider services,
            Guid userId,
            Guid tenantId,
            string displayName,
            string email,
            bool twoFactorEnabled)
        {
            await using var scope = services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

            db.Users.Add(new ApplicationUser
            {
                Id = userId,
                TenantId = tenantId,
                DisplayName = displayName,
                Email = email,
                UserName = email,
                NormalizedEmail = email.ToUpperInvariant(),
                NormalizedUserName = email.ToUpperInvariant(),
                TwoFactorEnabled = twoFactorEnabled,
                SecurityStamp = Guid.NewGuid().ToString()
            });

            await db.SaveChangesAsync();
        }

        private static async Task SeedAvatarAsync(
            IServiceProvider services,
            Guid userId,
            Guid tenantId,
            byte[] content)
        {
            await using var scope = services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

            db.UserAvatars.Add(new UserAvatar
            {
                UserId = userId,
                TenantId = tenantId,
                ContentType = "image/png",
                Content = content,
                SizeBytes = content.Length
            });

            await db.SaveChangesAsync();
        }
    }
}
