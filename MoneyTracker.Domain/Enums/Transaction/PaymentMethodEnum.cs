namespace MoneyTracker.Domain.Enums.Transaction
{
    public enum PaymentMethodEnum
    {
        Cash = 1,          // 💵 Efectivo
        DebitCard = 2,     // 💳 Tarjeta de débito
        CreditCard = 3,    // 💳 Tarjeta de crédito
        BankTransfer = 4,  // 🏦 Transferencia bancaria
        MobilePayment = 5, // 📱 Pago móvil (ej. MercadoPago)
        Check = 6,         // 📝 Cheque
        GiftCard = 7,      // 🎁 Tarjeta de regalo
        Cryptocurrency = 8, // 🪙 Criptomonedas
        googlePay = 9,     // 📱 Google Pay
        applePay = 10,     // 🍏 Apple Pay
        PayPal = 11,       // 💳 PayPal
        Other = 12         // 🔄 Otro método no especificado
    }
}
