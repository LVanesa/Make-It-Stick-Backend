using Make_It_Stick_Backend.Data;
using Make_It_Stick_Backend.Models.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Google.Cloud.Vision.V1;
using OpenCvSharp;




namespace Make_It_Stick_Backend.Controllers
{


    //[Authorize]
    //[ApiController]
    [Route("api/[controller]")]
    public class OcrController : BaseController
    {
        private readonly IWebHostEnvironment _env;
        public OcrController(AppDbContext context, IWebHostEnvironment env) : base(context)
        {
            _env = env;
        }

        private byte[] EnhanceImage(byte[] imageBytes)
        {
            using var src = Cv2.ImDecode(imageBytes, ImreadModes.Color);
            if (src.Empty()) return imageBytes;

            // Convert to Grayscale
            var gray = new Mat();
            Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);

            // Increase contrast using adaptive threshold
            var contrast = new Mat();
            Cv2.AdaptiveThreshold(gray, contrast, 255, AdaptiveThresholdTypes.MeanC, ThresholdTypes.Binary, 15, 10);

            // Optionally: sharpen
            var sharpened = new Mat();
            var kernel = new float[,]
            {
                { -1, -1, -1 },
                { -1,  9, -1 },
                { -1, -1, -1 }
            };
            Cv2.Filter2D(contrast, sharpened, MatType.CV_8U, InputArray.Create(kernel));

            // Encode to JPEG
            return sharpened.ToBytes(".jpg");
        }


        [HttpPost]
        public async Task<IActionResult> ExtractText([FromBody] OcrRequest request)
        {
            if (string.IsNullOrEmpty(request.Base64Image))
                return BadRequest("Image data is missing.");

            try
            {
                // Setăm calea absolută către fișierul JSON
                string credentialsPath = Path.Combine(_env.ContentRootPath, "Config", "google_vision_credentials.json");
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialsPath);

                // Scoatem prefixul dacă există
                var base64 = request.Base64Image.Contains(",")
                    ? request.Base64Image.Split(',')[1]
                    : request.Base64Image;

                byte[] imageBytes = Convert.FromBase64String(base64);
                var image = Image.FromBytes(imageBytes);

                var client = await ImageAnnotatorClient.CreateAsync();
                var result = await client.DetectDocumentTextAsync(image);

                return Ok(new { text = result?.Text ?? "No text found." });
            }
            catch (Exception ex)
            {
                Console.WriteLine("OCR ERROR: " + ex.Message);
                return StatusCode(500, new { error = "Something went wrong with OCR", details = ex.Message });
            }
        }

        [HttpPost("enhance")]
        public IActionResult Enhance([FromBody] OcrRequest request)
        {
            if (string.IsNullOrEmpty(request.Base64Image))
                return BadRequest("Image data is missing.");

            try
            {
                var base64 = request.Base64Image.Contains(",")
                    ? request.Base64Image.Split(',')[1]
                    : request.Base64Image;

                var imageBytes = Convert.FromBase64String(base64);
                var enhancedBytes = EnhanceImage(imageBytes);

                return Ok(new
                {
                    enhancedBase64 = $"data:image/jpeg;base64,{Convert.ToBase64String(enhancedBytes)}"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Enhancement ERROR: " + ex.Message);
                return StatusCode(500, new { error = "Failed to enhance image", details = ex.Message });
            }
        }



    }
}
