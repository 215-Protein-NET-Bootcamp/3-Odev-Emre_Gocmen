using Dapper;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace AccountsAndPersons
{
    public class AccountRepository 
    {
        protected readonly DapperDbContext _dapperDbContext;
        private readonly UnitOfWork _unitOfWork;
        private readonly byte[] _secret;
        private readonly JwtConfig _jwtConfig;

        public AccountRepository(DapperDbContext dbContext, UnitOfWork unitOfWork, IOptionsMonitor<JwtConfig> jwtConfig)
        {
            this._dapperDbContext = dbContext;
            this._unitOfWork = unitOfWork;
            this._jwtConfig = jwtConfig.CurrentValue;
            this._secret = Encoding.ASCII.GetBytes(_jwtConfig.Secret);
        }

        public async Task<IEnumerable<Account>> GetAllAsync()
        {
            var sql = "SELECT * FROM public.\"Account\"";
            using (var connection = _dapperDbContext.CreateConnection())
            {
                connection.Open();
                var result = await connection.QueryAsync<Account>(sql);
                return result;

            }
        }

        public virtual async Task<Account> GetByIdAsync(int entityId)
        {
            //return await entities.FindAsync(entityId);
            var query = "SELECT * FROM public.\"Account\" WHERE \"AccountId\" = @Id";

            var parameters = new DynamicParameters();
            parameters.Add("Id", entityId);

            using (var connection = _dapperDbContext.CreateConnection())
            {
                connection.Open();
                var result = await connection.QueryFirstAsync<Account>(query, parameters);
                return result;
            }
        }

        public async Task<Account> InsertAsync(Account entity)
        {
            var query = "INSERT INTO public.\"Account\" (\"UserName\", \"Password\", \"Name\", \"Email\", \"Role\", \"LastActivity\" ) " +
                "VALUES (@UserName, @Password, @Name, @Email, @Role, @LastActivity)";


            entity.LastActivity = DateTime.UtcNow;

            var parameters = new DynamicParameters();
            parameters.Add("UserName", entity.UserName, DbType.String);
            parameters.Add("Password", entity.Password, DbType.String);
            parameters.Add("Name", entity.Name, DbType.String);
            parameters.Add("Email", entity.Email, DbType.String);
            parameters.Add("Role", entity.Role, DbType.String);
            parameters.Add("LastActivity", entity.LastActivity, DbType.DateTime);

            using (var connection = _dapperDbContext.CreateConnection())
            {
                connection.Open();
                await connection.ExecuteAsync(query, parameters);
            }

            return entity;
        }

        public async Task<Person> Update(Person entity)
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
                var result = await connection.QueryFirstAsync(updateQuery, parameters);
                return result;
            }
        }

        public async Task<Person> Delete(Person entity)
        {

            using (var connection = _dapperDbContext.CreateConnection())
            {
                connection.Open();
                var deleteQuery = "DELETE FROM \"Person\" WHERE \"PersonId\" = @PersonId";
                var result = await connection.QueryFirstAsync(deleteQuery, new { PersonId = entity.PersonId });
                return result;
            }
        }



        public async void UpdateLastActivity(Account entity)
        {
            var updateQuery = "UPDATE public.\"Account\" SET \"LastActivity\"=@LastActivity WHERE \"AccountId\"=@Id";

            var parameters = new DynamicParameters();
            parameters.Add("LastActivity", entity.LastActivity);
            parameters.Add("Id", entity.AccountId);

            using (var connection = _dapperDbContext.CreateConnection())
            {
                connection.Open();
                await connection.ExecuteAsync(updateQuery, parameters);
            }
        }

        public async Task<Account> GetByIdAsyncToken(int entityId, bool hasToken)
        {
            var query = "SELECT * FROM public.\"Account\" WHERE \"Id\" = @Id";

            var parameters = new DynamicParameters();
            parameters.Add("Id", entityId);

            using (var connection = _dapperDbContext.CreateConnection())
            {
                connection.Open();
                var result = await connection.QueryFirstAsync<Account>(query, parameters);
                return result;
            }
        }


        public async Task<Account> ValidateCredentialsAsync(TokenRequest loginResource)
        {
            var query = "SELECT * FROM public.\"Account\" WHERE \"UserName\" = @UserName";

            Account result;

            using (var connection = _dapperDbContext.CreateConnection())
            {
                connection.Open();
                result = await connection.QueryFirstAsync<Account>(query, new { loginResource.UserName });
            }

            var accountStored = result;

            if (accountStored is null)
                return null;
            else
            {
                // Validate credential
                bool isValid = accountStored.Password.CheckingPassword(loginResource.Password);
                if (isValid)
                    return accountStored;
                else
                    return null;
            }
        }

        public async Task<BaseResponse<TokenResponse>> GenerateTokensAsync(TokenRequest tokenRequest, DateTime now, string userAgent)
        {
            try
            {
                // Validate Login-request
                var tempAccount = await ValidateCredentialsAsync(tokenRequest);
                if (tempAccount is null)
                    return new BaseResponse<TokenResponse>("Token_Invalid");

                // Get access-token
                var accessToken = GenerateAccessToken(tempAccount, now);

                // Set Last-Activity value
                tempAccount.LastActivity = DateTime.UtcNow;
                UpdateLastActivity(tempAccount);

                TokenResponse token = new TokenResponse
                {
                    AccessToken = accessToken,
                    ExpireTime = now.AddMinutes(_jwtConfig.AccessTokenExpiration),
                    Role = tempAccount.Role
                };

                return new BaseResponse<TokenResponse>(token);
            }
            catch (Exception ex)
            {
                throw new MessageResultException("Token_Saving_Error", ex);
            }
        }

        private string GenerateAccessToken(Account account, DateTime now)
        {
            // Get claim value
            Claim[] claims = GetClaim(account);

            var shouldAddAudienceClaim = string.IsNullOrWhiteSpace(claims?.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Aud)?.Value);

            var jwtToken = new JwtSecurityToken(
                _jwtConfig.Issuer,
                shouldAddAudienceClaim ? _jwtConfig.Audience : string.Empty,
                claims,
                expires: now.AddMinutes(_jwtConfig.AccessTokenExpiration),
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(_secret), SecurityAlgorithms.HmacSha256Signature));

            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwtToken);

            return accessToken;
        }

        private static Claim[] GetClaim(Account account)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, account.AccountId.ToString()),
                new Claim(ClaimTypes.Name, account.UserName),
                new Claim(ClaimTypes.Role, account.Role),
                new Claim("AccountId", account.AccountId.ToString()),
            };

            return claims;
        }

        public async Task UpdatePassword(int identifier, string newPassword)
        {
            var updateQuery = "UPDATE public.\"Account\" SET \"Password\"=@Password WHERE \"AccountId\"=@Id";

            var parameters = new DynamicParameters();
            parameters.Add("Password", newPassword);
            parameters.Add("Id", identifier);

            using (var connection = _dapperDbContext.CreateConnection())
            {
                connection.Open();
                await connection.ExecuteAsync(updateQuery, parameters);
            }
        }

    }
}
