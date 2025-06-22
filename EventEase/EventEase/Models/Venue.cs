// Code Attribution:
// 1. ASP.NET Core Model Validation using [Required] and [StringLength] — Fei Han — https://stackoverflow.com/questions/42083954/how-to-use-data-annotations-for-model-validation-in-asp-net-core
// 2. EF Core Navigation Property Example with Bookings — Smit Patel — https://stackoverflow.com/questions/39422550/ef-core-one-to-many-relationship-example


using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventEase.Models
{
    public class Venue
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }  // Primary Key

        [Required]
        [StringLength(100)]  // Limit the length of the venue name to 100 characters
        public string Name { get; set; }

        [Required]
        [StringLength(200)]  // Limit the length of the location to 200 characters
        public string Location { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Capacity must be greater than zero.")]
        public int Capacity { get; set; }

        [StringLength(500)]
        public string? ImageURL { get; set; }  // Make it nullable so it's optional in the form

        public bool IsAvailable { get; set; } = true; // Default value

        // Navigation property to represent the relationship with Booking
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public string? EventType { get; set; }  // Allow user to type directly



    }
}