using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using WebApplication1.Models;
using OpenAI;
using OpenAI.Embeddings;
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
                // ✨ chunk the text
                var chunks = SafeSplitBySentences(text);

                return Ok(chunks); // يرجعهم كـ array of strings
                                   // return Ok(text);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error reading PDF: {ex.Message}");
            }
        }


        public static List<string> SafeSplitBySentences(string text, int maxChunkLength = 300)
        {
            // 1️⃣ كلمات خاصة فيها نقاط، مش نهاية جملة
            var exceptions = new[] { "ASP.NET", "U.S.A.", "Dr.", "Mr.", "Mrs.", "gmail.com", "linkedin.com", "github.com", "www." };

            // 2️⃣ نستبدل النقطة بعلامة مؤقتة [dot]
            foreach (var word in exceptions)
            {
                text = text.Replace(word, word.Replace(".", "[dot]"));
            }

            // 3️⃣ نكسر النص على حسب النقطة أو السطر الجديد
            var sentences = text.Split(new[] { '.', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var chunks = new List<string>();
            var currentChunk = new StringBuilder();

            // 4️⃣ نجمع الجمل مع بعض في chunks صغيرة
            foreach (var rawSentence in sentences)
            {


                var sentence = rawSentence.Replace("[dot]", ".").Trim();
                sentence = Regex.Replace(sentence, @"\s+", " ").Trim();
                if ((currentChunk.Length + sentence.Length + 1) < maxChunkLength)
                {
                    currentChunk.Append(sentence + ". ");
                }
                else
                {
                    chunks.Add(currentChunk.ToString().Trim());
                    currentChunk.Clear();
                    currentChunk.Append(sentence + ". ");
                }
            }

            // 5️⃣ نضيف آخر chunk لو باقي
            if (currentChunk.Length > 0)
                chunks.Add(currentChunk.ToString().Trim());

            return chunks.Where(chunk => !string.IsNullOrWhiteSpace(chunk))  // نحذف الفراغات والجمل الفاضية
            .ToList();
        }




        private async Task<List<double[]>> GetEmbeddingsFromOpenAI(List<string> chunks, string apiKey)
        {
             var client = new OpenAIClient(apiKey);
            var embeddings = new List<double[]>();

            foreach (var chunk in chunks)
            {
               var result = await client.EmbeddingsEndpoint.CreateEmbeddingAsync(
            input: chunk,
            model: "text-embedding-3-small"
        );

        var embedding = result.Data[0].Embedding.ToArray();

        embeddings.Add(embedding);
            }

            return embeddings;
        }

    }
}
