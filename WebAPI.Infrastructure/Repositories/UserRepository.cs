using Azure.Core;
using Dapper;
using System.Data;
using WebAPI.Domain.Entities;

namespace WebAPI.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        //private readonly AppDbContext _context;
        private readonly IDbConnection _db;
        private readonly DBService _dbService;

        public UserRepository(IDbConnection db, DBService dbService)
        {
            _db = db;
            _dbService = dbService;
        }

        public async Task<User?> GetByUsername1(string username)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Username", username);

            return await _db.QuerySingleOrDefaultAsync<User>(
                "GetUserByUserName",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            //return _context.Users.FirstOrDefault(u => u.Username == username);
        }
        public async Task<User?> GetByUsername(string username, string clientId = null)
        {
            var dt = await _dbService.ExecuteStoredProcedureAsync("GetUserByUserName", new
            {
                ClientId = clientId,
                Username = username
            });
            try
            {
                if (dt?.Tables[0] != null)
                {
                    var user = new User()
                    {
                        UserID = dt.Tables[0].Rows[0]["UserID"].ToString(),
                        Password = dt.Tables[0].Rows[0]["Password"].ToString(),
                        ClientId = dt.Tables[0].Rows[0]["ClientID"].ToString()
                    };
                    return user;
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
           

        }

    }
}