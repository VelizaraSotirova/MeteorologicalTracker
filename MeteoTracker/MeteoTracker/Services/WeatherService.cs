using Microsoft.EntityFrameworkCore;
using MeteoTracker.Data;
using MeteoTracker.DTOs; 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MeteoTracker.Services
{
    public class WeatherService
    {
        private readonly ApplicationDbContext _context;

        public WeatherService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<object>> GetStationsAsync()
        {
            return await _context.WeatherStations
                .Select(s => new { s.Id, s.Name })
                .ToListAsync();
        }

        public async Task<(int TotalCount, IEnumerable<object> Data)> GetMeasurementsAsync(
            int stationId, DateTime fromDate, DateTime toDate, int page, int pageSize)
        {
            // Base query about the measurements for the specified station and date range
            var query = _context.WeatherMeasurements
                .Where(m => m.StationId == stationId && m.Timestamp >= fromDate && m.Timestamp <= toDate);

            // Count the total number of records for pagination purposes
            var totalCount = await query.CountAsync();

            // Fetch only the records for the specific page (Pagination)
            var data = await query
                .OrderBy(m => m.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new
                {
                    m.Id,
                    m.Timestamp,
                    m.Temperature,
                    m.Humidity
                })
                .ToListAsync();

            return (totalCount, data);
        }


        public async Task<ClimateStatsDto?> GetStatisticsAsync(int stationId, DateTime fromDate, DateTime toDate)
        {
            // 1. Download all measurements for the specified station and date range
            var measurements = await _context.WeatherMeasurements
                .Where(m => m.StationId == stationId && m.Timestamp >= fromDate && m.Timestamp <= toDate)
                .ToListAsync();

            // If there are no records for this period, return null
            if (!measurements.Any())
            {
                return null;
            }

            // 2. Calculate the basic statistics (average, max, min, average humidity, total count)
            decimal avgTemp = measurements.Average(m => m.Temperature);
            decimal maxTemp = measurements.Max(m => m.Temperature);
            decimal minTemp = measurements.Min(m => m.Temperature);
            decimal avgHum = measurements.Average(m => m.Humidity);
            int totalCount = measurements.Count;

            // 3. Calculate the climate average temperature based on the specific formula
            var groupedByDay = measurements
                .GroupBy(m => m.Timestamp.Date)
                .Select(g => new
                {
                    T08 = g.FirstOrDefault(m => m.Timestamp.Hour == 8)?.Temperature,
                    T15 = g.FirstOrDefault(m => m.Timestamp.Hour == 15)?.Temperature,
                    T22 = g.FirstOrDefault(m => m.Timestamp.Hour == 22)?.Temperature
                })
                // Take only the days where we have all three measurements (08:00, 15:00, 22:00)
                .Where(d => d.T08 != null && d.T15 != null && d.T22 != null)
                .ToList();

            decimal? climateAvgTemp = null;
            if (groupedByDay.Any())
            {
                decimal sumOfDailyAverages = groupedByDay
                    .Sum(d => (d.T08.Value + d.T15.Value + (2 * d.T22.Value)) / 4.0m);

                climateAvgTemp = sumOfDailyAverages / groupedByDay.Count;
            }

            // 4. Return the results in a ClimateStatsDto object
            return new ClimateStatsDto
            {
                AverageTemperature = avgTemp,
                MaxTemperature = maxTemp,
                MinTemperature = minTemp,
                AverageHumidity = avgHum,
                TotalMeasurements = totalCount,
                ClimateAverageTemperature = climateAvgTemp
            };
        }
    }
}