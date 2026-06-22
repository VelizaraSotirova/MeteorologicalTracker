using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MeteoTracker.Entities
{
    public class WeatherStation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } 

        // Connection with the measurements (One station has many measurements)
        public ICollection<WeatherMeasurement> Measurements { get; set; } = new List<WeatherMeasurement>();
    }
}