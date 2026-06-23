using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MeteoTracker.Services;
using System;
using System.Threading.Tasks;

namespace MeteoTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,User")] // Accessible to all logged-in users in the system
    public class WeatherController : ControllerBase
    {
        private readonly WeatherService _weatherService;

        public WeatherController(WeatherService weatherService)
        {
            _weatherService = weatherService;
        }

        // Endpoint for retrieving the list of stations
        [HttpGet("stations")]
        public async Task<IActionResult> GetStations()
        {
            var stations = await _weatherService.GetStationsAsync();
            return Ok(stations);
        }

        // Endpoint for retrieving measurements with date filters and pagination
        [HttpGet("measurements/{stationId}")]
        public async Task<IActionResult> GetMeasurements(
            int stationId,
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 100)
        {
            var (totalCount, data) = await _weatherService.GetMeasurementsAsync(stationId, fromDate, toDate, page, pageSize);

            return Ok(new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Data = data
            });
        }

        // Endpoint for calculating average values (Min/Max/Average)
        [HttpGet("statistics/{stationId}")]
        public async Task<IActionResult> GetStatistics(int stationId, [FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
        {
            var stats = await _weatherService.GetStatisticsAsync(stationId, fromDate, toDate);

            if (stats == null)
            {
                return NotFound("Няма намерени данни за тази станция в посочения период.");
            }

            return Ok(stats);
        }
    }
}