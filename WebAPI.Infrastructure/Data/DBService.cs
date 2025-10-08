using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.Data;

namespace WebAPI.Infrastructure.Repositories
{
    public class DBService
    {
        private readonly IConfiguration _configuration;

        public DBService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<DataSet> ExecuteStoredProcedureAsync(string spName, object parameters)
        {
            //string clientId = parameters?
            //    .GetType()
            //    .GetProperty("ClientId")?
            //    .GetValue(parameters)?
            //    .ToString() ?? string.Empty;

            string connectionStringKey = "ConnectionStrings:DefaultConnection";

            //switch (clientId)
            //{
            //    case "CERS":
            //        connectionStringKey = "ConnectionStrings:CERS";
            //        break;
            //    case "HFSI":
            //        connectionStringKey = "ConnectionStrings:HFSI";
            //        break;
            //    case "CRTN":
            //        connectionStringKey = "ConnectionStrings:CRTN";
            //        break;
            //    case "MTDR":
            //        connectionStringKey = "ConnectionStrings:MTDR";
            //        break;
            //    case "SMIT":
            //        connectionStringKey = "ConnectionStrings:SMIT";
            //        break;
            //    case "PDCE":
            //        connectionStringKey = "ConnectionStrings:PDCE";
            //        break;
            //    case "FUJI":
            //        connectionStringKey = "ConnectionStrings:FUJI";
            //        break;
            //    default:
            //        connectionStringKey = "ConnectionStrings:DefaultConnection";
            //        break;
            //}

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

                        // ✅ Convert JObject or JArray to JSON string
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

            string connectionStringKey = "ConnectionStrings:DefaultConnection";

            switch (clientId)
            {
                case "CERS":
                    connectionStringKey = "ConnectionStrings:CERS";
                    break;
                case "HFSI":
                    connectionStringKey = "ConnectionStrings:HFSI";
                    break;
                case "CRTN":
                    connectionStringKey = "ConnectionStrings:CRTN";
                    break;
                case "MTDR":
                    connectionStringKey = "ConnectionStrings:MTDR";
                    break;
                case "SMIT":
                    connectionStringKey = "ConnectionStrings:SMIT";
                    break;
                case "PDCE":
                    connectionStringKey = "ConnectionStrings:PDCE";
                    break;
                case "FUJI":
                    connectionStringKey = "ConnectionStrings:FUJI";
                    break;
                default:
                    connectionStringKey = "ConnectionStrings:DefaultConnection";
                    break;
            }

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

                        // ✅ Convert JObject or JArray to JSON string
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
    }
}