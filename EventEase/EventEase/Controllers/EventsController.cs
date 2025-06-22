// Code Attribution:
// 1. ASP.NET Core MVC DropDownList Selected Value Not Binding On Postback — rynop — https://stackoverflow.com/questions/43803833
// 2. How to Bind Multiple Models in ASP.NET Core MVC Post Action — Fei Han — https://stackoverflow.com/questions/42083954
// 3. How to Upload a File to Azure Blob Storage in ASP.NET Core — Jon Skeet — https://stackoverflow.com/questions/59358981

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Azure.Storage.Blobs;
using Azure.Storage;
using Azure.Storage.Sas;
using EventEase.Data;
using EventEase.Models;

namespace EventEase.Controllers
{
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly string _storageAccountName;
        private readonly string _storageAccountKey;
        private readonly string _containerName;

        public EventsController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _storageAccountName = configuration["AzureStorage:AccountName"];
            _storageAccountKey = configuration["AzureStorage:AccountKey"];
            _containerName = configuration["AzureStorage:ContainerName"];
        }

        public async Task<IActionResult> Index()
        {
            var events = await _context.Events.Include(e => e.Bookings).ToListAsync();
            return View(events);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var @event = await _context.Events.FirstOrDefaultAsync(m => m.EventId == id);
            if (@event == null) return NotFound();

            return View(@event);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EventId,Name,Description,StartDate,EndDate")] Event @event, IFormFile imageFile)
        {
            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    var blobName = await UploadImageToBlobStorageAsync(imageFile);
                    @event.ImageURL = blobName;
                }

                _context.Add(@event);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(@event);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var @event = await _context.Events.FindAsync(id);
            if (@event == null) return NotFound();

            return View(@event);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("EventId,Name,Description,StartDate,EndDate,ImageURL")] Event @event, IFormFile imageFile)
        {
            if (id != @event.EventId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingEvent = await _context.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == id);

                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var newBlobName = await UploadImageToBlobStorageAsync(imageFile);

                        if (!string.IsNullOrEmpty(existingEvent.ImageURL))
                        {
                            await DeleteBlobAsync(existingEvent.ImageURL);
                        }

                        @event.ImageURL = newBlobName;
                    }
                    else
                    {
                        @event.ImageURL = existingEvent?.ImageURL;
                    }

                    _context.Update(@event);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EventExists(@event.EventId)) return NotFound();
                    else throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(@event);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var @event = await _context.Events.FirstOrDefaultAsync(m => m.EventId == id);
            if (@event == null) return NotFound();

            return View(@event);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var hasBookings = await _context.Bookings.AnyAsync(b => b.EventId == id);
            if (hasBookings)
            {
                TempData["ErrorMessage"] = "Cannot delete this event because it has existing bookings.";
                return RedirectToAction(nameof(Index));
            }

            var @event = await _context.Events.FindAsync(id);
            if (@event != null)
            {
                if (!string.IsNullOrEmpty(@event.ImageURL))
                {
                    await DeleteBlobAsync(@event.ImageURL);
                }

                _context.Events.Remove(@event);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool EventExists(int id)
        {
            return _context.Events.Any(e => e.EventId == id);
        }

        private async Task<string> UploadImageToBlobStorageAsync(IFormFile file)
        {
            var credential = new StorageSharedKeyCredential(_storageAccountName, _storageAccountKey);
            var blobServiceClient = new BlobServiceClient(new Uri($"https://{_storageAccountName}.blob.core.windows.net"), credential);
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);

            await containerClient.CreateIfNotExistsAsync();

            var blobName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var blobClient = containerClient.GetBlobClient(blobName);

            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, overwrite: true);

            return blobName;
        }

        private async Task DeleteBlobAsync(string blobName)
        {
            var credential = new StorageSharedKeyCredential(_storageAccountName, _storageAccountKey);
            var blobServiceClient = new BlobServiceClient(new Uri($"https://{_storageAccountName}.blob.core.windows.net"), credential);
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);

            var blobClient = containerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync();
        }

        // ✅ NEW: Generate SAS URL for image viewing
        public IActionResult GetImageSasUrl(string blobName)
        {
            var sasUrl = GenerateBlobSasUrl(blobName);
            return Redirect(sasUrl);
        }

        private string GenerateBlobSasUrl(string blobName)
        {
            var blobServiceClient = new BlobServiceClient(
                new Uri($"https://{_storageAccountName}.blob.core.windows.net"),
                new StorageSharedKeyCredential(_storageAccountName, _storageAccountKey));

            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasToken = sasBuilder.ToSasQueryParameters(
                new StorageSharedKeyCredential(_storageAccountName, _storageAccountKey)).ToString();

            return $"{blobClient.Uri}?{sasToken}";
        }
    }
}
