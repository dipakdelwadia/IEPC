using Azure;
using Dapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebAPI.Domain.Entities;
using WebAPI.RequestResponse;
using System.Security.Claims;


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

        public async Task<ReportResponse<string>> GetReportData(ReportRequests request, string userId)
        {
            var response = new ReportResponse<string>();

            try
            {
                //var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                string clientId = "IEPC-"+request.ClientId;
                string username = userId;// GetUserNamebyClientId(request.ClientId); //"IEPC-SMIT@indoglobus.com";
                string formCode = GetFormCodeByReportName(request.ReportName);

                string sqlText = _reportSqlMapping[request.ReportName](clientId, username, formCode);

                // Query DB
                var ds = await _dbService.ExecuteRawSqlWithClientId(sqlText, clientId);

                // FIXED DOWNLOAD PATH
                string outputDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Download");

                if (!Directory.Exists(outputDir))
                    Directory.CreateDirectory(outputDir);

                string filePath = Path.Combine(outputDir, "IEPCReport.json");

                // Write JSON to file
                using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var sw = new StreamWriter(fs))
                using (var writer = new JsonTextWriter(sw))
                {
                    writer.Formatting = Formatting.None;
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(writer, ds);
                }

                response.Payload = filePath.Replace("\\", "/");
                response.Status = "Success";
                response.PayloadCount = ds.Tables.Count;
                response.Message = "Json file successfully created";
            }
            catch (Exception ex)
            {
                response.Status = "Error";
                response.Message = $"Unexpected error: {ex.Message}";
            }

            return response;
        }

        private readonly Dictionary<string, Func<string, string, string, string>> _reportSqlMapping = new()
        {
            //Dashboard -------------------------------------------------------------

             { "Inspection Planning", (clientId, username, formCode) =>
                $@"exec SP_FormDataIEPCMultiChart_v1 
                @type = 1001,
                @year = 2025,
                @isOldcount = 1,
                @username = '{username}',
                @clientId = '{clientId}',
                @IsRefresh = 0"
            },

            //Maps -------------------------------------------------------------

            { "Facility Map", (clientId, username, formCode) =>
                $@"exec Sp_FormdataIEPCFacilityMap '{username}', '{clientId}'"
            },

            { "PV Asset Map", (clientId, username, formCode) =>
                $@"exec SP_FormDataIEPCAssetMap_v5 '{username}', '{clientId}'"
            },

            { "PP Asset Map", (clientId, username, formCode) =>
                $@"exec SP_FormDataIEPCPPAssetMap '{username}', '{clientId}'"
            },

            { "TK Asset Map", (clientId, username, formCode) =>
                $@"exec SP_FormDataIEPCTKAssetMap '{username}', '{clientId}'"
            },

            { "PRD Asset Map", (clientId, username, formCode) =>
                $@"exec SP_FormDataIEPCPRDAssetMap '{username}', '{clientId}'"
            },

            // Audit Reports -------------------------------------------------------------
            { "PV Audit Report",
                (clientId, username, formCode) => $@"
                exec dbo.SP_IEPCAssetCMLAlgoParentChild_V4 
                @type = 'Select', 
                @fdate = null, 
                @tdate = null, 
                @FromNumber = 1,  
                @ToNumber = 100000,  
                @SQLSortString = '', 
                @SQLFilterString = '',
                @username = '{username}',
                @clientId = '{clientId}',
                @FormCode = '{formCode}',
                @value8 = '',
                @Id = '',
                @IsRefresh = 0"
            },

            { "PV CML Audit Report",
                (clientId, username, formCode) => $@"
                exec dbo.SP_IEPCCMLTableAlgorithm_v4 
                @type = 'selectForGrid',
                @fdate = null,
                @tdate = null,
                @FromNumber = 1,
                @ToNumber = 100000,
                @SQLSortString = '',
                @SQLFilterString = '',
                @clientId = '{clientId}',
                @username = '{username}',
                @FormCode = '{formCode}',
                @IsRefresh = 0"
            },

            { "PV Scheduler Report",
                (clientId, username, formCode) => $@"
                exec dbo.SP_IEPCPVSchedulerReport_v4 
                @type = 'selectForGrid',
                @fdate = null,
                @tdate = null,
                @FromNumber = 1,
                @ToNumber = 100000,
                @SQLSortString = '',
                @SQLFilterString = '',
                @clientId = '{clientId}',
                @username = '{username}',
                @FormCode = '{formCode}',
                @IsRefresh = 0"
            },

            { "PV Findings Report",
                (clientId, username, formCode) => $@"
                exec dbo.Sp_IEPCPVNotesAndFindingAuditReport_v4 
                @type = 'selectForGrid', 
                @fdate = null, 
                @tdate = null,  
                @FromNumber = 1,  
                @ToNumber = 100000,  
                @SQLSortString = '',  
                @SQLFilterString = '',
                @clientId = '{clientId}',
                @useremail = '{username}',
                @FormCode = '{formCode}',
                @IsRefresh = 1"
            },

            { "PP Audit Report",
                (clientId, username, formCode) => $@"
                exec dbo.SP_IEPCPPAuditReport_v4 
                @type = 'selectForGrid', 
                @fdate = null, 
                @tdate = null,  
                @FromNumber = 1,  
                @ToNumber = 100000,  
                @SQLSortString = '',  
                @SQLFilterString = '',
                @clientId = '{clientId}',
                @username = '{username}',
                @FormCode = '{formCode}',
                @value8 = '',
                @Id = '',
                @IsRefresh = 0"
            },

            { "PP CML Audit Report",
                (clientId, username, formCode) => $@"
                exec dbo.SP_IEPCPPCMLAuditReport_v4 
                @type = 'selectForGrid', 
                @fdate = null, 
                @tdate = null,  
                @FromNumber = 1,  
                @ToNumber = 100000,  
                @SQLSortString = '',  
                @SQLFilterString = '',
                @clientId = '{clientId}',
                @username = '{username}',
                @FormCode = '{formCode}',
                @IsRefresh = 0"
            },

            { "PP Scheduler Report",
                (clientId, username, formCode) => $@"
                exec dbo.SP_IEPCPPSchedulerReport_v4 
                @type = 'selectForGrid', 
                @fdate = null, 
                @tdate = null,  
                @FromNumber = 1,  
                @ToNumber = 100000,  
                @SQLSortString = '',  
                @SQLFilterString = '',
                @clientId = '{clientId}',
                @username = '{username}',
                @FormCode = '{formCode}',
                @IsRefresh = 0"
            },

            { "PP Findings Report",
                (clientId, username, formCode) => $@"
                exec dbo.Sp_IEPCPPNotesAndFindingAuditReport_v4 
                @type = 'selectForGrid', 
                @fdate = null, 
                @tdate = null,  
                @FromNumber = 1,  
                @ToNumber = 100000,  
                @SQLSortString = '',  
                @SQLFilterString = '',
                @clientId = '{clientId}',
                @useremail = '{username}',
                @FormCode = '{formCode}',
                @IsRefresh = 0"
            },

            { "TK Audit Report",
                (clientId, username, formCode) => $@"
                exec dbo.SP_IEPCTKAuditReport_v4 
                @type = 'selectForGrid', 
                @fdate = null, 
                @tdate = null,  
                @FromNumber = 1,  
                @ToNumber = 100000,  
                @SQLSortString = '',  
                @SQLFilterString = '',
                @clientId = '{clientId}',
                @username = '{username}',
                @FormCode = '{formCode}',
                @value8 = '',
                @Id = '',
                @IsRefresh = 0"
            },

            { "TK CML Audit Report",
                (clientId, username, formCode) => $@"
                exec dbo.SP_IEPCTKCMLAlgorithm_v4 
                @type = 'selectForGrid', 
                @fdate = null, 
                @tdate = null,  
                @FromNumber = 1,  
                @ToNumber = 100000,  
                @SQLSortString = '',  
                @SQLFilterString = '',
                @clientId = '{clientId}',
                @username = '{username}',
                @FormCode = '{formCode}',
                @IsRefresh = 0"
            },

            { "TK Scheduler Report",
                (clientId, username, formCode) => $@"
                exec dbo.SP_IEPCTKSchedulerReport_v4 
                @type = 'selectForGrid', 
                @fdate = null, 
                @tdate = null,  
                @FromNumber = 1,  
                @ToNumber = 100000,  
                @SQLSortString = '',  
                @SQLFilterString = '',
                @clientId = '{clientId}',
                @username = '{username}',
                @FormCode = '{formCode}',
                @IsRefresh = 0"
            },

            { "TK Findings Report",
                (clientId, username, formCode) => $@"
                exec dbo.Sp_IEPCTKNotesAndFindingAuditReport_v4 
                @type = 'selectForGrid', 
                @fdate = null, 
                @tdate = null,  
                @FromNumber = 1,  
                @ToNumber = 100000,  
                @SQLSortString = '',  
                @SQLFilterString = '',
                @clientId = '{clientId}',
                @useremail = '{username}',
                @FormCode = '{formCode}',
                @IsRefresh = 0"
            },

            { "PRD Audit Report",
                (clientId, username, formCode) => $@"
                exec dbo.Sp_IEPCPRDAuditReport_v4 
                @type = 'selectForGrid', 
                @fdate = null, 
                @tdate = null,  
                @FromNumber = 1,  
                @ToNumber = 100000,  
                @SQLSortString = '',  
                @SQLFilterString = '',
                @clientId = '{clientId}',
                @useremail = '{username}',
                @FormCode = '{formCode}',
                @value8 = '',
                @IsRefresh = 0"
            },

            { "PRD Findings Report",
                (clientId, username, formCode) => $@"
                exec dbo.Sp_IEPCPRDNotesAndFindingAuditReport_v4 
                @type = 'selectForGrid', 
                @fdate = null, 
                @tdate = null,  
                @FromNumber = 1,  
                @ToNumber = 100000,  
                @SQLSortString = '',  
                @SQLFilterString = '',
                @clientId = '{clientId}',
                @useremail = '{username}',
                @FormCode = '{formCode}',
                @IsRefresh = 0"
            },

            //Advanced Analytics -----------------------------------------------------------
            { "PV CML Algorithm", (clientId, username, formCode) =>
                $@"exec dbo.SP_IEPCCMLTableAlgorithm_v4 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{clientId}',
                    @username = '{username}',
                    @FormCode = '{formCode}',
                    @IsRefresh = 0"
            },

            { "PV Asset Algorithm", (clientId, username, formCode) =>
                $@"exec dbo.SP_IEPCAssetTableAlgorithm_v4 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{clientId}',
                    @username = '{username}',
                    @FormCode = '{formCode}',
                    @IsRefresh = 0"
            },

            { "PV Scheduler Algorithm", (clientId, username, formCode) =>
                $@"exec dbo.SP_IEPCPVSchedulerAlgorithm_v4 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{clientId}',
                    @username = '{username}',
                    @FormCode = '{formCode}',
                    @IsRefresh = 0"
            },

            { "PP CML Algorithm", (clientId, username, formCode) =>
                $@"exec dbo.SP_IEPCPPCMLAlgorithm_v4 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{clientId}',
                    @username = '{username}',
                    @FormCode = '{formCode}',
                    @IsRefresh = 0"
            },

            { "PP Asset Algorithm", (clientId, username, formCode) =>
                $@"exec dbo.SP_IEPCPPAssetAlgorithm_v4 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{clientId}',
                    @username = '{username}',
                    @FormCode = '{formCode}',
                    @IsRefresh = 0"
            },

            { "PP Scheduler Algorithm", (clientId, username, formCode) =>
                $@"exec dbo.SP_IEPCPPSchedulerAlgorithm_v4 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{clientId}',
                    @username = '{username}',
                    @FormCode = '{formCode}',
                    @IsRefresh = 0"
            },

            { "TK CML Algorithm", (clientId, username, formCode) =>
                $@"exec dbo.SP_IEPCTKCMLAlgorithm_V4 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{clientId}',
                    @username = '{username}',
                    @FormCode = '{formCode}',
                    @IsRefresh = 0"
            },

            { "TK Asset Algorithm", (clientId, username, formCode) =>
                $@"exec dbo.SP_IEPCTKAssetAlgorithm_v4 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{clientId}',
                    @username = '{username}',
                    @FormCode = '{formCode}',
                    @IsRefresh = 0"
            },

            { "TK Scheduler Algorithm", (clientId, username, formCode) =>
                $@"exec dbo.SP_IEPCTKSchedulerAlgorithm_v4 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{clientId}',
                    @username = '{username}',
                    @FormCode = '{formCode}',
                    @IsRefresh = 0"
            },

            { "PRD Asset Algorithm", (clientId, username, formCode) =>
                $@"exec dbo.Sp_IEPCPRDAssetAlgorithm_V4 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{clientId}',
                    @useremail = '{username}',
                    @FormCode = '{formCode}',
                    @IsRefresh = 1"
            },


            //Facility & System -----------------------------------------------------------

            { "Facility", (clientId, username, formCode) =>
                $@"exec dbo.SP_formdataIEPCFacilityShared 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{clientId}',
                    @useremail = '{username}',
                    @FormCode = '{formCode}'"
            },

            { "System List", (clientId, username, formCode) =>
                $@"exec dbo.SP_formdataIEPCSystemHierarchyShared 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{clientId}',
                    @useremail = '{username}',
                    @FormCode = '{formCode}'"
            },


            //PV Source Data -----------------------------------------------------------

            {
                "PV Inventory",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_formdataIEPCPVInventoryAssetLevelAddEditReport_v1 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{{clientId}}',
                    @username = '{{username}}',
                    @FormCode = '{{formCode}}'"
            },

            {
                "PV Design",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_FormDataIEPCPVDesignAssetLevel 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{{clientId}}',
                    @useremail = '{{username}}',
                    @FormCode = '{{formCode}}'"
            },

            {
                "PV Operation",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_FormDataIEPCPVOperationsAssetLevel 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{{clientId}}',
                    @useremail = '{{username}}',
                    @FormCode = '{{formCode}}'"
            },

            {
                "PV Options",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_FormDataIEPCPVOptionsAssetLevel 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{{clientId}}',
                    @useremail = '{{username}}',
                    @FormCode = '{{formCode}}'"
            },

            {
                "PV Inspection Scheduler",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_FormDataIEPCPVInspectionSchedulerChild 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{{clientId}}',
                    @useremail = '{{username}}',
                    @FormCode = '{{formCode}}'"
            },

            {
                "PV Inspections",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_formdataIEPCPVInspectionsChildOfAsset 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{{clientId}}',
                    @useremail = '{{username}}',
                    @FormCode = '{{formCode}}'"
            },

            {
                "PV Notes & Findings",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_FormDataIEPCPVNotesOrFindingsChildofAsset 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{{clientId}}',
                    @useremail = '{{username}}',
                    @FormCode = '{{formCode}}'"
            },

            {
                "PV Components",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_FormDataIEPCPVComponentChildofAsset 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{{clientId}}',
                    @useremail = '{{username}}',
                    @FormCode = '{{formCode}}'"
            },

            {
                "PV CMLs",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_FormDataIEPCPVCMLChildofComponent 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{{clientId}}',
                    @useremail = '{{username}}',
                    @FormCode = '{{formCode}}'"
            },

            {
                "PV CML Readings",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_formdataIEPCPVCMLReadingsChildofCML 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{{clientId}}',
                    @useremail = '{{username}}',
                    @FormCode = '{{formCode}}'"
            },

            { "PV CML Readings(Multi-Edit)",
                 (clientId, username, formCode) => $@"exec "
            },

            //PP Source Data -------------------------------------------------------------

           {
                "PP Inventory",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_formdataIEPCPPInventoryAssetLevelAddEditReport 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{clientId}',
                    @username = '{username}',
                    @FormCode = '{formCode}'"
            },

            {
                "PP Design",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_FormDataIEPCPPDesignAssetLevel 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{clientId}',
                    @useremail = '{username}',
                    @FormCode = '{formCode}'"
            },

            {
                "PP Operation",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_formdataIEPCPPOperationAssetLevel 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{clientId}',
                    @username = '{username}',
                    @FormCode = '{formCode}'"
            },

            {
                "PP Options",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_FormDataIEPCPPOptionsAssetLevel 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{clientId}',
                    @useremail = '{username}',
                    @FormCode = '{formCode}'"
            },

            {
                "PP Inspection Scheduler",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_FormDataIEPCPPInspectionSchedulerChild 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{clientId}',
                    @useremail = '{username}',
                    @FormCode = '{formCode}'"
            },

            {
                "PP Inspections",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_formdataIEPCPPInspectionsChildOfAsset 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{clientId}',
                    @useremail = '{username}',
                    @FormCode = '{formCode}'"
            },

            {
                "PP Notes & Findings",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_FormDataIEPCPPNotesOrFindingsChildofAsset 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{clientId}',
                    @useremail = '{username}',
                    @FormCode = '{formCode}'"
            },

            {
                "PP Components",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_FormDataIEPCPPComponentChildofAsset 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{clientId}',
                    @useremail = '{username}',
                    @FormCode = '{formCode}'"
            },

            {
                "PP CMLs",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_FormDataIEPCPPCMLChildofComponent 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{clientId}',
                    @useremail = '{username}',
                    @FormCode = '{formCode}'"
            },

            {
                "PP CML Readings",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_formdataIEPCPPCMLReadingsChildofCML 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{clientId}',
                    @useremail = '{username}',
                    @FormCode = '{formCode}'"
            },

            //TK Source Data -------------------------------------------------------------

           
            { 
                "TK Inventory",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_formdataIEPCTKInventoryAssetLevelAddEditReport 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{clientId}',
                    @username = '{username}',
                    @FormCode = '{formCode}'"
            },

            {
                "TK Design",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_FormDataIEPCTKDesignAssetLevel 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{clientId}',
                    @useremail = '{username}',
                    @FormCode = '{formCode}'"
            },

            {
                "TK Operation",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_formdataIEPCTKOperationAssetLevel 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{clientId}',
                    @username = '{username}',
                    @FormCode = '{formCode}'"
            },

            {
                "TK Options",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_FormDataIEPCTKOptionsAssetLevel 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{clientId}',
                    @useremail = '{username}',
                    @FormCode = '{formCode}'"
            },

            {
                "TK Inspection Scheduler",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_FormDataIEPCTKInspectionSchedulerChild 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{clientId}',
                    @useremail = '{username}',
                    @FormCode = '{formCode}'"
            },

            {
                "TK Inspections",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_formdataIEPCTKInspectionsChildofAsset 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{clientId}',
                    @useremail = '{username}',
                    @FormCode = '{formCode}'"
            },

            {
                "TK Notes & Findings",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_FormDataIEPCTKNotesOrFindingsChildofAsset 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{clientId}',
                    @useremail = '{username}',
                    @FormCode = '{formCode}'"
            },

            {
                "TK Components",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_FormDataIEPCTKComponentChildofAsset 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{clientId}',
                    @useremail = '{username}',
                    @FormCode = '{formCode}'"
            },

            {
                "TK CMLs",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_FormDataIEPCTKCMLChildofComponent 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{clientId}',
                    @useremail = '{username}',
                    @FormCode = '{formCode}'"
            },

            {
                "TK CML Readings",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_formdataIEPCTKCMLReadingsChildofCML 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = '{clientId}',
                    @useremail = '{username}',
                    @FormCode = '{formCode}'"
            },


             //PRD Source Data -------------------------------------------------------------

            {
                "PRD Inventory",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_formdataIEPCPRDInventoryAssetLevelAddEditReport 
                        @type = 'selectForGrid', 
                        @fdate = null, 
                        @tdate = null,  
                        @FromNumber = 1,  
                        @ToNumber = 10,  
                        @SQLSortString = '',  
                        @SQLFilterString = '',
                        @clientId = '{clientId}',
                        @username = '{username}',
                        @FormCode = '{formCode}'"
            },

            {
                "PRD Options",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_FormDataIEPCPRDOptionsAssetLevel 
                        @type = 'selectForGrid', 
                        @fdate = null, 
                        @tdate = null,  
                        @FromNumber = 1,  
                        @ToNumber = 10,  
                        @SQLSortString = '',  
                        @SQLFilterString = '',
                        @clientId = '{clientId}',
                        @useremail = '{username}',
                        @FormCode = '{formCode}'"
            },

            {
                "PRD Inspections",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_formdataIEPCPRDInspectionsChildOfAsset 
                        @type = 'selectForGrid', 
                        @fdate = null, 
                        @tdate = null,  
                        @FromNumber = 1,  
                        @ToNumber = 10,  
                        @SQLSortString = '',  
                        @SQLFilterString = '',
                        @clientId = '{clientId}',
                        @useremail = '{username}',
                        @FormCode = '{formCode}'"
            },

            {
                "PRD Notes & Findings",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_FormDataIEPCPRDNotesOrFindingsChildofAsset 
                        @type = 'selectForGrid', 
                        @fdate = null, 
                        @tdate = null,  
                        @FromNumber = 1,  
                        @ToNumber = 10,  
                        @SQLSortString = '',  
                        @SQLFilterString = '',
                        @clientId = '{clientId}',
                        @useremail = '{username}',
                        @FormCode = '{formCode}'"
            },


             //Attachments -------------------------------------------------------------

            {
                "Facility Attachments",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_formDataIEPCFacilityAttachments 
                        @type = 'SelectRecordsByFacility', 
                        @fdate = null, 
                        @tdate = null,  
                        @FromNumber = 1,  
                        @ToNumber = 10,  
                        @SQLSortString = '',  
                        @SQLFilterString = '', 
                        @value1 = '0', 
                        @clientId = '{clientId}', 
                        @value2 = '0'"
            },

            {
                "PV Attachments",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_formDataPVAttachmentsChildofAsset_v2 
                        @value1 = 'selectByAssetIdentifierAndAttachmentType',
                        @clientId = '{clientId}',
                        @value2 = '0',
                        @value3 = '0', 
                        @FromNumber = 1, 
                        @ToNumber = 10, 
                        @SQLSortString = '', 
                        @SQLFilterString = ''"
            },

            {
                "PP Attachments",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_formDataPPAttachmentsChildofAsset_v2 
                        @value1 = 'selectByAssetIdentifierAndAttachmentType',
                        @clientId = '{clientId}',
                        @value2 = '0',
                        @value3 = '0', 
                        @FromNumber = 1,   
                        @ToNumber = 10, 
                        @SQLSortString = '', 
                        @SQLFilterString = ''"
            },

            {
                "PRD Attachments",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_formDataPRDAttachments 
                        @type = 'selectByAssetIdentifierAndAttachmentType',  
                        @FromNumber = 1,  
                        @ToNumber = 10,  
                        @SQLSortString = '',  
                        @SQLFilterString = '', 
                        @value2 = '0', 
                        @value3 = '0', 
                        @clientId = '{clientId}'"
            },

            {
                "TK Attachments",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_formDataTKAttachmentsChildofAsset_v2 
                        @value1 = 'selectByAssetIdentifierAndAttachmentType',
                        @clientId = '{clientId}',
                        @value2 = '0',
                        @value3 = '0', 
                        @FromNumber = 1,   
                        @ToNumber = 10, 
                        @SQLSortString = '', 
                        @SQLFilterString = ''"
            },

            {
                "Attachments Report",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_FormDataIEPCAttachmentsReport_v2 
                        @AssetType = 1001,  
                        @FromNumber = 1,  
                        @ToNumber = 10,  
                        @SQLSortString = '',  
                        @SQLFilterString = ''"
            },



             //Configurations -------------------------------------------------------------

            {
                "System Dropdown",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_FormDataIEPCSystem 
                        @type = 'selectForGrid', 
                        @fdate = null, 
                        @tdate = null,  
                        @FromNumber = 1,  
                        @ToNumber = 10,  
                        @SQLSortString = '',  
                        @SQLFilterString = '',
                        @clientId = '{clientId}',
                        @useremail = '{username}',
                        @FormCode = '{formCode}'"
            },

            {
                "PV Material Reference",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_FormDataIEPCMaterialAllowableStressLookupNew 
                        @type = 'selectForGrid', 
                        @fdate = null, 
                        @tdate = null,  
                        @FromNumber = 1,  
                        @ToNumber = 10,  
                        @SQLSortString = '',  
                        @SQLFilterString = ''"
            },

            {
                "PP Material Reference",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_FormDataIEPCPPMaterialAllowableStressLookupNew 
                        @type = 'selectForGrid', 
                        @fdate = null, 
                        @tdate = null,  
                        @FromNumber = 1,  
                        @ToNumber = 10,  
                        @SQLSortString = '',  
                        @SQLFilterString = ''"
            },

            {
                "TK Material Reference",
                (clientId, username, formCode) => $@"
                    exec dbo.SP_FormDataIEPCTKMaterialAllowableStressLookupNew 
                        @type = 'selectForGrid', 
                        @fdate = null, 
                        @tdate = null,  
                        @FromNumber = 1,  
                        @ToNumber = 10,  
                        @SQLSortString = '',  
                        @SQLFilterString = ''"
            },


            //Bulk Update -------------------------------------------------------------

            { "Bulk Update",
                 (clientId, username, formCode) => $@"exec "
            },

            //Bulk Import -------------------------------------------------------------

            { "Upload Data From Excel",
                (clientId, username, formCode) => $@"exec "
            },
        };

        private static string GetFormCodeByReportName(string reportName)
        {
            return reportName switch
            {
                "PV Audit Report" => "IEPC-PVINVEAL",
                "PV CML Audit Report" => "IEPC-PVCML",
                "PV Scheduler Report" => "IEPC-PVSCHED",
                "PV Findings Report" => "IEPC-PVFIN",
                "PP Audit Report" => "IEPC-PPAUDIT",
                "PP CML Audit Report" => "IEPC-PPCML",
                "PP Scheduler Report" => "IEPC-PPSCHED",
                "PP Findings Report" => "IEPC-PPFIN",
                "TK Audit Report" => "IEPC-TKAUDIT",
                "TK CML Audit Report" => "IEPC-TKCML",
                "TK Scheduler Report" => "IEPC-TKSCHED",
                "TK Findings Report" => "IEPC-TKFIN",
                "PRD Audit Report" => "IEPC-PRDAUDIT",
                "PRD Findings Report" => "IEPC-PRDFIN",
                _ => "IEPC-UNKNOWN"
            };
        }
       
    }
}
