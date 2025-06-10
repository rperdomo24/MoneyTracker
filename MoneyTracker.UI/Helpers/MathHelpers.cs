namespace MoneyTracker.UI.Helpers;

public static class MathHelpers
{
    #region Amount Comparison Methods

    /// <summary>
    /// Determina si dos montos son similares dentro de un porcentaje de tolerancia
    /// </summary>
    public static bool IsSimilarAmount(decimal amount1, decimal amount2, decimal tolerance = 0.1m)
    {
        if (amount1 == 0 && amount2 == 0) return true;
        if (amount1 == 0 || amount2 == 0) return false;

        var difference = Math.Abs(amount1 - amount2);
        var average = (amount1 + amount2) / 2;
        return (difference / average) <= tolerance;
    }

    /// <summary>
    /// Obtiene el porcentaje de diferencia entre dos montos
    /// </summary>
    public static decimal GetPercentageDifference(decimal amount1, decimal amount2)
    {
        if (amount1 == 0 && amount2 == 0) return 0;
        if (amount1 == 0) return 100;

        return Math.Abs((amount2 - amount1) / amount1) * 100;
    }

    /// <summary>
    /// Determina si un monto está dentro de un rango
    /// </summary>
    public static bool IsInRange(decimal amount, decimal min, decimal max)
    {
        return amount >= min && amount <= max;
    }

    #endregion

    #region Percentage Methods

    /// <summary>
    /// Calcula el porcentaje que representa amount1 de amount2
    /// </summary>
    public static decimal GetPercentageOf(decimal amount1, decimal amount2)
    {
        if (amount2 == 0) return 0;
        return (amount1 / amount2) * 100;
    }

    /// <summary>
    /// Calcula el monto que representa un porcentaje de un total
    /// </summary>
    public static decimal GetAmountFromPercentage(decimal total, decimal percentage)
    {
        return total * (percentage / 100);
    }
    #endregion

    #region Rounding Methods

    /// <summary>
    /// Redondea un monto a la centésima más cercana (para monedas)
    /// </summary>
    public static decimal RoundToCurrency(this decimal amount)
    {
        return Math.Round(amount, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Redondea hacia arriba al entero más cercano
    /// </summary>
    public static decimal RoundUp(this decimal amount)
    {
        return Math.Ceiling(amount);
    }

    /// <summary>
    /// Redondea hacia abajo al entero más cercano
    /// </summary>
    public static decimal RoundDown(this decimal amount)
    {
        return Math.Floor(amount);
    }

    #endregion

    #region Statistical Methods

    /// <summary>
    /// Calcula el promedio de una lista de montos
    /// </summary>
    public static decimal CalculateAverage(this IEnumerable<decimal> amounts)
    {
        var amountList = amounts.ToList();
        if (!amountList.Any()) return 0;
        return amountList.Average();
    }

    /// <summary>
    /// Calcula la mediana de una lista de montos
    /// </summary>
    public static decimal CalculateMedian(this IEnumerable<decimal> amounts)
    {
        var sortedAmounts = amounts.OrderBy(x => x).ToList();
        if (!sortedAmounts.Any()) return 0;

        int count = sortedAmounts.Count;
        if (count % 2 == 0)
        {
            return (sortedAmounts[count / 2 - 1] + sortedAmounts[count / 2]) / 2;
        }
        else
        {
            return sortedAmounts[count / 2];
        }
    }

    /// <summary>
    /// Encuentra el valor máximo en una lista
    /// </summary>
    public static decimal FindMaximum(this IEnumerable<decimal> amounts)
    {
        return amounts.Any() ? amounts.Max() : 0;
    }

    /// <summary>
    /// Encuentra el valor mínimo en una lista
    /// </summary>
    public static decimal FindMinimum(this IEnumerable<decimal> amounts)
    {
        return amounts.Any() ? amounts.Min() : 0;
    }

    #endregion

    #region Validation Methods

    /// <summary>
    /// Determina si un monto es válido (mayor que 0)
    /// </summary>
    public static bool IsValidAmount(this decimal amount)
    {
        return amount > 0;
    }

    /// <summary>
    /// Determina si un monto es válido y está dentro de un rango aceptable
    /// </summary>
    public static bool IsValidAmountInRange(this decimal amount, decimal maxAmount = 1_000_000)
    {
        return amount > 0 && amount <= maxAmount;
    }

    /// <summary>
    /// Clamp un valor entre un mínimo y máximo
    /// </summary>
    public static decimal Clamp(this decimal value, decimal min, decimal max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    #endregion
}