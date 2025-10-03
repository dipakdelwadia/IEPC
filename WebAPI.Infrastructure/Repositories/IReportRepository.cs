using Newtonsoft.Json.Linq;
using WebAPI.Domain.Entities;
using WebAPI.RequestResponse;

namespace WebAPI.Infrastructure.Repositories
{
    public interface IReportRepository
    {
        Task<ReportResponse<JObject>> GetReport(ReportRequest request);
        //void Add(User user);
    }
}