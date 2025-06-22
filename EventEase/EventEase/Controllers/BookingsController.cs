// How to use SelectList with ViewBag in ASP.NET Core — Rick Strahl — https://stackoverflow.com/questions/40030008/how-to-use-selectlist-in-mvc-core-viewbag

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EventEase.Data;
using EventEase.Models;

namespace EventEase.Controllers
{
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Standard Booking List
        public async Task<IActionResult> Index()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.Venue)
                .ToListAsync();

            return View(bookings);
        }

        // GET: Enhanced Booking Search
        public async Task<IActionResult> EnhancedIndex(string searchString, string eventType, DateTime? startDate, DateTime? endDate, bool? isAvailable)
        {
            // Populate distinct EventType values from Venue.EventType (string)
            var eventTypeList = await _context.Venues
                .Where(v => !string.IsNullOrEmpty(v.EventType))
                .Select(v => v.EventType.Trim())
                .Distinct()
                .OrderBy(name => name)
                .ToListAsync();

            ViewBag.EventTypeList = new SelectList(eventTypeList);

            // Retrieve bookings
            var bookings = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.Venue)
                .Select(b => new BookingViewModel
                {
                    BookingId = b.BookingId,
                    CustomerName = b.CustomerName,
                    ContactEmail = b.ContactEmail,
                    BookingDate = b.BookingDate,
                    IsBooked = b.IsBooked,
                    VenueId = b.VenueId,
                    EventId = b.EventId,
                    EventName = b.Event.Name,
                    VenueName = b.Venue.Name,
                    VenueEventType = b.Venue.EventType ?? "",
                    VenueAvailable = b.Venue.IsAvailable
                })
                .ToListAsync();

            // Apply filters
            if (!string.IsNullOrEmpty(searchString))
            {
                bookings = bookings.Where(b =>
                    b.BookingId.ToString().Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                    b.CustomerName.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                    b.EventName.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                    b.VenueName.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrEmpty(eventType))
            {
                bookings = bookings.Where(b => b.VenueEventType?.Trim().Equals(eventType.Trim(), StringComparison.OrdinalIgnoreCase) == true).ToList();
            }

            if (startDate.HasValue)
            {
                bookings = bookings.Where(b => b.BookingDate.Date >= startDate.Value.Date).ToList();
            }

            if (endDate.HasValue)
            {
                bookings = bookings.Where(b => b.BookingDate.Date <= endDate.Value.Date).ToList();
            }

            if (isAvailable.HasValue)
            {
                bookings = bookings.Where(b => b.VenueAvailable == isAvailable.Value).ToList();
            }

            return View(bookings);
        }

        // --- Booking CRUD methods below (unchanged) ---

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.Venue)
                .FirstOrDefaultAsync(m => m.BookingId == id);

            if (booking == null) return NotFound();

            return View(booking);
        }

        public IActionResult Create()
        {
            PopulateVenueAndEventDropDowns();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CustomerName,ContactEmail,BookingDate,IsBooked,VenueId,EventId")] Booking booking)
        {
            if (booking.VenueId <= 0)
                ModelState.AddModelError("VenueId", "Please select a valid venue.");

            if (booking.EventId <= 0)
                ModelState.AddModelError("EventId", "Please select a valid event.");

            if (ModelState.IsValid)
            {
                var existingBooking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.VenueId == booking.VenueId
                        && b.BookingDate == booking.BookingDate
                        && b.IsBooked == true);

                if (existingBooking != null)
                {
                    ModelState.AddModelError("BookingDate", "This venue is already booked for the selected date and time.");
                }
                else
                {
                    _context.Add(booking);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }

            PopulateVenueAndEventDropDowns(booking.VenueId, booking.EventId);
            return View(booking);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            PopulateVenueAndEventDropDowns(booking.VenueId, booking.EventId);
            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("BookingId,CustomerName,ContactEmail,BookingDate,IsBooked,VenueId,EventId")] Booking booking)
        {
            if (id != booking.BookingId) return NotFound();

            if (ModelState.IsValid)
            {
                var existingBooking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.VenueId == booking.VenueId
                        && b.BookingDate == booking.BookingDate
                        && b.IsBooked == true
                        && b.BookingId != booking.BookingId);

                if (existingBooking != null)
                {
                    ModelState.AddModelError("BookingDate", "This venue is already booked for the selected date and time.");
                }
                else
                {
                    _context.Update(booking);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }

            PopulateVenueAndEventDropDowns(booking.VenueId, booking.EventId);
            return View(booking);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.Venue)
                .FirstOrDefaultAsync(m => m.BookingId == id);

            if (booking == null) return NotFound();

            return View(booking);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking != null)
            {
                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private void PopulateVenueAndEventDropDowns(int? selectedVenueId = null, int? selectedEventId = null)
        {
            ViewBag.VenueList = new SelectList(_context.Venues.OrderBy(v => v.Name), "Id", "Name", selectedVenueId);
            ViewBag.EventList = new SelectList(_context.Events.OrderBy(e => e.Name), "EventId", "Name", selectedEventId);
        }

        private bool BookingExists(int id)
        {
            return _context.Bookings.Any(e => e.BookingId == id);
        }
    }
}
