using Azure;
using Dapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebAPI.Domain.Entities;
using WebAPI.RequestResponse;

namespace WebAPI.Infrastructure.Repositories
{
    public class ReportRepository : IReportRepository
    {
        //private readonly AppDbContext _context;
        private readonly IDbConnection _db;
        private readonly DBService _dbService;

        public ReportRepository(IDbConnection db, DBService dbService)
        {
            _db = db;
            _dbService = dbService;
        }

        public async Task<ReportResponse<JObject>> GetReport(ReportRequest request)
        {
            ReportResponse<JObject> response = new();

            try
            {
                var dt = await _dbService.ExecuteStoredProcedureAsyncWithClientId("SP_GetReportByTableName", new
                {
                    ClientId = request.ClientId,
                    TableName = request.TableName,
                    PageSize = "",
                    PageNo = ""
                });

                string json = JsonConvert.SerializeObject(dt);

                response.Status = "Success";

                response.Payload = JObject.Parse(json);
                response.PayloadCount = dt.Tables.Count;
            }
            catch (Exception ex)
            {
                response.Status = "Error";
                response.Message = $"Unexpected error: {ex.Message}";
            }

            return (response);
        }


    }
}
