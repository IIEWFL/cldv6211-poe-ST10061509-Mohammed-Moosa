// Code Attribution:
// 1. How to Upload a File to Azure Blob Storage in ASP.NET Core — Jon Skeet — https://stackoverflow.com/questions/59358981/how-to-upload-a-file-to-azure-blob-storage-in-asp-net-core
// 2. Getting SAS Token URL for Azure Blob Storage — Gaurav Mantri — https://stackoverflow.com/questions/39282187/getting-sas-token-url-for-azure-blob-storage
// 3. How to Upload File With Edit Form ASP.NET Core MVC — Sam Axe — https://stackoverflow.com/questions/54112294/how-to-upload-file-with-edit-form-asp-net-core-mvc
// 4. EF Core Delete Entity With Related Data Check — Smit Patel — https://stackoverflow.com/questions/46667413/ef-core-delete-entity-with-related-data-check
// 5. How to use SelectList with ViewBag in ASP.NET Core — Rick Strahl — https://stackoverflow.com/questions/40030008/how-to-use-selectlist-in-mvc-core-viewbag

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Azure.Storage.Blobs;
using Azure.Storage;
using Azure.Storage.Sas;
using EventEase.Data;
using EventEase.Models;
using Microsoft.AspNetCore.Http;

namespace EventEase.Controllers
{
    public class VenuesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly string _storageAccountName;
        private readonly string _storageAccountKey;
        private readonly string _containerName;

        public VenuesController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _storageAccountName = configuration["AzureStorage:AccountName"];
            _storageAccountKey = configuration["AzureStorage:AccountKey"];
            _containerName = configuration["AzureStorage:ContainerName"];
        }

        // GET: Venues
        public async Task<IActionResult> Index()
        {
            var venues = await _context.Venues.Include(v => v.Bookings).ToListAsync();
            return View(venues);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var venue = await _context.Venues.FirstOrDefaultAsync(m => m.Id == id);
            if (venue == null) return NotFound();

            return View(venue);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Location,Capacity,IsAvailable,EventType")] Venue venue, IFormFile imageFile)


        {
            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    var blobName = await UploadImageToBlobStorageAsync(imageFile);
                    venue.ImageURL = blobName;
                }

                _context.Add(venue);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(venue);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var venue = await _context.Venues.FindAsync(id);
            if (venue == null) return NotFound();

            return View(venue);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Location,Capacity,ImageURL,IsAvailable")] Venue venue, IFormFile imageFile)
        {
            if (id != venue.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Get the existing venue from DB
                    var existingVenue = await _context.Venues.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);

                    if (imageFile != null && imageFile.Length > 0)
                    {
                        // Upload new image
                        var newBlobName = await UploadImageToBlobStorageAsync(imageFile);

                        // Delete old image if exists
                        if (!string.IsNullOrEmpty(existingVenue.ImageURL))
                        {
                            await DeleteBlobAsync(existingVenue.ImageURL);
                        }

                        venue.ImageURL = newBlobName; // Save new blob name
                    }
                    else
                    {
                        venue.ImageURL = existingVenue.ImageURL; // Keep old image if no new file
                    }

                    _context.Update(venue);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VenueExists(venue.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(venue);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var venue = await _context.Venues.FirstOrDefaultAsync(m => m.Id == id);
            if (venue == null) return NotFound();

            return View(venue);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var hasBookings = await _context.Bookings.AnyAsync(b => b.VenueId == id);
            if (hasBookings)
            {
                TempData["ErrorMessage"] = "Cannot delete this venue because it has existing bookings.";
                return RedirectToAction(nameof(Index));
            }

            var venue = await _context.Venues.FindAsync(id);
            if (venue != null)
            {
                // Delete image from blob if exists
                if (!string.IsNullOrEmpty(venue.ImageURL))
                {
                    await DeleteBlobAsync(venue.ImageURL);
                }

                _context.Venues.Remove(venue);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool VenueExists(int id)
        {
            return _context.Venues.Any(e => e.Id == id);
        }

        private async Task<string> UploadImageToBlobStorageAsync(IFormFile file)
        {
            var credential = new StorageSharedKeyCredential(_storageAccountName, _storageAccountKey);
            var blobServiceClient = new BlobServiceClient(new Uri($"https://{_storageAccountName}.blob.core.windows.net"), credential);
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);

            await containerClient.CreateIfNotExistsAsync();

            var blobName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var blobClient = containerClient.GetBlobClient(blobName);

            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, overwrite: true);
            }

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
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1),
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasToken = sasBuilder.ToSasQueryParameters(
                new StorageSharedKeyCredential(_storageAccountName, _storageAccountKey)).ToString();

            return $"{blobClient.Uri}?{sasToken}";
        }
    }
}
