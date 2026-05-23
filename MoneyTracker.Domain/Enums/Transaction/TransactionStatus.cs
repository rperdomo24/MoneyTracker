namespace MoneyTracker.Domain.Enums.Transaction
{
    public enum TransactionStatus
    {
        Completed = 1,    // ✅ Ya ocurrió y está en el banco
        Pending = 2,      // ⏰ Ocurrió pero no aparece aún (compras con tarjeta)
        Scheduled = 3,    // 📅 Programado en banco (autopays futuros)
        Planned = 4       // 📝 Planning/presupuesto (futuro)
    }
}
