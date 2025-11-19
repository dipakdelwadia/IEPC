using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;

namespace WebAPI.Infrastructure.Repositories
{
    public class JsonResultWithCount
    {
        public string Json { get; set; } = string.Empty;
        public int TableCount { get; set; }
    }

    public class DBService
    {
        private readonly IConfiguration _configuration;

        public DBService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<DataSet> ExecuteStoredProcedureAsync(string spName, object parameters)
        {
            string connectionStringKey = "ConnectionStrings:DefaultConnection";

            using (var connection = new SqlConnection(_configuration[connectionStringKey]))
            using (var cmd = new SqlCommand(spName, connection))
            using (var adapter = new SqlDataAdapter(cmd))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                // Add parameters dynamically
                if (parameters != null)
                {
                    foreach (var prop in parameters.GetType().GetProperties())
                    {
                        var value = prop.GetValue(parameters);

                        
                        if (value is JObject jObject)
                            value = jObject.ToString();
                        else if (value is JArray jArray)
                            value = jArray.ToString();

                        cmd.Parameters.AddWithValue("@" + prop.Name, value ?? DBNull.Value);
                    }
                }

                var ds = new DataSet();
                await connection.OpenAsync();
                adapter.Fill(ds);
                return ds;
            }
        }

        public async Task<DataSet> ExecuteStoredProcedureAsyncWithClientId(string spName, object parameters)
        {
            string clientId = parameters?
                .GetType()
                .GetProperty("ClientId")?
                .GetValue(parameters)?
                .ToString() ?? string.Empty;

            string connectionStringKey = clientId switch
            {
                "CERS" => "ConnectionStrings:IEPC-CERS",
                "HFSI" => "ConnectionStrings:IEPC-HFSI",
                "CRTN" => "ConnectionStrings:IEPC-CRTN",
                "MTDR" => "ConnectionStrings:IEPC-MTDR",
                "SMIT" => "ConnectionStrings:IEPC-SMIT",
                "PDCE" => "ConnectionStrings:IEPC-PDCE",
                "FUJI" => "ConnectionStrings:IEPC-FUJI",
                _ => "ConnectionStrings:DefaultConnection"
            };

            using (var connection = new SqlConnection(_configuration[connectionStringKey]))
            using (var cmd = new SqlCommand(spName, connection))
            using (var adapter = new SqlDataAdapter(cmd))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                // Add parameters dynamically
                if (parameters != null)
                {
                    foreach (var prop in parameters.GetType().GetProperties())
                    {
                        var value = prop.GetValue(parameters);

                        if (value is JObject jObject)
                            value = jObject.ToString();
                        else if (value is JArray jArray)
                            value = jArray.ToString();

                        cmd.Parameters.AddWithValue("@" + prop.Name, value ?? DBNull.Value);
                    }
                }

                var ds = new DataSet();
                await connection.OpenAsync();
                adapter.Fill(ds);
                return ds;
            }
        }

        public async Task<DataSet> ExecuteRawSqlWithClientId(string sqlText, string clientId)
        {
            string connectionStringKey = clientId switch
            {
                "IEPC-CERS" => "ConnectionStrings:IEPC-CERS",
                "IEPC-HFSI" => "ConnectionStrings:IEPC-HFSI",
                "IEPC-CRTN" => "ConnectionStrings:IEPC-CRTN",
                "IEPC-MTDR" => "ConnectionStrings:IEPC-MTDR",
                "IEPC-SMIT" => "ConnectionStrings:IEPC-SMIT",
                "IEPC-PDCE" => "ConnectionStrings:IEPC-PDCE",
                "IEPC-FUJI" => "ConnectionStrings:IEPC-FUJI",
                _ => "ConnectionStrings:DefaultConnection"
            };

            using var connection = new SqlConnection(_configuration[connectionStringKey]);
            using var cmd = new SqlCommand(sqlText, connection);
            using var adapter = new SqlDataAdapter(cmd);

            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 0;

            var ds = new DataSet();
            await connection.OpenAsync();
            adapter.Fill(ds);
            return ds;
        }

    }
}