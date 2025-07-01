using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using UglyToad.PdfPig;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class pdfController : ControllerBase
    {

[HttpPost("extract-text")]
        public IActionResult ExtractTextFromPdf([FromForm] Files File)
        {
            if (File.File == null || File.File.Length == 0)
                return BadRequest("No file uploaded");

            try
            {
                using var stream = File.File.OpenReadStream();
                using var pdf = PdfDocument.Open(stream);
                var text = string.Join("\n", pdf.GetPages().Select(p => p.Text));

                return Ok(text);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error reading PDF: {ex.Message}");
            }
        }
    
    }
}
