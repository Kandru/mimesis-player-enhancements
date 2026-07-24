using System.Collections.Immutable;

namespace MimesisPlayerEnhancement.Features.Weather
{
    internal static class WeatherScheduleRebuilder
    {
        private const string Feature = "Weather";

        internal static void StripRandomWeather(DungeonWeather weather, int dayCount, int randomSeed, int overrideDefaultWeatherId)
        {
            if (!weather.IsRandomOccured)
            {
                return;
            }

            ExcelDataManager? excel = HubGameDataAccess.Excel;
            if (excel == null)
            {
                return;
            }

            ContaInfo? contaInfo = excel.GetContaInfoByDay(dayCount);
            if (contaInfo == null)
            {
                return;
            }

            try
            {
                List<int> rebuiltHours = BuildNonRandomSchedule(contaInfo, randomSeed, overrideDefaultWeatherId);
                List<bool> rebuiltForecast = new(new bool[24]);
                WeatherRoomAccess.ApplySchedule(weather, rebuiltHours, rebuiltForecast, isRandomOccured: false);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"StripRandomWeather failed — {ex.Message}");
            }
        }

        private static List<int> BuildNonRandomSchedule(ContaInfo contaInfo, int randomSeed, int overrideDefaultWeatherId)
        {
            int defaultWeatherId = contaInfo.DefaultWeatherID;
            if (overrideDefaultWeatherId > 0)
            {
                defaultWeatherId = overrideDefaultWeatherId;
            }

            List<int> weatherByHour = new(24);
            for (int i = 0; i < 24; i++)
            {
                weatherByHour.Add(defaultWeatherId);
            }

            Random random = new(randomSeed);
            ImmutableArray<WeatherTimeInfo>.Enumerator enumerator = contaInfo.WeatherChanges.GetEnumerator();
            while (enumerator.MoveNext())
            {
                WeatherTimeInfo current = enumerator.Current;
                int hours = TimeSpan.FromSeconds(current.rangeStartTimeSec).Hours;
                int hours2 = TimeSpan.FromSeconds(current.rangeEndTimeSec).Hours;
                for (int i = ((hours2 - hours > 0) ? random.Next(hours, hours2 + 1) : hours); i < weatherByHour.Count; i++)
                {
                    weatherByHour[i] = current.weatherId;
                }
            }

            return weatherByHour;
        }
    }
}
