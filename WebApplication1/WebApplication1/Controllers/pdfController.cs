﻿using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using WebApplication1.Models;
using OpenAI;
using OpenAI.Embeddings;
using System.Text.Json;
namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class pdfController : ControllerBase
    {


        private readonly IConfiguration _config;

        public pdfController(IConfiguration config)
        {
            _config = config;
        }

        [HttpPost("extract-text")]
        public async Task<IActionResult> ExtractTextFromPdfAsync([FromForm] Files File)
        {
            if (File.File == null || File.File.Length == 0)
                return BadRequest("No file uploaded");

            try
            {
                using var stream = File.File.OpenReadStream();
                using var pdf = PdfDocument.Open(stream);
                var text = string.Join("\n", pdf.GetPages().Select(p => p.Text));
                //  chunk the text
                var chunks = SafeSplitBySentences(text);
                // جلب مفتاح API من الإعدادات
       // var apiKey = _config["Gemini:ApiKey"];

        // 🧬 تحويل كل chunk إلى Embedding
        var embeddings = new List<float[]>();
        foreach (var chunk in chunks)
        {
            var embedding = await  GetEmbeddingFromPythonAsync(chunk);
            embeddings.Add(embedding.ToArray());
        }

        // ✨ نرجع الـ chunks مع الـ embeddings
        var result = chunks.Select((chunk, index) => new
        {
            Text = chunk,
            Embedding = embeddings[index]
        });
                return Ok(result); 
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




        // 📌 ده بيرسل الـ chunk لبايثون اللي بيرجع الـ embedding
private async Task<List<float>> GetEmbeddingFromPythonAsync(string chunk)
{
    using var client = new HttpClient();

    var json = JsonSerializer.Serialize(new { text = chunk });

    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var response = await client.PostAsync("http://localhost:5000/get-embedding", content);

    if (!response.IsSuccessStatusCode)
        throw new Exception("Python server error");

    var result = await response.Content.ReadAsStringAsync();

    var doc = JsonDocument.Parse(result);
    var values = doc.RootElement
        .GetProperty("embedding")
        .EnumerateArray()
        .Select(x => (float)x.GetDouble())
        .ToList();

    return values;
}


    }
}
