using Newtonsoft.Json.Linq;
using WebAPI.Domain.Entities;
using WebAPI.RequestResponse;

namespace WebAPI.Infrastructure.Repositories
{
    public interface IReportRepository
    {
        Task<ReportResponse<JObject>> GetReport(ReportRequest request);
        Task<ReportResponse<string>> GetReportData(ReportRequests request);
        //void Add(User user);
    }
}