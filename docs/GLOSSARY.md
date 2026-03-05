# Glossary

- Account: Financial container (cash/bank/credit/etc.) with balance, credit limit, icon, and color (`Domain/Entities/Account.cs`).
- Category: Classification for transactions, supports parent-child tree and type (`Income`, `Expense`, `Transfer`).
- Transaction: Financial movement linked to one account and one category; supports transfer pairing and status.
- Transfer: Two linked transactions (outgoing/incoming) connected by `TransferPairId`.
- Budget: Monthly amount target per category with optional child inclusion and rollover behavior.
- Dashboard: Aggregated finance views (net worth, cash flow, alerts, trends, comparisons).
- Expense: Outflow category/transaction concept; often normalized with absolute value in summary views.
- Income: Inflow category/transaction concept.
- OperationResult: Standard service response wrapper with success flag, message, and optional data.
- TimeRange filter: Date interval abstraction from `TimePeriodFilter` (last 7/30/90 days, month, year, custom, all time).

## Key enums
- `AccountType`: `None`, `Cash`, `Bank`, `Credit`, `Investment`, `Savings`, `Checking`.
- `CategoryTypeEnum`: `Income`, `Expense`, `Transfer`.
- `TransactionTypeEnum`: `Income`, `Expense`, `Transfer`, `CreditPayment`.
- `PaymentMethodEnum`: cash/card/transfer/mobile/check/crypto/etc. payment channels.
- `TransactionStatus`: `Completed`, `Pending`, `Scheduled`, `Planned`.
- `RolloverMode`: `None`, `CarryOverRemaining`, `CarryOverOverspend`, `Reset`.

## Icon-related enums
- `AccountIcon`: wallet/bank/savings/credit/payments icon options for accounts.
- `CategoryIcon`: large set of material-style icon names used by categories.

## System categories
- IDs `1-10` are reserved as system categories (`SystemCategories.cs`).
- They include initial balance, balance adjustment, transfer out/in, credit payment/advance pairs.
