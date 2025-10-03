using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;

namespace WebAPI.RequestResponse
{
    public class ReportRequest
    {

        [Required]
        public string ClientId { get; set; } = "";

        [Required]
        public string TableName { get; set; } = "";

        public string? PageSize { get; set; } = null;
       
        public string? PageNo { get; set; } = null;

    }
}
