using FluentAssertions;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs;
using MoneyTracker.Application.DTOs.Transactions;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Application.Services;
using MoneyTracker.Domain.Const;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.Account;
using MoneyTracker.Domain.Interfaces;
using Moq;

namespace MoneyTracker.Tests.Services
{
    public class AccountServiceTests
    {
        private readonly Mock<IAccountRepository> _accountRepo = new();
        private readonly Mock<ITransactionRepository> _transactionRepo = new();
        private readonly Mock<ITransactionService> _transactionService = new();
        private readonly Mock<ITimeZoneService> _timeZoneService = new();
        private readonly Mock<ISystemCategoryResolver> _systemCategoryResolver = new();

        private AccountService CreateService()
            => new AccountService(
                _accountRepo.Object,
                _transactionRepo.Object,
                _transactionService.Object,
                _timeZoneService.Object,
                _systemCategoryResolver.Object);

        [Fact]
        public async Task GetAllAsync_ReturnsAccountsOrderedByType()
        {
            _accountRepo.Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<Account>
                {
                    new() { Id = 2, Name = "Credit", Type = AccountType.Credit, Icon = "CreditCard" },
                    new() { Id = 1, Name = "Cash", Type = AccountType.Cash, Icon = "Wallet" }
                });

            var service = CreateService();

            var result = await service.GetAllAsync();

            result.Success.Should().BeTrue();
            result.Message.Should().Be(OperationMessages.DataRetrieved);
            result.Data.Should().NotBeNull();
            result.Data!.Select(x => x.Type).Should().ContainInOrder(AccountType.Cash, AccountType.Credit);
        }

