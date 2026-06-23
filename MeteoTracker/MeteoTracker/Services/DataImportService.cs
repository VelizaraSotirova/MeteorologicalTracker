using Microsoft.EntityFrameworkCore;
using MeteoTracker.Data;
using MeteoTracker.Entities;
using ExcelDataReader;
using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace MeteoTracker.Services
{
    public class DataImportService
    {
        private readonly ApplicationDbContext _context;

        public DataImportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> ImportCsvAsync(string filePath, string fileName)
        {
            string stationName = fileName.Split(' ')[0].Trim();
            if (string.IsNullOrEmpty(stationName) || stationName.Contains('.'))
            {
                throw new ArgumentException("Неуспешно извличане на името на станцията.");
            }
            stationName = char.ToUpper(stationName[0]) + stationName.Substring(1).ToLower();

            var station = await _context.WeatherStations
                .FirstOrDefaultAsync(s => s.Name == stationName);

            if (station == null)
            {
                station = new WeatherStation { Name = stationName };
                _context.WeatherStations.Add(station);
                await _context.SaveChangesAsync();
            }

            int recordsImported = 0;

            // Time zone for Bulgaria (FLE Standard Time)
            TimeZoneInfo bgTimeZone = TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time");

            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    var result = reader.AsDataSet();
                    var table = result.Tables[0];

                    for (int i = 1; i < table.Rows.Count; i++)
                    {
                        var row = table.Rows[i];
                        if (row.ItemArray.Length < 4) continue;

                        string dateText = row[1]?.ToString()?.Trim() ?? "";
                        string tempText = row[2]?.ToString()?.Trim() ?? "";
                        string humidityText = row[3]?.ToString()?.Trim() ?? "";

                        if (string.IsNullOrEmpty(dateText) || string.IsNullOrEmpty(tempText) || string.IsNullOrEmpty(humidityText))
                            continue;

                        // Data parsing with multiple formats
                        if (!DateTime.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime timestamp))
                        {
                            if (!DateTime.TryParseExact(dateText, "d.M.yyyy H:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out timestamp))
                            {
                                if (!DateTime.TryParseExact(dateText, "M.d.yyyy H:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out timestamp))
                                {
                                    continue;
                                }
                            }
                        }

                        // FILTER BY HOUR AND MINUTES (CLIMATIC TERMS)
                        // 1. Check for minutes - must be exactly 25
                        if (timestamp.Minute != 25)
                        {
                            continue; // Skip the row immediately, without loading the database 
                        }

                        // 2. Check if the specific date is in Daylight Saving Time (DST) according to the BG time zone
                        bool isSubmitedInDst = bgTimeZone.IsDaylightSavingTime(timestamp);
                        int requiredEveningHour = isSubmitedInDst ? 22 : 21; // 22 for summer, 21 for winter

                        // 3. Check if the hour matches one of the three terms      
                        if (timestamp.Hour != 8 && timestamp.Hour != 15 && timestamp.Hour != requiredEveningHour)
                        {
                            continue; // We don't need this hour, skip the row
                        }

                        // Parsing numbers (only for approved terms)
                        if (!decimal.TryParse(tempText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal temperature) ||
                            !decimal.TryParse(humidityText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal humidity))
                        {
                            continue;
                        }

                        // Prevent duplication: Check if a record with the same station and timestamp already exists 
                        bool alreadyExists = await _context.WeatherMeasurements
                            .AnyAsync(m => m.StationId == station.Id && m.Timestamp == timestamp);

                        if (alreadyExists) continue;

                        // Create record and add to the database
                        var measurement = new WeatherMeasurement
                        {
                            StationId = station.Id,
                            Timestamp = timestamp,
                            Temperature = temperature,
                            Humidity = humidity
                        };

                        _context.WeatherMeasurements.Add(measurement);
                        recordsImported++;

                        if (recordsImported % 500 == 0)
                        {
                            await _context.SaveChangesAsync();
                        }
                    }
                }
            }

            if (recordsImported % 500 != 0)
            {
                await _context.SaveChangesAsync();
            }

            return recordsImported;
        }
    }
}