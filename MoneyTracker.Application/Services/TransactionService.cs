using FluentValidation;
using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.Common.Extensions;
using MoneyTracker.Application.Constants;
using MoneyTracker.Application.DTOs;
using MoneyTracker.Application.DTOs.Transactions;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Application.Mappers;
using MoneyTracker.Application.Mappers.Transactions;
using MoneyTracker.Domain.Const;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.Category;
using MoneyTracker.Domain.Enums.Transaction;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Application.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _repository;
        private readonly IAccountRepository _accountRepository;
        private readonly IValidator<TransactionDto> _validator;
        private readonly IValidator<CreateTransferDto> _transferValidator;
        private readonly ILogger<TransactionService> _logger;
        private readonly ITimeZoneService _timeZoneService;
        private readonly ITimeRangeService _timeRangeService;
        private readonly ICategoryService _categoryService;

        public TransactionService(
            ITransactionRepository repository,
            IAccountRepository accountRepository,
            IValidator<TransactionDto> validator,
            IValidator<CreateTransferDto> transferValidator,
            ILogger<TransactionService> logger,
            ITimeZoneService timeZoneService,
            ITimeRangeService timeRangeService,
            ICategoryRepository categoryRepository,
            ICategoryService categoryService)
        {
            _repository = repository;
            _accountRepository = accountRepository;
            _validator = validator;
            _transferValidator = transferValidator;
            _logger = logger;
            _timeZoneService = timeZoneService;
            _timeRangeService = timeRangeService;
            _categoryService = categoryService;
        }

        public async Task<OperationResult<List<TransactionDto>>> GetAllAsync()
        {
            try
            {
                var expenses = await _repository.GetAllAsync();
                var dtoList = expenses.Select(x => x.MapToDto(_timeZoneService)).ToList();
                return OperationResult<List<TransactionDto>>.Ok(dtoList, OperationMessages.DataRetrieved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<List<TransactionDto>>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<TransactionDto>> GetByIdAsync(int id)
        {
            try
            {
                var expense = await _repository.GetByIdAsync(id);
                if (expense == null)
                    return OperationResult<TransactionDto>.Fail(OperationMessages.NotFound);

                return OperationResult<TransactionDto>.Ok(expense.MapToDto(_timeZoneService), OperationMessages.DataRetrieved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<TransactionDto>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<TransactionWithPairDto>> GetByIdWithPairAsync(int id)
        {
            try
            {
                var transaction = await _repository.GetByIdAsync(id);
                if (transaction == null)
                    return OperationResult<TransactionWithPairDto>.Fail(OperationMessages.NotFound);

                // Cargar pareja si existe
                Transaction? paired = null;
                if (transaction.TransferPairId.HasValue)
                {
                    paired = await _repository.GetByIdAsync(transaction.TransferPairId.Value);
                }

                // Usar mapper
                var result = transaction.MapToTransactionWithPair(paired, _timeZoneService);

                return OperationResult<TransactionWithPairDto>.Ok(result, OperationMessages.DataRetrieved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<TransactionWithPairDto>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<bool>> CreateAsync(TransactionDto dto)
        {
            var validation = await _validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return OperationResult<bool>.Fail(string.Join(", ", validation.Errors.Select(e => e.ErrorMessage)));

            try
            {
                if (dto.Category is null)
                {
                    var categoryResult = await _categoryService.GetByIdAsync(dto.CategoryId);
                    if (categoryResult.Success)
                    {
                        dto.Category = categoryResult.Data;
                    }
                }

                var entity = dto.MapToEntity(_timeZoneService);
                entity.CreatedAt = _timeZoneService.GetNowInUtc();

                await _repository.AddAsync(entity);
                return OperationResult<bool>.Ok(true, OperationMessages.Created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<bool>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<bool>> CreateTransferAsync(CreateTransferDto dto)
        {
            var validation = await _transferValidator.ValidateAsync(dto);
            if (!validation.IsValid)
                return OperationResult<bool>.Fail(string.Join(", ", validation.Errors.Select(e => e.ErrorMessage)));

            try
            {
                var fromAccount = await _accountRepository.GetByIdAsync(dto.FromAccountId);
                if (fromAccount == null)
                    return OperationResult<bool>.Fail(OperationMessages.TransferSourceNotFound);

                var toAccount = await _accountRepository.GetByIdAsync(dto.ToAccountId);
                if (toAccount == null)
                    return OperationResult<bool>.Fail(OperationMessages.TransferDestinationNotFound);

                var (fromCategoryId, toCategoryId) = SystemCategories.GetTransferCategoriesByAccountType(
                    fromAccount.Type,
                    toAccount.Type);

                var fromAccountDto = fromAccount.MapToDto();
                var toAccountDto = toAccount.MapToDto();

                var (fromTransaction, toTransaction) = dto.MapToTransferPair(
                    fromCategoryId,
                    toCategoryId,
                    fromAccountDto.Name,
                    toAccountDto.Name,
                    _timeZoneService);

                // Crear primera transacción
                int fromId = await _repository.AddAndReturnIdAsync(fromTransaction);

                // Vincular y crear segunda transacción
                toTransaction.TransferPairId = fromId;
                int toId = await _repository.AddAndReturnIdAsync(toTransaction);

                // Actualizar primera con el par
                fromTransaction.TransferPairId = toId;
                await _repository.UpdateAsync(fromTransaction);

                return OperationResult<bool>.Ok(true, OperationMessages.TransferCreated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<bool>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<bool>> UpdateAsync(TransactionDto dto)
        {
            var validation = await _validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return OperationResult<bool>.Fail(string.Join(", ", validation.Errors.Select(e => e.ErrorMessage)));

            try
            {
                var existing = await _repository.GetByIdAsync(dto.Id);
                if (existing is null)
                    return OperationResult<bool>.Fail(OperationMessages.NotFound);

                TransactionMapper.UpdateEntity(existing, dto, _timeZoneService);
                await _repository.UpdateAsync(existing);
                return OperationResult<bool>.Ok(true, OperationMessages.Updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<bool>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<bool>> UpdateTransferAsync(UpdateTransferDto dto)
        {
            // Validación básica
            if (dto.Amount <= 0)
                return OperationResult<bool>.Fail(ValidationMessages.GreaterThanZero);

            try
            {
                var transaction = await _repository.GetByIdAsync(dto.TransactionId);
                if (transaction == null)
                    return OperationResult<bool>.Fail(OperationMessages.NotFound);

                // Verificar que sea un transfer
                if (!transaction.TransferPairId.HasValue)
                    return OperationResult<bool>.Fail(OperationMessages.TransferNotValid);

                // Obtener pareja
                var paired = await _repository.GetByIdAsync(transaction.TransferPairId.Value);
                if (paired == null)
                    return OperationResult<bool>.Fail(OperationMessages.TransferPairNotFound);

                // Usar mapper para actualizar ambas
                TransferMapper.UpdateTransferPair(transaction, paired, dto, _timeZoneService);

                await _repository.UpdateAsync(transaction);
                await _repository.UpdateAsync(paired);

                _logger.LogInformation(
                    "Transfer updated: Transaction {Id1} and paired {Id2}, Amount: {Amount}",
                    transaction.Id, paired.Id, dto.Amount);

                return OperationResult<bool>.Ok(true, OperationMessages.Updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<bool>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<bool>> DeleteAsync(int id)
        {
            try
            {
                var existing = await _repository.GetByIdAsync(id);
                if (existing is null)
                    return OperationResult<bool>.Fail(OperationMessages.NotFound);

                await _repository.DeleteAsync(id);
                return OperationResult<bool>.Ok(true, OperationMessages.Deleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<bool>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<bool>> DeleteTransferAsync(int transactionId)
        {
            try
            {
                var transaction = await _repository.GetByIdAsync(transactionId);
                if (transaction == null)
                    return OperationResult<bool>.Fail(OperationMessages.NotFound);

                // Verificar que sea un transfer
                if (!transaction.TransferPairId.HasValue)
                    return OperationResult<bool>.Fail(OperationMessages.TransferNotValid);

                var paired = await _repository.GetByIdAsync(transaction.TransferPairId.Value);
                if (paired == null)
                    return OperationResult<bool>.Fail(OperationMessages.TransferPairNotFound);

                // Eliminar ambas transacciones
                await _repository.DeleteAsync(transaction.Id);
                await _repository.DeleteAsync(paired.Id);

                _logger.LogInformation(
                    "Transfer deleted: Transaction {Id1} and paired {Id2}",
                    transaction.Id, paired.Id);

                return OperationResult<bool>.Ok(true, OperationMessages.Deleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<bool>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<int>> DuplicateTransactionAsync(int transactionId)
        {
            try
            {
                var original = await _repository.GetByIdAsync(transactionId);
                if (original == null)
                    return OperationResult<int>.Fail(OperationMessages.NotFound);

                // Validar: NO duplicar credit payments
                if (SystemCategories.IsCreditRelatedCategory(original.CategoryId))
                {
                    return OperationResult<int>.Fail(OperationMessages.CreditPaymentCannotDuplicate);
                }

                // Si es transfer, duplicar usando mapper
                if (original.TransferPairId.HasValue)
                {
                    var paired = await _repository.GetByIdAsync(original.TransferPairId.Value);
                    if (paired == null)
                        return OperationResult<int>.Fail(OperationMessages.TransferPairNotFound);

                    // Determinar FROM y TO
                    var isOutgoing = original.Category?.Type == CategoryTypeEnum.Expense;
                    var fromTransaction = isOutgoing ? original : paired;
                    var toTransaction = isOutgoing ? paired : original;

                    // Usar mapper para crear DTO
                    var transferDto = TransferMapper.MapToDuplicateTransferDto(
                        fromTransaction,
                        toTransaction,
                        _timeZoneService);

                    var result = await CreateTransferAsync(transferDto);

                    if (result.Success)
                    {
                        _logger.LogInformation(
                            "Transfer duplicated: Original {OriginalId} (paired {PairedId})",
                            original.Id, paired.Id);
                    }

                    return result.Success
                        ? OperationResult<int>.Ok(0, OperationMessages.TransferDuplicated)
                        : OperationResult<int>.Fail(result.Message ?? OperationMessages.UnexpectedError);
                }

                // Duplicar transacción regular usando mapper
                var duplicate = original.MapToDuplicate(_timeZoneService);
                int newId = await _repository.AddAndReturnIdAsync(duplicate);

                _logger.LogInformation(
                    "Transaction duplicated: Original {OriginalId} -> New {NewId}",
                    transactionId, newId);

                return OperationResult<int>.Ok(newId, OperationMessages.TransactionDuplicated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<int>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<CategoryStatsDto>> GetCategoryStatsAsync(int categoryId)
        {
            try
            {
                var result = await _repository.GetAllAsync();

                var categoryTransactions = result
                    .Where(t => t.CategoryId == categoryId)
                    .ToList();

                if (!categoryTransactions.Any())
                {
                    return OperationResult<CategoryStatsDto>.Ok(new CategoryStatsDto(), "No transactions found.");
                }

                var thisMonthAmount = categoryTransactions
                    .Where(t => t.Date.IsThisMonth())
                    .Sum(t => t.Amount);

                var dto = new CategoryStatsDto
                {
                    TotalCount = categoryTransactions.Count,
                    ThisMonthAmount = thisMonthAmount,
                    AverageAmount = categoryTransactions.Select(t => t.Amount).DefaultIfEmpty().Average()
                };

                return OperationResult<CategoryStatsDto>.Ok(dto, OperationMessages.DataRetrieved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<CategoryStatsDto>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<TransactionSummaryDto>> GetFilteredAsync(TransactionFilterDto filter)
        {
            try
            {
                var (fromDateUtc, toDateUtc) = _timeRangeService.GetDateRangeUtc(
                    filter.TimePeriod,
                    filter.FromDate,
                    filter.ToDate
                );

                // Llamar al repositorio con fechas ya convertidas a UTC
                var transactions = await _repository.GetFilteredAsync(
                    fromDateUtc,
                    toDateUtc,
                    filter.AccountIds,
                    filter.TransactionTypeIds
                );

                // Convertir entidades a DTOs (las fechas se convierten de UTC a zona local aquí)
                var transactionDtos = transactions.Select(x => x.MapToDto(_timeZoneService)).ToList();

                // Aplicar filtros adicionales en Application Layer
                transactionDtos = ApplyApplicationFilters(transactionDtos, filter);

                // Calcular estadísticas basadas en los datos filtrados (excluir transfers)
                var totalIncome = transactionDtos
                    .Where(t => t.IsIncome())
                    .Sum(t => t.Amount);

                var totalExpense = transactionDtos
                    .Where(t => t.IsExpense())
                    .Sum(t => t.Amount);

                var summary = new TransactionSummaryDto
                {
                    Transactions = transactionDtos,
                    TotalIncome = totalIncome,
                    TotalExpense = totalExpense,
                    Balance = totalIncome - Math.Abs(totalExpense),
                    TotalCount = transactionDtos.Count
                };

                return OperationResult<TransactionSummaryDto>.Ok(summary, OperationMessages.DataRetrieved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<TransactionSummaryDto>.Fail(OperationMessages.UnexpectedError);
            }
        }

        private List<TransactionDto> ApplyApplicationFilters(List<TransactionDto> transactions, TransactionFilterDto filter)
        {
            var filtered = transactions.AsEnumerable();

            // Filtro de búsqueda por texto
            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                var searchLower = filter.SearchText.ToLower();
                filtered = filtered.Where(t =>
                    t.Name.ToLower().Contains(searchLower) ||
                    (!string.IsNullOrWhiteSpace(t.Description) && t.Description.ToLower().Contains(searchLower)));
            }

            // Filtro por categoría
            if (filter.CategoryId.HasValue)
            {
                filtered = filtered.Where(t => t.CategoryId == filter.CategoryId.Value);
            }

            // Filtro por tipo de categoría
            if (filter.Type.HasValue)
            {
                filtered = filtered.Where(t => t.Category?.Type == filter.Type.Value);
            }

            return filtered
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.CreatedAt)
                .ToList();
        }
    }
}