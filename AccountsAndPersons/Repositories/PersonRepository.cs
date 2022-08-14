using Dapper;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccountsAndPersons
{
    public class PersonRepository
    {
        private readonly DapperDbContext _dapperDbContext;

        public PersonRepository(DapperDbContext dapperDbContext) : base()
        {
            this._dapperDbContext = dapperDbContext;
        }


        public async Task<IEnumerable<Person>> GetAllAsync()
        {
            var sql = "SELECT * FROM public.\"Person\"";
            using (var connection = _dapperDbContext.CreateConnection())
            {
                connection.Open();
                var result = await connection.QueryAsync<Person>(sql);
                return result;
            }
        }

        public async Task<IEnumerable<Person>> GetByAccountAsync(string accountId)
        {
            var query = "SELECT * FROM public.\"Person\" WHERE \"AccountId\" = @accountId";
            using (var connection = _dapperDbContext.CreateConnection())
            {
                connection.Open();
                var result = await connection.QueryAsync<Person>(query, new { accountId });
                return result;
            }
        }

        public async Task Insert(Person entity)
        {
            var query = "INSERT INTO \"Person\" " +
                "(\"AccountId\",\"FirstName\",\"LastName\",\"Email\",\"Description\",\"Phone\",\"DateOfBirth\")" +
                "VALUES (@AccountId,@FirstName,@LastName,@Email,@Description,@Phone,@DateOfBirth)";

            var parameters = new DynamicParameters();

            parameters.Add("AccountId", entity.AccountId);
            parameters.Add("FirstName", entity.FirstName);
            parameters.Add("LastName", entity.LastName);
            parameters.Add("Email", entity.Email);
            parameters.Add("Description", entity.Description);
            parameters.Add("Phone", entity.Phone);
            parameters.Add("DateOfBirth", entity.DateOfBirth);

            using (var connection = _dapperDbContext.CreateConnection())
            {
                connection.Open();
                await connection.ExecuteAsync(query, parameters);
            }
        }

        public async Task Update(Person entity)
        {
            var updateQuery = "UPDATE public.\"Person\" SET \"AccountId\"=@AccountId, \"FirstName\"=@FirstName, \"LastName\"=@LastName," +
                " \"Email\"=@Email, \"Description\"=@Description, \"Phone\"=@Phone, \"DateOfBirth\"=@DateOfBirth" +
                " WHERE \"PersonId\"=@Id";

            var parameters = new DynamicParameters();

            parameters.Add("AccountId", entity.AccountId);
            parameters.Add("FirstName", entity.FirstName);
            parameters.Add("LastName", entity.LastName);
            parameters.Add("Email", entity.Email);
            parameters.Add("Description", entity.Description);
            parameters.Add("Phone", entity.Phone);
            parameters.Add("DateOfBirth", entity.DateOfBirth);
            parameters.Add("Id", entity.PersonId);

            using (var connection = _dapperDbContext.CreateConnection())
            {
                connection.Open();
                await connection.ExecuteAsync(updateQuery, parameters);
            }
        }

        public async Task Delete(Person entity)
        {

            using (var connection = _dapperDbContext.CreateConnection())
            {
                connection.Open();
                var deleteQuery = "DELETE FROM \"Person\" WHERE \"PersonId\" = @PersonId";
                await connection.ExecuteAsync(deleteQuery, new { PersonId = entity.PersonId });

            }
        }
    }
}
