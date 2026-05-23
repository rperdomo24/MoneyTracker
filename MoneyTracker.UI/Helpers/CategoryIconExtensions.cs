using MoneyTracker.Domain.Enums.Category;
using MudBlazor;

namespace MoneyTracker.UI.Helpers
{
    public static class CategoryIconExtensions
    {
        public static string ToIcon(this CategoryIcon icon)
        {
            return icon switch
            {
                // ============ INCOME ============
                CategoryIcon.AttachMoney => Icons.Material.Filled.AttachMoney,
                CategoryIcon.WorkOutline => Icons.Material.Filled.WorkOutline,
                CategoryIcon.TrendingUp => Icons.Material.Filled.TrendingUp,
                CategoryIcon.BusinessCenter => Icons.Material.Filled.BusinessCenter,
                CategoryIcon.AccountBalanceWallet => Icons.Material.Filled.AccountBalanceWallet,
                CategoryIcon.MonetizationOn => Icons.Material.Filled.MonetizationOn,
                CategoryIcon.CurrencyExchange => Icons.Material.Filled.CurrencyExchange,
                CategoryIcon.Paid => Icons.Material.Filled.Paid,
                CategoryIcon.RequestQuote => Icons.Material.Filled.RequestQuote,
                CategoryIcon.SavingsAccount => Icons.Material.Filled.AccountBalance,

                // ============ FOOD & DINING ============
                CategoryIcon.Restaurant => Icons.Material.Filled.Restaurant,
                CategoryIcon.LocalGroceryStore => Icons.Material.Filled.LocalGroceryStore,
                CategoryIcon.LocalCafe => Icons.Material.Filled.LocalCafe,
                CategoryIcon.LocalBar => Icons.Material.Filled.LocalBar,
                CategoryIcon.FastFood => Icons.Material.Filled.Fastfood,
                CategoryIcon.LocalPizza => Icons.Material.Filled.LocalPizza,
                CategoryIcon.RamenDining => Icons.Material.Filled.RamenDining,
                CategoryIcon.Icecream => Icons.Material.Filled.Icecream,
                CategoryIcon.Cake => Icons.Material.Filled.Cake,
                CategoryIcon.LocalDining => Icons.Material.Filled.LocalDining,
                CategoryIcon.Coffee => Icons.Material.Filled.Coffee,
                CategoryIcon.LunchDining => Icons.Material.Filled.LunchDining,
                CategoryIcon.Breakfast => Icons.Material.Filled.FreeBreakfast,

                // ============ TRANSPORTATION ============
                CategoryIcon.Commute => Icons.Material.Filled.Commute,
                CategoryIcon.DirectionsCar => Icons.Material.Filled.DirectionsCar,
                CategoryIcon.FlightTakeoff => Icons.Material.Filled.FlightTakeoff,
                CategoryIcon.Train => Icons.Material.Filled.Train,
                CategoryIcon.DirectionsBus => Icons.Material.Filled.DirectionsBus,
                CategoryIcon.Bike => Icons.Material.Filled.DirectionsBike,
                CategoryIcon.LocalTaxi => Icons.Material.Filled.LocalTaxi,
                CategoryIcon.LocalGasStation => Icons.Material.Filled.LocalGasStation,
                CategoryIcon.CarRepair => Icons.Material.Filled.CarRepair,

                // ============ HOME & UTILITIES ============
                CategoryIcon.Home => Icons.Material.Filled.Home,
                CategoryIcon.Bolt => Icons.Material.Filled.Bolt,
                CategoryIcon.Water => Icons.Material.Filled.Water,
                CategoryIcon.Wifi => Icons.Material.Filled.Wifi,
                CategoryIcon.PhoneIphone => Icons.Material.Filled.PhoneIphone,
                CategoryIcon.Cable => Icons.Material.Filled.Cable,
                CategoryIcon.CleaningServices => Icons.Material.Filled.CleaningServices,
                CategoryIcon.Plumbing => Icons.Material.Filled.Plumbing,
                CategoryIcon.ElectricalServices => Icons.Material.Filled.ElectricalServices,
                CategoryIcon.Security => Icons.Material.Filled.Security,

                // ============ HEALTH & WELLNESS ============
                CategoryIcon.LocalHospital => Icons.Material.Filled.LocalHospital,
                CategoryIcon.FitnessCenter => Icons.Material.Filled.FitnessCenter,
                CategoryIcon.Medication => Icons.Material.Filled.Medication,
                CategoryIcon.Psychology => Icons.Material.Filled.Psychology,
                CategoryIcon.Spa => Icons.Material.Filled.Spa,
                CategoryIcon.SelfImprovement => Icons.Material.Filled.SelfImprovement,
                CategoryIcon.HealthAndSafety => Icons.Material.Filled.HealthAndSafety,

                // ============ ENTERTAINMENT ============
                CategoryIcon.SportsEsports => Icons.Material.Filled.SportsEsports,
                CategoryIcon.Tv => Icons.Material.Filled.Tv,
                CategoryIcon.MusicNote => Icons.Material.Filled.MusicNote,
                CategoryIcon.Movie => Icons.Material.Filled.Movie,
                CategoryIcon.TheaterComedy => Icons.Material.Filled.TheaterComedy,
                CategoryIcon.SportsFootball => Icons.Material.Filled.SportsFootball,
                CategoryIcon.SportsBasketball => Icons.Material.Filled.SportsBasketball,
                CategoryIcon.SportsTennis => Icons.Material.Filled.SportsTennis,
                CategoryIcon.Casino => Icons.Material.Filled.Casino,
                CategoryIcon.Nightlife => Icons.Material.Filled.Nightlife,
                CategoryIcon.Weekend => Icons.Material.Filled.Weekend,

                // ============ SHOPPING ============
                CategoryIcon.ShoppingCart => Icons.Material.Filled.ShoppingCart,
                CategoryIcon.LocalMall => Icons.Material.Filled.LocalMall,
                CategoryIcon.CreditCard => Icons.Material.Filled.CreditCard,
                CategoryIcon.Payments => Icons.Material.Filled.Payments,
                CategoryIcon.Checkroom => Icons.Material.Filled.Checkroom,
                CategoryIcon.Diamond => Icons.Material.Filled.Diamond,
                CategoryIcon.Watch => Icons.Material.Filled.Watch,
                CategoryIcon.Redeem => Icons.Material.Filled.Redeem,

                // ============ EDUCATION & WORK ============
                CategoryIcon.School => Icons.Material.Filled.School,
                CategoryIcon.MenuBook => Icons.Material.Filled.MenuBook,
                CategoryIcon.Computer => Icons.Material.Filled.Computer,
                CategoryIcon.Laptop => Icons.Material.Filled.Laptop,
                CategoryIcon.Print => Icons.Material.Filled.Print,
                CategoryIcon.Class => Icons.Material.Filled.Class,

                // ============ FAMILY & PERSONAL ============
                CategoryIcon.Pets => Icons.Material.Filled.Pets,
                CategoryIcon.BabyChangingStation => Icons.Material.Filled.BabyChangingStation,
                CategoryIcon.Celebration => Icons.Material.Filled.Celebration,
                CategoryIcon.CardGiftcard => Icons.Material.Filled.CardGiftcard,
                CategoryIcon.FamilyRestroom => Icons.Material.Filled.FamilyRestroom,
                CategoryIcon.ChildCare => Icons.Material.Filled.ChildCare,
                CategoryIcon.Elderly => Icons.Material.Filled.Elderly,
                CategoryIcon.PersonalVideo => Icons.Material.Filled.PersonalVideo,

                // ============ FINANCIAL ============
                CategoryIcon.Savings => Icons.Material.Filled.Savings,
                CategoryIcon.AccountBalance => Icons.Material.Filled.AccountBalance,
                CategoryIcon.CreditScore => Icons.Material.Filled.CreditScore,
                CategoryIcon.Receipt => Icons.Material.Filled.Receipt,
                CategoryIcon.TrendingDown => Icons.Material.Filled.TrendingDown,
                CategoryIcon.PieChart => Icons.Material.Filled.PieChart,
                CategoryIcon.Assessment => Icons.Material.Filled.Assessment,

                // ============ SERVICES ============
                CategoryIcon.Construction => Icons.Material.Filled.Construction,
                CategoryIcon.LocalLaundryService => Icons.Material.Filled.LocalLaundryService,
                CategoryIcon.DryCleaning => Icons.Material.Filled.DryCleaning,
                CategoryIcon.Handyman => Icons.Material.Filled.Handyman,
                CategoryIcon.Carpenter => Icons.Material.Filled.Carpenter,
                CategoryIcon.Electrical => Icons.Material.Filled.ElectricalServices,

                // ============ MISCELLANEOUS ============
                CategoryIcon.EmojiEvents => Icons.Material.Filled.EmojiEvents,
                CategoryIcon.Star => Icons.Material.Filled.Star,
                CategoryIcon.Favorite => Icons.Material.Filled.Favorite,
                CategoryIcon.NewReleases => Icons.Material.Filled.NewReleases,
                CategoryIcon.Error => Icons.Material.Filled.Error,
                CategoryIcon.Help => Icons.Material.Filled.Help,
                CategoryIcon.Info => Icons.Material.Filled.Info,
                CategoryIcon.Category => Icons.Material.Filled.Category,
                _ => Icons.Material.Filled.Category
            };
        }
    }
}
