using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bike.Models
{
    [Table("fuel_logs")]
    public class FuelLog
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("fuel_date")]
        public DateTime FuelDate { get; set; }

        [Column("fuel_liter")]
        public double FuelLiter { get; set; }

        [Column("distance_km")]
        public double DistanceKm { get; set; }

        [Column("cost")]
        public double? Cost { get; set; }

        [Column("currency")]
        public string? Currency { get; set; }

        [NotMapped]
        public double FuelEfficiency
        {
            get
            {
                return FuelLiter > 0 ? DistanceKm / FuelLiter : 0;
            }
        }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}