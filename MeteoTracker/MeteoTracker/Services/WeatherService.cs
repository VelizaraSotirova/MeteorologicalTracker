using Microsoft.EntityFrameworkCore;
using MeteoTracker.Data;
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

        
        public async Task<object?> GetStatisticsAsync(int stationId, DateTime fromDate, DateTime toDate)
        {
            var measurements = _context.WeatherMeasurements
                .Where(m => m.StationId == stationId && m.Timestamp >= fromDate && m.Timestamp <= toDate);

            // If there are no records for this period, return null
            if (!await measurements.AnyAsync())
            {
                return null;
            }

            // Group the data and let SQL Server calculate the aggregates very quickly
            return await measurements
                .GroupBy(m => m.StationId)
                .Select(g => new
                {
                    AverageTemperature = g.Average(m => m.Temperature),
                    MaxTemperature = g.Max(m => m.Temperature),
                    MinTemperature = g.Min(m => m.Temperature),
                    AverageHumidity = g.Average(m => m.Humidity),
                    TotalMeasurements = g.Count()
                })
                .FirstOrDefaultAsync();
        }
    }
}