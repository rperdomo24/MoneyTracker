namespace MoneyTracker.UI.Helpers
{
    public static class SelectorHelper
    {
        /// <summary>
        /// Shared rule for every dropdown: hidden items are not offered (unless they are the
        /// current selection, so editing an old record never loses its value) and the rest
        /// are sorted alphabetically by name.
        /// </summary>
        public static List<T> VisibleSorted<T>(
            IEnumerable<T> items,
            Func<T, string> name,
            Func<T, bool> isHidden,
            Func<T, bool> isSelected)
        {
            return items
                .Where(i => !isHidden(i) || isSelected(i))
                .OrderBy(i => name(i).Trim(), StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }
    }
}
