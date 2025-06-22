using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;  // ✅ Needed for [ValidateNever]

namespace EventEase.Models
{
    public class Booking
    {
        [Key]
        [Column("BookingId")]
        public int BookingId { get; set; }

        [Required(ErrorMessage = "Customer name is required.")]
        [StringLength(100, ErrorMessage = "Customer name cannot exceed 100 characters.")]
        public string CustomerName { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string ContactEmail { get; set; }

        [Required(ErrorMessage = "Booking date is required.")]
        public DateTime BookingDate { get; set; } = DateTime.Now;

        [Column("IsBooked")]
        public bool IsBooked { get; set; } = true;

        // Foreign keys
        [Required(ErrorMessage = "Venue ID is required.")]
        public int VenueId { get; set; }

        [Required(ErrorMessage = "Event ID is required.")]
        public int EventId { get; set; }

        // ✅ Navigation properties (excluded from model validation)
        [ForeignKey("VenueId")]
        [ValidateNever]  // Prevents "The Venue field is required" error
        public virtual Venue Venue { get; set; }

        [ForeignKey("EventId")]
        [ValidateNever]  // Prevents "The Event field is required" error
        public virtual Event Event { get; set; }

        // Read-only alias
        public int Id => BookingId;
    }
}
