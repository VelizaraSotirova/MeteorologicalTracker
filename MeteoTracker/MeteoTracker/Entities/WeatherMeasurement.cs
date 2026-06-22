using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MeteoTracker.Entities
{
    public class WeatherMeasurement
    {
        [Key]
        public int Id { get; set; }

        // Foreign key to the station
        [Required]
        public int StationId { get; set; }

        [ForeignKey(nameof(StationId))]
        public WeatherStation Station { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        [Required]
        [Column(TypeName = "decimal(5, 2)")]
        public decimal Temperature { get; set; }

        [Required]
        [Column(TypeName = "decimal(5, 2)")]
        public decimal Humidity { get; set; }
    }
}