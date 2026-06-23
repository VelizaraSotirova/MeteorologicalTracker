using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MeteoTracker.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MeteoTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImportController : ControllerBase
    {
        private readonly DataImportService _importService;

        public ImportController(DataImportService importService)
        {
            _importService = importService;
        }

        [HttpPost("upload-csv")]
        [Authorize(Roles = "Admin")] // Only users with the "Admin" role can access this endpoint
        public async Task<IActionResult> UploadCsv(IFormFile file)
        {
            // Basic web validation: Check if the client actually sent a file
            if (file == null || file.Length == 0)
            {
                return BadRequest("Файлът не е избран или е празен.");
            }

            // Create a temporary file on the server's hard drive to safely read it
            var tempPath = Path.GetTempFileName();

            using (var stream = new FileStream(tempPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            try
            {
                // Pass the path to the temporary file and the ORIGINAL file name (e.g., "petrich 2026.csv") to the service
                int importedRows = await _importService.ImportCsvAsync(tempPath, file.FileName);

                return Ok(new { Message = $"Успешен импорт! Данните от файл '{file.FileName}' бяха обработени. Записани са {importedRows} нови измервания." });
            }
            catch (ArgumentException ex)
            {
                // If the file name is in the wrong format, the service will throw this error and the controller returns it to the client
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                // Protection against unexpected errors during processing
                return StatusCode(500, $"Възникна системна грешка при обработката на файла: {ex.Message}");
            }
            finally
            {
                // The finally block ensures that the temporary file is deleted from the disk after processing,
                // regardless of whether the import was successful or an error occurred.
                // This prevents the server's storage from filling up with temporary files.
                if (System.IO.File.Exists(tempPath))
                {
                    System.IO.File.Delete(tempPath);
                }
            }
        }
    }
}