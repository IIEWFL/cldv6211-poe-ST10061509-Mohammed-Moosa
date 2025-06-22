// Code Attribution:
// 1. ASP.NET Core MVC Foreign Key Relationships with Navigation Properties — rynop — https://stackoverflow.com/questions/43803833
// 2. Using Data Annotations for Email and Date Validation in ASP.NET Core — Fei Han — https://stackoverflow.com/questions/42083954

using System;

namespace EventEase.Models
{
    public class BookingViewModel
    {
        // Booking basic info
        public int BookingId { get; set; }
        public string CustomerName { get; set; }
        public string ContactEmail { get; set; }
        public DateTime BookingDate { get; set; }
        public bool IsBooked { get; set; }

        // Foreign keys
        public int VenueId { get; set; }
        public int EventId { get; set; }

        // Display fields
        public string EventName { get; set; }
        public string VenueName { get; set; }

        // Venue's event type stored directly in Venue table
        public string VenueEventType { get; set; }

        // NEW: Venue availability (true/false)
        public bool VenueAvailable { get; set; }

        // Computed status
        public string Status => IsBooked ? "Confirmed" : "Cancelled";
    }
}
