using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Application.Services;
using WebAPI.Infrastructure.Repositories;
using WebAPI.RequestResponse;
using Newtonsoft.Json.Linq;


namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requires JWT token
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
            return Ok(result);
        }

    }
}
