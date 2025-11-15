using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using WebAPI.Application.Services;
using WebAPI.Infrastructure.Repositories;
using WebAPI.RequestResponse;


namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize] // Requires JWT token
    public class ReportController : ControllerBase
    {
        private readonly IReportRepository _reportRepository;
        public ReportController(IReportRepository reportRepository)
        {
            _reportRepository = reportRepository;
        }

        [HttpPost("get")]
        public async Task<IActionResult> Get([FromBody] ReportRequest request)
        {
            var result = await _reportRepository.GetReport(request);
            return Ok(result);
        }

        [HttpPost("GetReport")]
        public async Task<IActionResult> GetReport([FromBody] ReportRequests request)
        {
            var result = await _reportRepository.GetReportData(request);
            if (result.Status != "Success")
            {
                return BadRequest(result.Message ?? "Unknown error occurred.");
            }

            string filePath = result.Payload;

            if (string.IsNullOrWhiteSpace(filePath))
            {
                return BadRequest("Repository did not return a valid file path.");
            }

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("File not found. Report generation failed.");
            }

            byte[] bytes = System.IO.File.ReadAllBytes(filePath);
            return File(bytes, "application/json", "IEPCReport.json");

            //return Ok(result);
        }

    }
}
