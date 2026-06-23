using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MeteoTracker.Data;
using MeteoTracker.Entities;

namespace MeteoTracker.Services
{
    public class DataImportService
    {
        private readonly ApplicationDbContext _context;

        public DataImportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> ImportCsvAsync(string filePath, string stationName)
        {
            // 1. Проверяваме дали станцията съществува, ако не - я създаваме
            var station = await _context.WeatherStations
                .FirstOrDefaultAsync(s => s.Name.ToLower() == stationName.ToLower());

            if (station == null)
            {
                station = new WeatherStation { Name = stationName };
                _context.WeatherStations.Add(station);
                await _context.SaveChangesAsync();
            }

            // Вземаме последното записвано време за тази станция, за да не дублираме данни, ако качим файла пак
            var lastTimestamp = await _context.WeatherMeasurements
                .Where(m => m.StationId == station.Id)
                .Select(m => (DateTime?)m.Timestamp)
                .MaxAsync() ?? DateTime.MinValue;

            var lines = await File.ReadAllLinesAsync(filePath);
            int importedCount = 0;

            // Пропускаме заглавния ред (Index, Timestamp...)
            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var tokens = line.Split(',');
                if (tokens.Length < 4) continue;

                string rawTimestamp = tokens[1].Trim();
                string rawTemp = tokens[2].Trim();
                string rawHumidity = tokens[3].Trim();

                // Парсваме датата точно по формата в лога (M.d.yyyy H:mm:ss)
                if (DateTime.TryParseExact(rawTimestamp, "M.d.yyyy H:mm:ss",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime timestamp))
                {
                    // Ако този запис вече съществува в базата, го пропускаме
                    if (timestamp <= lastTimestamp) continue;

                    if (decimal.TryParse(rawTemp, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal temp) &&
                        decimal.TryParse(rawHumidity, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal humidity))
                    {
                        var measurement = new WeatherMeasurement
                        {
                            StationId = station.Id,
                            Timestamp = timestamp,
                            Temperature = temp,
                            Humidity = humidity
                        };

                        _context.WeatherMeasurements.Add(measurement);
                        importedCount++;

                        // За да не претоварваме паметта при 12,000 реда, записваме в базата на порции от по 1000 реда
                        if (importedCount % 1000 == 0)
                        {
                            await _context.SaveChangesAsync();
                        }
                    }
                }
            }

            // Записваме останалите редове
            if (importedCount % 1000 != 0)
            {
                await _context.SaveChangesAsync();
            }

            return importedCount;
        }
    }
}