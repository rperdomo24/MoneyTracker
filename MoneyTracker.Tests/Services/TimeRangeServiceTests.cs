using FluentAssertions;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Application.Services;
using MoneyTracker.Domain.Enums.Filters;
using Moq;

namespace MoneyTracker.Tests.Services
{
    public class TimeRangeServiceTests
    {
        private readonly Mock<ITimeZoneService> _tz = new();

        private TimeRangeService CreateService() => new(_tz.Object);

        [Fact]
        public void GetDateRangeUtc_Last7Days_UsesLocalTodayBoundaries()
        {
            // Time range is calculated from the user's local "today", then converted to UTC.
            var localNow = new DateTime(2026, 3, 5, 14, 0, 0);
            _tz.Setup(x => x.GetLocalTimeInConfiguredTimeZone()).Returns(localNow);
            _tz.Setup(x => x.ConvertToUtc(It.IsAny<DateTime>())).Returns((DateTime d) => d);
            var svc = CreateService();

            var (start, end) = svc.GetDateRangeUtc(TimePeriodFilter.Last7Days);

            start.Should().Be(new DateTime(2026, 2, 26));
            end.Should().Be(new DateTime(2026, 3, 5).AddDays(1).AddTicks(-1));
        }

        [Fact]
        public void GetDateRangeUtc_ThisMonth_ReturnsMonthStartAndEnd()
        {
            _tz.Setup(x => x.GetLocalTimeInConfiguredTimeZone()).Returns(new DateTime(2026, 3, 20, 8, 0, 0));
            _tz.Setup(x => x.ConvertToUtc(It.IsAny<DateTime>())).Returns((DateTime d) => d);
            var svc = CreateService();

            var (start, end) = svc.GetDateRangeUtc(TimePeriodFilter.ThisMonth);

            start.Should().Be(new DateTime(2026, 3, 1, 0, 0, 0));
            end.Should().Be(new DateTime(2026, 3, 31, 23, 59, 59));
        }

        [Fact]
        public void GetDateRangeUtc_Custom_UsesToDateEndOfDay()
        {
            // Custom end date must include the full day (23:59:59.9999999).
            _tz.Setup(x => x.ConvertToUtc(It.IsAny<DateTime>())).Returns((DateTime d) => d);
            var svc = CreateService();

            var (start, end) = svc.GetDateRangeUtc(
                TimePeriodFilter.Custom,
                new DateTime(2026, 1, 10),
                new DateTime(2026, 1, 15));

            start.Should().Be(new DateTime(2026, 1, 10, 0, 0, 0));
            end.Should().Be(new DateTime(2026, 1, 15, 23, 59, 59, 999).AddTicks(9999));
        }

        [Fact]
        public void GetDateRangeUtc_AllTime_ReturnsNullRange()
        {
            var svc = CreateService();

            var (start, end) = svc.GetDateRangeUtc(TimePeriodFilter.AllTime);

            start.Should().BeNull();
            end.Should().BeNull();
        }
    }
}
