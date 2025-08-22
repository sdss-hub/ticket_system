using Microsoft.AspNetCore.Mvc;
using SupportTicketSystem.Core.Entities;
using SupportTicketSystem.Core.Interfaces;

namespace SupportTicketSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AttachmentsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _environment;

        public AttachmentsController(IUnitOfWork unitOfWork, IWebHostEnvironment environment)
        {
            _unitOfWork = unitOfWork;
            _environment = environment;
        }

        [HttpPost("upload/{ticketId}")]
        public async Task<ActionResult> UploadFile(int ticketId, IFormFile file, [FromQuery] int userId = 1)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "No file uploaded" });

                // Validate file size (max 10MB)
                if (file.Length > 10 * 1024 * 1024)
                    return BadRequest(new { message = "File size exceeds 10MB limit" });

                // Check if ticket exists
                var ticket = await _unitOfWork.Tickets.GetByIdAsync(ticketId);
                if (ticket == null)
                    return NotFound(new { message = "Ticket not found" });

                // Create unique filename
                var fileExtension = Path.GetExtension(file.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                
                // Create uploads directory
                var uploadsDir = Path.Combine(_environment.WebRootPath ?? "wwwroot", "uploads");
                Directory.CreateDirectory(uploadsDir);
                
                var filePath = Path.Combine(uploadsDir, uniqueFileName);

                // Save file and read into memory for BLOB storage
                byte[] fileData;
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    fileData = stream.ToArray();
                }

                // Also save to file system
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Create attachment record
                var attachment = new Attachment
                {
                    TicketId = ticketId,
                    FileName = uniqueFileName,
                    OriginalFileName = file.FileName,
                    FileSize = file.Length,
                    MimeType = file.ContentType,
                    FilePath = $"/uploads/{uniqueFileName}",
                    FileData = fileData, // Store in BLOB column
                    UploadedById = userId,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Attachments.AddAsync(attachment);
                await _unitOfWork.SaveChangesAsync();

                // Update ticket timestamp
                ticket.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Tickets.Update(ticket);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new
                {
                    message = "File uploaded successfully",
                    attachment = new
                    {
                        attachment.Id,
                        attachment.FileName,
                        attachment.OriginalFileName,
                        attachment.FileSize,
                        attachment.MimeType,
                        attachment.CreatedAt,
                        DownloadUrl = $"/api/attachments/{attachment.Id}/download"
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "File upload failed", error = ex.Message });
            }
        }

        [HttpGet("{id}/download")]
        public async Task<ActionResult> DownloadFile(int id)
        {
            try
            {
                var attachment = await _unitOfWork.Attachments.GetByIdAsync(id);
                if (attachment == null)
                    return NotFound(new { message = "Attachment not found" });

                // Try BLOB data first, then file system
                if (attachment.FileData != null && attachment.FileData.Length > 0)
                {
                    return File(attachment.FileData, attachment.MimeType, attachment.OriginalFileName);
                }
                
                if (!string.IsNullOrEmpty(attachment.FilePath))
                {
                    var fullPath = Path.Combine(_environment.WebRootPath ?? "wwwroot", attachment.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(fullPath))
                    {
                        var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
                        return File(fileStream, attachment.MimeType, attachment.OriginalFileName);
                    }
                }

                return NotFound(new { message = "File not found" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Download failed", error = ex.Message });
            }
        }

        [HttpGet("ticket/{ticketId}")]
        public async Task<ActionResult> GetTicketAttachments(int ticketId)
        {
            try
            {
                var attachments = await _unitOfWork.Attachments.FindAsync(a => a.TicketId == ticketId);
                
                var result = attachments.Select(a => new
                {
                    a.Id,
                    a.FileName,
                    a.OriginalFileName,
                    a.FileSize,
                    a.MimeType,
                    a.CreatedAt,
                    DownloadUrl = $"/api/attachments/{a.Id}/download",
                    HasBlobData = a.FileData != null && a.FileData.Length > 0,
                    HasFileSystemData = !string.IsNullOrEmpty(a.FilePath)
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to retrieve attachments", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAttachment(int id, [FromQuery] int userId = 1)
        {
            try
            {
                var attachment = await _unitOfWork.Attachments.GetByIdAsync(id);
                if (attachment == null)
                    return NotFound(new { message = "Attachment not found" });

                // Delete file from file system if exists
                if (!string.IsNullOrEmpty(attachment.FilePath))
                {
                    var fullPath = Path.Combine(_environment.WebRootPath ?? "wwwroot", attachment.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }

                // Delete from database
                _unitOfWork.Attachments.Remove(attachment);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new { message = "Attachment deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to delete attachment", error = ex.Message });
            }
        }
    }
}
