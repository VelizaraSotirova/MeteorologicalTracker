using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MeteoTracker.Services;
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
        public async Task<IActionResult> UploadCsv(IFormFile file, [FromQuery] string stationName)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Файлът не е избран или е празен.");

            if (string.IsNullOrWhiteSpace(stationName))
                return BadRequest("Моля, въведете име на станцията.");

            // Създаваме временно копие на файла на сървъра, за да го прочетем
            var tempPath = Path.GetTempFileName();
            using (var stream = new FileStream(tempPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            try
            {
                int importedRows = await _importService.ImportCsvAsync(tempPath, stationName);
                return Ok(new { Message = $"Успешен импорт! Записани са {importedRows} нови измервания за станция '{stationName}'." });
            }
            finally
            {
                // Изтриваме временния файл
                if (System.IO.File.Exists(tempPath))
                    System.IO.File.Delete(tempPath);
            }
        }
    }
}