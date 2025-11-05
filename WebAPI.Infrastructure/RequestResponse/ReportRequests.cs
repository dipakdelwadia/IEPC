using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;

namespace WebAPI.RequestResponse
{
    public class ReportRequests
    {

        [Required]
        public string ClientId { get; set; } = "";

        [Required]
        public string ReportName { get; set; } = "";

    }
}