        [Fact]
        public async Task GetByIdAsync_WhenNotFound_ReturnsFailResult()
        {
            _accountRepo.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Account?)null);
            var service = CreateService();

            var result = await service.GetByIdAsync(999);

            result.Success.Should().BeFalse();
            result.Message.Should().Be(OperationMessages.NotFound);
        }

        [Fact]
        public async Task UpdateAsync_WhenNotFound_ReturnsFailResult()
        {
            _accountRepo.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Account?)null);
            var service = CreateService();

            var result = await service.UpdateAsync(new AccountDto { Id = 1, Name = "Main", Icon = AccountIcon.Wallet });

            result.Success.Should().BeFalse();
            result.Message.Should().Be(OperationMessages.NotFound);
        }

        [Fact]
        public async Task UpdateAsync_WhenRepositoryUpdateFails_ReturnsFailResult()
        {
            _accountRepo.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(new Account { Id = 1, Name = "Old", Icon = "Wallet", Type = AccountType.Cash });
            _accountRepo.Setup(x => x.UpdateAsync(It.IsAny<Account>())).ReturnsAsync(false);
            var service = CreateService();

            var result = await service.UpdateAsync(new AccountDto
            {
                Id = 1,
                Name = "Updated",
                Icon = AccountIcon.Payments,
                Type = AccountType.Bank
            });

            result.Success.Should().BeFalse();
            result.Message.Should().Be("Error updating account");
        }

        [Fact]
        public async Task DeleteAsync_WhenSuccess_ReturnsDeletedMessage()
        {
            _accountRepo.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(new Account { Id = 1, Name = "Main", Icon = "Wallet", Type = AccountType.Cash });
            _transactionRepo.Setup(x => x.SoftDeleteByAccountAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<int>());
            _accountRepo.Setup(x => x.DeleteAsync(1)).ReturnsAsync(true);
            var service = CreateService();

            var result = await service.DeleteAsync(1);

            result.Success.Should().BeTrue();
            result.Message.Should().Be(OperationMessages.Deleted);
            _transactionRepo.Verify(x => x.SoftDeleteByAccountAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenRelatedAccountNeedsSync_UpdatesBalanceBeforeDeleting()
        {
            _accountRepo.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(new Account { Id = 1, Name = "Main", Icon = "Wallet", Type = AccountType.Cash });
            _transactionRepo.Setup(x => x.SoftDeleteByAccountAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<int> { 1, 2 });

            var related = new Account { Id = 2, Name = "Savings", Icon = "Savings", Type = AccountType.Savings, Balance = 999m };
            _accountRepo.Setup(x => x.GetByIdAsync(2)).ReturnsAsync(related);
            _transactionService.Setup(x => x.GetAccountBalanceAsync(2))
                .ReturnsAsync(OperationResult<decimal>.Ok(120m));
            _accountRepo.Setup(x => x.UpdateAsync(It.IsAny<Account>())).ReturnsAsync(true);
            _accountRepo.Setup(x => x.DeleteAsync(1)).ReturnsAsync(true);

            var service = CreateService();

            var result = await service.DeleteAsync(1);

            result.Success.Should().BeTrue();
            related.Balance.Should().Be(120m);
            _accountRepo.Verify(x => x.UpdateAsync(It.Is<Account>(a => a.Id == 2 && a.Balance == 120m)), Times.Once);
            _accountRepo.Verify(x => x.DeleteAsync(1), Times.Once);
        }

        [Fact]
        public async Task CreateWithInitialBalanceAsync_WhenDuplicateName_ReturnsFailResult()
        {
            _accountRepo.Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<Account> { new() { Name = "Main", Icon = "Wallet", Type = AccountType.Cash } });
            var service = CreateService();

            var result = await service.CreateWithInitialBalanceAsync(
                new AccountDto { Name = "Main", Icon = AccountIcon.Wallet, Type = AccountType.Cash }, 100m);

            result.Success.Should().BeFalse();
            result.Message.Should().Be(OperationMessages.DuplicateName);
            _accountRepo.Verify(x => x.AddAsync(It.IsAny<Account>()), Times.Never);
        }

        [Fact]
        public async Task CreateWithInitialBalanceAsync_WithInitialBalance_CreatesInitialTransaction()
        {
            // Fix local time to keep transaction timestamps deterministic.
            var now = new DateTime(2026, 3, 5, 10, 0, 0);
            _timeZoneService.Setup(x => x.GetLocalTimeInConfiguredTimeZone()).Returns(now);
            _accountRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Account>());
            _accountRepo.Setup(x => x.AddAsync(It.IsAny<Account>()))
                .ReturnsAsync(true)
                .Callback<Account>(a => a.Id = 77);
            _systemCategoryResolver.Setup(x => x.GetInitialBalanceCategoryIdAsync(true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(1001);
            _transactionService.Setup(x => x.CreateAsync(It.IsAny<TransactionDto>()))
                .ReturnsAsync(OperationResult<bool>.Ok(true));

            var service = CreateService();
            var dto = new AccountDto { Name = "Wallet", Icon = AccountIcon.Wallet, Type = AccountType.Cash };

            var result = await service.CreateWithInitialBalanceAsync(dto, 250m);

            result.Success.Should().BeTrue();
            result.Message.Should().Be(OperationMessages.Created);
            // Verify that an initial-balance system transaction is generated with the new account Id.
            _transactionService.Verify(x => x.CreateAsync(It.Is<TransactionDto>(t =>
                t.AccountId == 77 &&
                t.CategoryId == 1001 &&
                t.Amount == 250m &&
                t.Date == now &&
                t.Description == $"{SystemCategoryNames.INITIAL_BALANCE_NAME} - {dto.Name}")), Times.Once);
        }

        [Fact]
        public async Task AdjustBalanceAsync_WhenNoChange_ReturnsNoChangesMessage()
        {
            _accountRepo.Setup(x => x.GetByIdAsync(3))
                .ReturnsAsync(new Account { Id = 3, Name = "Bank", Balance = 300m, Icon = "Wallet", Type = AccountType.Bank });
            var service = CreateService();

            var result = await service.AdjustBalanceAsync(3, 300m);

            result.Success.Should().BeTrue();
            result.Message.Should().Be("No changes in balance");
            _transactionService.Verify(x => x.CreateAsync(It.IsAny<TransactionDto>()), Times.Never);
        }

        [Fact]
        public async Task AdjustBalanceAsync_WhenPositiveAdjustment_CreatesAdjustmentTransaction()
        {
            // Account goes from 100 to 250, so adjustment must be +150.
            var now = new DateTime(2026, 3, 5, 11, 0, 0);
            _timeZoneService.Setup(x => x.GetLocalTimeInConfiguredTimeZone()).Returns(now);
            _accountRepo.Setup(x => x.GetByIdAsync(4))
                .ReturnsAsync(new Account { Id = 4, Name = "Bank", Balance = 100m, Icon = "Wallet", Type = AccountType.Bank });
            _systemCategoryResolver.Setup(x => x.GetBalanceAdjustmentCategoryIdAsync(true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(2001);
            _transactionService.Setup(x => x.CreateAsync(It.IsAny<TransactionDto>()))
                .ReturnsAsync(OperationResult<bool>.Ok(true));
            var service = CreateService();

            var result = await service.AdjustBalanceAsync(4, 250m, "manual fix");

            result.Success.Should().BeTrue();
            result.Message.Should().StartWith("Balance adjusted by +");
            _transactionService.Verify(x => x.CreateAsync(It.Is<TransactionDto>(t =>
                t.AccountId == 4 &&
                t.CategoryId == 2001 &&
                t.Amount == 150m &&
                t.Date == now &&
                t.Description == $"{SystemCategoryNames.BALANCE_ADJUSTMENT_NAME} - Bank")), Times.Once);
        }

        [Fact]
        public async Task SyncBalanceAsync_WhenBalanceCalculationFails_ReturnsFailResult()
        {
            _accountRepo.Setup(x => x.GetByIdAsync(8))
                .ReturnsAsync(new Account { Id = 8, Name = "Savings", Icon = "Savings", Type = AccountType.Savings });
            _transactionService.Setup(x => x.GetAccountBalanceAsync(8))
                .ReturnsAsync(OperationResult<decimal>.Fail("calc failed"));
            var service = CreateService();

            var result = await service.SyncBalanceAsync(8);

            result.Success.Should().BeFalse();
            result.Message.Should().Be("calc failed");
            _accountRepo.Verify(x => x.UpdateAsync(It.IsAny<Account>()), Times.Never);
        }

        [Fact]
        public async Task SyncBalanceAsync_WhenSuccess_UpdatesAccountBalance()
        {
            // Sync should overwrite persisted balance with the calculated transaction balance.
            var account = new Account { Id = 8, Name = "Savings", Balance = 0m, Icon = "Savings", Type = AccountType.Savings };
            _accountRepo.Setup(x => x.GetByIdAsync(8)).ReturnsAsync(account);
            _transactionService.Setup(x => x.GetAccountBalanceAsync(8))
                .ReturnsAsync(OperationResult<decimal>.Ok(123.45m));
            _accountRepo.Setup(x => x.UpdateAsync(It.IsAny<Account>())).ReturnsAsync(true);
            var service = CreateService();

            var result = await service.SyncBalanceAsync(8);

            result.Success.Should().BeTrue();
            result.Message.Should().Be("Account balance synced successfully");
            account.Balance.Should().Be(123.45m);
            _accountRepo.Verify(x => x.UpdateAsync(It.Is<Account>(a => a.Id == 8 && a.Balance == 123.45m)), Times.Once);
        }

        [Fact]
        public async Task HasAccountByType_WhenAccountsExist_ReturnsTrueAndFoundMessage()
        {
            _accountRepo.Setup(x => x.HasAccountsByTypeAsync(AccountType.Credit)).ReturnsAsync(true);
            var service = CreateService();

            var result = await service.HasAccountByType(AccountType.Credit);

            result.Success.Should().BeTrue();
            result.Data.Should().BeTrue();
            result.Message.Should().Be("Accounts found");
        }
    }
}
