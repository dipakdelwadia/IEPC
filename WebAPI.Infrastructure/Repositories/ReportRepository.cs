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
            //Dashboard -------------------------------------------------------------

            { "Inspection Planning",
                @"exec SP_FormDataIEPCMultiChart_v1 
                @type = 1001,
                @year = 2025,
                @isOldcount = 1,
                @username = 'IEPC-SMIT@indoglobus.com',
                @clientId='IEPC-SMIT',
                @IsRefresh=0"
            },

            //Maps -------------------------------------------------------------

            { "Facility Map",
                @"exec Sp_FormdataIEPCFacilityMap 
                'IEPC-SMIT@indoglobus.com', 
                'IEPC-SMIT'"
            },

            { "PV Asset Map",
                @"exec SP_FormDataIEPCAssetMap_v5 
                'IEPC-SMIT@indoglobus.com', 
                'IEPC-SMIT'"
            },

            { "PP Asset Map",
                @"exec SP_FormDataIEPCPPAssetMap 
                'IEPC-SMIT@indoglobus.com', 
                'IEPC-SMIT'"
            },

            { "TK Asset Map",
                @"exec SP_FormDataIEPCTKAssetMap 
                'IEPC-SMIT@indoglobus.com', 
                'IEPC-SMIT'"
            },

            { "PRD Asset Map",
                @"exec SP_FormDataIEPCPRDAssetMap 
                'IEPC-SMIT@indoglobus.com', 
                'IEPC-SMIT'"
            },

            // Audit Reports -------------------------------------------------------------
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
            },

            //Advanced Analytics -----------------------------------------------------------
            { "PV CML Algorithm",
                @"exec dbo.SP_IEPCCMLTableAlgorithm_v4 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @username = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-PVINVEAL',
                    @IsRefresh = 0"
            },

            { "PV Asset Algorithm",
                @"exec dbo.SP_IEPCAssetTableAlgorithm_v4 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @username = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-PVINVEAL',
                    @IsRefresh = 0"
            },

            { "PV Scheduler Algorithm",
                @"exec dbo.SP_IEPCPVSchedulerAlgorithm_v4 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @username = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-PVINVEAL',
                    @IsRefresh = 0"
            },

            { "PP CML Algorithm",
                @"exec dbo.SP_IEPCPPCMLAlgorithm_v4 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @username = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-PPINVEAL',
                    @IsRefresh = 0"
            },

            { "PP Asset Algorithm",
                @"exec dbo.SP_IEPCPPAssetAlgorithm_v4 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @username = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-PPINVEAL',
                    @IsRefresh = 0"
            },

            { "PP Scheduler Algorithm",
                @"exec dbo.SP_IEPCPPSchedulerAlgorithm_v4 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @username = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-PPINVEAL',
                    @IsRefresh = 0"
            },

            { "TK CML Algorithm",
                @"exec dbo.SP_IEPCTKCMLAlgorithm_V4 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @username = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-TKINVEAL',
                    @IsRefresh = 0"
            },

            { "TK Asset Algorithm",
                @"exec dbo.SP_IEPCTKAssetAlgorithm_v4 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @username = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-TKINVEAL',
                    @IsRefresh = 0"
            },

            { "TK Scheduler Algorithm",
                @"exec dbo.SP_IEPCTKSchedulerAlgorithm_v4 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @username = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-TKINVEAL',
                    @IsRefresh = 0"
            },

            { "PRD Asset Algorithm",
                @"exec dbo.Sp_IEPCPRDAssetAlgorithm_V4 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @useremail = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-PRDINVEAL',
                    @IsRefresh = 1"
            },

            //Facility & System -----------------------------------------------------------

            { "Facility",
                @"exec dbo.SP_formdataIEPCFacilityShared 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @useremail = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-FS'"
            },

            { "System List",
                @"exec dbo.SP_formdataIEPCSystemHierarchyShared 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @useremail = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-SHS'"
            },

            //PV Source Data -----------------------------------------------------------

            { "PV Inventory",
                @"exec dbo.SP_formdataIEPCPVInventoryAssetLevelAddEditReport_v1 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @username = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-PVINVEAL'"
            },

            { "PV Design",
                @"exec dbo.SP_FormDataIEPCPVDesignAssetLevel 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @useremail = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-PVDAL'"
            },

            { "PV Operation",
                @"exec dbo.SP_FormDataIEPCPVOperationsAssetLevel 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @useremail = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-PVOPRTN'"
            },

            { "PV Options",
                @"exec dbo.SP_FormDataIEPCPVOptionsAssetLevel 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @useremail = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-OPTION'"
            },

            { "PV Inspection Scheduler",
                @"exec dbo.SP_FormDataIEPCPVInspectionSchedulerChild 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @useremail = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-PVINSC'"
            },

            { "PV Inspections",
                @"exec dbo.SP_formdataIEPCPVInspectionsChildOfAsset 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @useremail = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-PVINSPCD'"
            },

            { "PV Notes & Findings",
                @"exec dbo.SP_FormDataIEPCPVNotesOrFindingsChildofAsset 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @useremail = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-PVNFCOA'"
            },

            { "PV Components",
                @"exec dbo.SP_FormDataIEPCPVComponentChildofAsset 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @useremail = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-PVCCDOA'"
            },

            { "PV CMLs",
                @"exec dbo.SP_FormDataIEPCPVCMLChildofComponent 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @useremail = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-PVCMLCOC'"
            },

            { "PV CML Readings",
                @"exec dbo.SP_formdataIEPCPVCMLReadingsChildofCML 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @useremail = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-PVCMLRCOCML'"
            },

            { "PV CML Readings(Multi-Edit)",
                @"exec "
            },

            //PP Source Data -------------------------------------------------------------

            { "PP Inventory",
            @"exec dbo.SP_formdataIEPCPPInventoryAssetLevelAddEditReport 
                @type = 'selectForGrid', 
                @fdate = null, 
                @tdate = null,  
                @FromNumber = 1,  
                @ToNumber = 10,  
                @SQLSortString = '',  
                @SQLFilterString = '',
                @clientId = 'IEPC-SMIT',
                @username = 'IEPC-SMIT@indoglobus.com',
                @FormCode = 'IEPC-PPINVEAL'"
            },

            { "PP Design",
                @"exec dbo.SP_FormDataIEPCPPDesignAssetLevel 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @useremail = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-PPDAL'"
            },

            { "PP Operation",
                @"exec dbo.SP_formdataIEPCPPOperationAssetLevel 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @username = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-PPOPRTN'"
            },

            { "PP Options",
                @"exec dbo.SP_FormDataIEPCPPOptionsAssetLevel 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @useremail = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-PPOPTION'"
            },

            { "PP Inspection Scheduler",
                @"exec dbo.SP_FormDataIEPCPPInspectionSchedulerChild 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @useremail = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-PPINSC'"
            },

            { "PP Inspections",
                @"exec dbo.SP_formdataIEPCPPInspectionsChildOfAsset 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @useremail = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-PPINSPCD'"
            },

            { "PP Notes & Findings",
                @"exec dbo.SP_FormDataIEPCPPNotesOrFindingsChildofAsset 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @useremail = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-PPNFCOA'"
            },

            { "PP Components",
                @"exec dbo.SP_FormDataIEPCPPComponentChildofAsset 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @useremail = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-PPCCDOA'"
            },

            { "PP CMLs",
                @"exec dbo.SP_FormDataIEPCPPCMLChildofComponent 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @useremail = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-PPCMLCOC'"
            },

            { "PP CML Readings",
                @"exec dbo.SP_formdataIEPCPPCMLReadingsChildofCML 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @useremail = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-PPCMLRCOCML'"
            },

            //TK Source Data -------------------------------------------------------------

            { "TK Inventory",
                @"exec dbo.SP_formdataIEPCTKInventoryAssetLevelAddEditReport 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @username = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-TKINVEAL'"
            },

            { "TK Design",
                @"exec dbo.SP_FormDataIEPCTKDesignAssetLevel 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @useremail = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-TKDAL'"
            },

            { "TK Operation",
                @"exec dbo.SP_formdataIEPCTKOperationAssetLevel 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @username = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-TKOPRTN'"
            },

            { "TK Options",
                @"exec dbo.SP_FormDataIEPCTKOptionsAssetLevel 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @useremail = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-TKOPTION'"
            },

            { "TK Inspection Scheduler",
                @"exec dbo.SP_FormDataIEPCTKInspectionSchedulerChild 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @useremail = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-TKINSC'"
            },

            { "TK Inspections",
                @"exec dbo.SP_formdataIEPCTKInspectionsChildofAsset 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @useremail = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-TKINSPCD'"
            },

            { "TK Notes & Findings",
                @"exec dbo.SP_FormDataIEPCTKNotesOrFindingsChildofAsset 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @useremail = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-TKNFCOA'"
            },

            { "TK Components",
                @"exec dbo.SP_FormDataIEPCTKComponentChildofAsset 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @useremail = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-TKCCDOA'"
            },

            { "TK CMLs",
                @"exec dbo.SP_FormDataIEPCTKCMLChildofComponent 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @useremail = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-TKCMLCOC'"
            },

            { "TK CML Readings",
                @"exec dbo.SP_formdataIEPCTKCMLReadingsChildofCML 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @useremail = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-TKCMLRCOCML'"
            },

             //PRD Source Data -------------------------------------------------------------

            { "PRD Inventory",
                @"exec dbo.SP_formdataIEPCPRDInventoryAssetLevelAddEditReport 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @username = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-PRDINVEAL'"
            },

            { "PRD Options",
                @"exec dbo.SP_FormDataIEPCPRDOptionsAssetLevel 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @useremail = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-PRDOA'"
            },

            { "PRD Inspections",
                @"exec dbo.SP_formdataIEPCPRDInspectionsChildOfAsset 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @useremail = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-PRDINSPCD'"
            },

            { "PRD Notes & Findings",
                @"exec dbo.SP_FormDataIEPCPRDNotesOrFindingsChildofAsset 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @useremail = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-PRDNFCOA'"
            },


             //Attachments -------------------------------------------------------------

            { "Facility Attachments",
                @"exec dbo.SP_formDataIEPCFacilityAttachments 
                    @type = 'SelectRecordsByFacility', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '', 
                    @value1 = '0', 
                    @clientId = 'IEPC-SMIT', 
                    @value2 = '0'"
            },

            { "PV Attachments",
                @"exec dbo.SP_formDataPVAttachmentsChildofAsset_v2 
                    @value1 = 'selectByAssetIdentifierAndAttachmentType',
                    @clientId = 'IEPC-SMIT',
                    @value2 = '0',
                    @value3 = '0', 
                    @FromNumber = 1, 
                    @ToNumber = 10, 
                    @SQLSortString = '', 
                    @SQLFilterString = ''"
            },

            { "PP Attachments",
                @"exec dbo.SP_formDataPPAttachmentsChildofAsset_v2 
                    @value1 = 'selectByAssetIdentifierAndAttachmentType',
                    @clientId = 'IEPC-SMIT',
                    @value2 = '0',
                    @value3 = '0', 
                    @FromNumber = 1,   
                    @ToNumber = 10, 
                    @SQLSortString = '', 
                    @SQLFilterString = ''"
            },

            { "PRD Attachments",
                @"exec dbo.SP_formDataPRDAttachments 
                    @type = 'selectByAssetIdentifierAndAttachmentType',  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '', 
                    @value2 = '0', 
                    @value3 = '0', 
                    @clientId = 'IEPC-SMIT'"
            },

            { "TK Attachments",
                @"exec dbo.SP_formDataTKAttachmentsChildofAsset_v2 
                    @value1 = 'selectByAssetIdentifierAndAttachmentType',
                    @clientId = 'IEPC-SMIT',
                    @value2 = '0',
                    @value3 = '0', 
                    @FromNumber = 1,   
                    @ToNumber = 10, 
                    @SQLSortString = '', 
                    @SQLFilterString = ''"
            },

            { "Attachments Report",
                @"exec dbo.SP_FormDataIEPCAttachmentsReport_v2 
                    @AssetType = 1001,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = ''"
            },


             //Configurations -------------------------------------------------------------

            { "System Dropdown",
                @"exec dbo.SP_FormDataIEPCSystem 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = '',
                    @clientId = 'IEPC-SMIT',
                    @useremail = 'IEPC-SMIT@indoglobus.com',
                    @FormCode = 'IEPC-SYS'"
            },

            { "PV Material Reference",
                @"exec dbo.SP_FormDataIEPCMaterialAllowableStressLookupNew 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = ''"
            },

            { "PP Material Reference",
                @"exec dbo.SP_FormDataIEPCPPMaterialAllowableStressLookupNew 
                    @type = 'selectForGrid', 
                    @fdate = null, 
                    @tdate = null,  
                    @FromNumber = 1,  
                    @ToNumber = 10,  
                    @SQLSortString = '',  
                    @SQLFilterString = ''"
            },

            { "TK Material Reference",
                @"exec dbo.SP_FormDataIEPCTKMaterialAllowableStressLookupNew 
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
                @"exec "
            },

            //Bulk Import -------------------------------------------------------------

            { "Upload Data From Excel",
                @"exec "
            },
        };

    }
}
