namespace MeteoTracker.DTOs
{
    public class ClimateStatsDto
    {
        public decimal AverageTemperature { get; set; }
        public decimal MaxTemperature { get; set; }
        public decimal MinTemperature { get; set; }
        public decimal AverageHumidity { get; set; }
        public int TotalMeasurements { get; set; }
        public decimal? ClimateAverageTemperature { get; set; }
    }
}