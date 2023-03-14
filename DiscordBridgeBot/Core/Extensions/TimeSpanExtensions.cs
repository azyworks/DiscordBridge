namespace DiscordBridgeBot.Core.Extensions
{
    public static class TimeSpanExtensions
    {
        public static string ToReadableString(this TimeSpan span)
        {
            return string.Join(", ", span.GetReadableStringElements()
               .Where(str => !string.IsNullOrWhiteSpace(str)));
        }

        private static IEnumerable<string> GetReadableStringElements(this TimeSpan span)
        {
            yield return GetYearsString((int)Math.Floor(span.TotalDays / 365));
            yield return GetMonthsString((int)Math.Floor(span.TotalDays / 30));
            yield return GetWeeksString((int)Math.Floor(span.TotalDays / 7));
            yield return GetDaysString((int)Math.Floor(span.TotalDays));
            yield return GetHoursString(span.Hours);
            yield return GetMinutesString(span.Minutes);
            yield return GetSecondsString(span.Seconds);
        }

        private static string GetYearsString(int years)
        {
            if (years == 0)
                return string.Empty;

            if (years == 1)
                return "1 rok";

            if (years >= 2 && years <= 4)
                return $"{years} roky";

            return $"{years} let";
        }

        private static string GetMonthsString(int months)
        {
            if (months == 0)
                return string.Empty;

            if (months == 1)
                return "1 měsíc";

            if (months >= 2 && months <= 4)
                return $"{months} měsíce";

            return $"{months} měsíců";
        }

        private static string GetWeeksString(int weeks)
        {
            if (weeks == 0)
                return string.Empty;

            if (weeks == 1)
                return "1 týden";

            if (weeks >= 2 && weeks <= 4)
                return $"{weeks} týdny";

            return $"{weeks} týdnů";
        }

        private static string GetDaysString(int days)
        {
            if (days == 0)
                return string.Empty;

            if (days == 1)
                return "1 den";

            if (days >= 2 && days <= 4)
                return $"{days} dny";

            return $"{days} dní";
        }

        private static string GetHoursString(int hours)
        {
            if (hours == 0)
                return string.Empty;

            if (hours == 1)
                return "1 hodina";

            if (hours >= 2 && hours <= 4)
                return $"{hours} hodiny";

            return $"{hours} hodin";
        }

        private static string GetMinutesString(int minutes)
        {
            if (minutes == 0)
                return string.Empty;

            if (minutes == 1)
                return "1 minuta";

            if (minutes >= 2 && minutes <= 4)
                return $"{minutes} minuty";

            return $"{minutes} minut";
        }

        private static string GetSecondsString(int seconds)
        {
            if (seconds == 0)
                return string.Empty;

            if (seconds == 1)
                return "1 sekunda";

            if (seconds >= 2 && seconds <= 4)
                return $"{seconds} sekundy";

            return $"{seconds} sekund";
        }
    }
}
