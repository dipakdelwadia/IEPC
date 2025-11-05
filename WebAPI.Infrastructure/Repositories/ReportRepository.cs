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

        public async Task<ReportResponse<JObject>> GetReportData(ReportRequests request)
        {
            var response = new ReportResponse<JObject>();

            try
            {
                string sqlText = _reportSqlMapping[request.ReportName];

                var ds = await _dbService.ExecuteRawSqlWithClientId(sqlText, request.ClientId);

                string json = JsonConvert.SerializeObject(ds);
                response.Status = "Success";
                response.Payload = JObject.Parse(json);
                response.PayloadCount = ds.Tables.Count;
            }
            catch (Exception ex)
            {
                response.Status = "Error";
                response.Message = $"Unexpected error: {ex.Message}";
            }

            return response;
        }

        private readonly Dictionary<string, string> _reportSqlMapping = new()
        {
            { "PV Audit Report",
                @"exec dbo.SP_IEPCAssetCMLAlgoParentChild_V4 
                @type = 'Select', 
                @fdate = null, 
                @tdate = null, 
                @FromNumber = 1,  
                @ToNumber = 100000,  
                @SQLSortString = '', 
                @SQLFilterString = '',
                @username='IEPC-SMIT@indoglobus.com',
                @clientId='IEPC-SMIT',
                @FormCode='IEPC-PVINVEAL',
                @value8='',
                @Id='',
                @IsRefresh=0"
            },

            { "PV CML Audit Report", 
                @"exec dbo.SP_IEPCCMLTableAlgorithm_v4 
                @type = 'selectForGrid',
                @fdate = null,
                @tdate = null,
                @FromNumber = 1,
                @ToNumber = 100000,
                @SQLSortString = '',
                @SQLFilterString = '',
                @clientId='IEPC-SMIT',
                @username='IEPC-SMIT@indoglobus.com',
                @FormCode='IEPC-PVINVEAL',
                @IsRefresh=0"
            },

            { "PV Scheduler Report", 
                @"exec dbo.SP_IEPCPVSchedulerReport_v4 
                @type = 'selectForGrid',
                @fdate = null,
                @tdate = null,
                @FromNumber = 1,
                @ToNumber = 100000,
                @SQLSortString = '',
                @SQLFilterString = '' ,
                @clientId='IEPC-SMIT',
                @username='IEPC-SMIT@indoglobus.com',
                @FormCode='IEPC-PVINVEAL',@IsRefresh=0"
            },

            { "PV Findings Report",
                @"exec dbo.Sp_IEPCPVNotesAndFindingAuditReport_v4 
                @type = 'selectForGrid', 
                @fdate = null, 
                @tdate = null,  
                @FromNumber = 1,  
                @ToNumber = 100000,  
                @SQLSortString = '',  
                @SQLFilterString = '',
                @clientId = 'IEPC-SMIT',
                @useremail = 'IEPC-SMIT@indoglobus.com',
                @FormCode = 'IEPC-PVINVEAL',
                @IsRefresh = 1"
            },

            { "PP Audit Report",
                @"exec dbo.SP_IEPCPPAuditReport_v4 
                @type = 'selectForGrid', 
                @fdate = null, 
                @tdate = null,  
                @FromNumber = 1,  
                @ToNumber = 100000,  
                @SQLSortString = '',  
                @SQLFilterString = '',
                @clientId = 'IEPC-SMIT',
                @username = 'IEPC-SMIT@indoglobus.com',
                @FormCode = 'IEPC-PPINVEAL',
                @value8 = '',
                @Id = '',
                @IsRefresh = 0"
            },

            { "PP CML Audit Report",
                @"exec dbo.SP_IEPCPPCMLAuditReport_v4 
                @type = 'selectForGrid', 
                @fdate = null, 
                @tdate = null,  
                @FromNumber = 1,  
                @ToNumber = 100000,  
                @SQLSortString = '',  
                @SQLFilterString = '',
                @clientId = 'IEPC-SMIT',
                @username = 'IEPC-SMIT@indoglobus.com',
                @FormCode = 'IEPC-PPINVEAL',
                @IsRefresh = 0"
            },

            { "PP Scheduler Report",
                @"exec dbo.SP_IEPCPPSchedulerReport_v4 
                @type = 'selectForGrid', 
                @fdate = null, 
                @tdate = null,  
                @FromNumber = 1,  
                @ToNumber = 100000,  
                @SQLSortString = '',  
                @SQLFilterString = '',
                @clientId = 'IEPC-SMIT',
                @username = 'IEPC-SMIT@indoglobus.com',
                @FormCode = 'IEPC-PPINVEAL',
                @IsRefresh = 0"
            },

            { "PP Findings Report",
                @"exec dbo.Sp_IEPCPPNotesAndFindingAuditReport_v4 
                @type = 'selectForGrid', 
                @fdate = null, 
                @tdate = null,  
                @FromNumber = 1,  
                @ToNumber = 100000,  
                @SQLSortString = '',  
                @SQLFilterString = '',
                @clientId = 'IEPC-SMIT',
                @useremail = 'IEPC-SMIT@indoglobus.com',
                @FormCode = 'IEPC-PPINVEAL',
                @IsRefresh = 0"
            },

            { "TK Audit Report",
                @"exec dbo.SP_IEPCTKAuditReport_v4 
                @type = 'selectForGrid', 
                @fdate = null, 
                @tdate = null,  
                @FromNumber = 1,  
                @ToNumber = 100000,  
                @SQLSortString = '',  
                @SQLFilterString = '',
                @clientId = 'IEPC-SMIT',
                @username = 'IEPC-SMIT@indoglobus.com',
                @FormCode = 'IEPC-TKINVEAL',
                @value8 = '',
                @Id = '',
                @IsRefresh = 0"
            },

            { "TK CML Audit Report",
                @"exec dbo.SP_IEPCTKCMLAlgorithm_v4 
                @type = 'selectForGrid', 
                @fdate = null, 
                @tdate = null,  
                @FromNumber = 1,  
                @ToNumber = 100000,  
                @SQLSortString = '',  
                @SQLFilterString = '',
                @clientId = 'IEPC-SMIT',
                @username = 'IEPC-SMIT@indoglobus.com',
                @FormCode = 'IEPC-TKINVEAL',
                @IsRefresh = 0"
            },

            { "TK Scheduler Report",
                @"exec dbo.SP_IEPCTKSchedulerReport_v4 
                @type = 'selectForGrid', 
                @fdate = null, 
                @tdate = null,  
                @FromNumber = 1,  
                @ToNumber = 100000,  
                @SQLSortString = '',  
                @SQLFilterString = '',
                @clientId = 'IEPC-SMIT',
                @username = 'IEPC-SMIT@indoglobus.com',
                @FormCode = 'IEPC-TKINVEAL',
                @IsRefresh = 0"
            },

            { "TK Findings Report",
                @"exec dbo.Sp_IEPCTKNotesAndFindingAuditReport_v4 
                @type = 'selectForGrid', 
                @fdate = null, 
                @tdate = null,  
                @FromNumber = 1,  
                @ToNumber = 100000,  
                @SQLSortString = '',  
                @SQLFilterString = '',
                @clientId = 'IEPC-SMIT',
                @useremail = 'IEPC-SMIT@indoglobus.com',
                @FormCode = 'IEPC-TKINVEAL',
                @IsRefresh = 0"
            },

            { "PRD Audit Report",
                @"exec dbo.Sp_IEPCPRDAuditReport_v4 
                @type = 'selectForGrid', 
                @fdate = null, 
                @tdate = null,  
                @FromNumber = 1,  
                @ToNumber = 100000,  
                @SQLSortString = '',  
                @SQLFilterString = '',
                @clientId = 'IEPC-SMIT',
                @useremail = 'IEPC-SMIT@indoglobus.com',
                @FormCode = 'IEPC-PRDINVEAL',
                @value8 = '',
                @IsRefresh = 0"
            },

            { "PRD Findings Report",
                @"exec dbo.Sp_IEPCPRDNotesAndFindingAuditReport_v4 
                @type = 'selectForGrid', 
                @fdate = null, 
                @tdate = null,  
                @FromNumber = 1,  
                @ToNumber = 100000,  
                @SQLSortString = '',  
                @SQLFilterString = '',
                @clientId = 'IEPC-SMIT',
                @useremail = 'IEPC-SMIT@indoglobus.com',
                @FormCode = 'IEPC-PRDINVEAL',
                @IsRefresh = 0"
            }

        };

    }
}
