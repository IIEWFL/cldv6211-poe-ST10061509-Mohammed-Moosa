// How to use SelectList with ViewBag in ASP.NET Core — Rick Strahl — https://stackoverflow.com/questions/40030008/how-to-use-selectlist-in-mvc-core-viewbag

using System.ComponentModel.DataAnnotations;

namespace EventEase.Models
{
    public class EventType
    {
        public int EventTypeId { get; set; }

        [Required]
        public string Name { get; set; }
    }
}
