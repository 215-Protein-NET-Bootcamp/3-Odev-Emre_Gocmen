using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AccountsAndPersons
{
    [ApiController]
    [Route("OfferAble/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly AccountRepository _accountRepository;


        public AccountController(AccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        [Route("GetAll")]
        [HttpGet]
        public virtual async Task<IActionResult> GetAllAsync()
        {
            var tempEntity = await _accountRepository.GetAllAsync();

            var result = new BaseResponse<IEnumerable<Account>>(tempEntity);

            if (!result.Success)
                return BadRequest(result);

            if (result.Response is null)
                return NoContent();

            return Ok(result);
        }


        [HttpGet("GetAccountDetail")]
        [Authorize]
        public async Task<IActionResult> GetAccountDetail()
        {
            var userId = (User.Identity as ClaimsIdentity).FindFirst("AccountId").Value;

            var result = new BaseResponse<Account>(await _accountRepository.GetByIdAsync(int.Parse(userId)));

            if (!result.Success)
                return BadRequest(result);

            if (result.Response is null)
                return NoContent();

            return Ok(result);
        }


        [HttpPost("Create")]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] Account resource)
        {
            var userIdentify = (User.Identity as ClaimsIdentity).FindFirst(ClaimTypes.NameIdentifier).Value;

            var result = new BaseResponse<Account>(await _accountRepository.InsertAsync(resource));

            if (!result.Success)
                return BadRequest(result);

            return StatusCode(201, result);
        }


        [HttpPut("Update")]
        public async Task<IActionResult> Update(Person resource)
        {
            var result = new BaseResponse<Person>(await _accountRepository.Update(resource));
            if (result.Success)
            {
                return Ok();
            }
            return BadRequest(result);
        }

        [HttpDelete("Delete")]
        public async Task<IActionResult> Delete(Person resource)
        {
            var result = new BaseResponse<Person>(await _accountRepository.Delete(resource));
            if (result.Success)
            {
                return Ok();
            }
            return BadRequest(result);
        }

        [HttpPost("Login")]
        public async Task<IActionResult> LoginAsync([FromBody] TokenRequest tokenRequest)
        {
            string userAgent = Request.Headers["User-Agent"].ToString();
            var result = await _accountRepository.GenerateTokensAsync(tokenRequest, DateTime.UtcNow, userAgent);

            if (result.Success)
            {
                Log.Information($"Role {result.Response.Role}: is loged in.");
                return Ok(result);
            }
            return Unauthorized(result);
        }

        [HttpPut("ChangePassword")]
        [Authorize]
        public async Task<IActionResult> UpdatePasswordAsync(int id, [FromBody] UpdatePasswordRequest resource)
        {
            // Check if the id belongs to me
            var identifier = (User.Identity as ClaimsIdentity).FindFirst(ClaimTypes.NameIdentifier).Value;
            if (!identifier.Equals(id.ToString()))
                return BadRequest(new BaseResponse<Account>("Account_Not_Permitted"));

            // Checking duplicate password
            if (resource.OldPassword.Equals(resource.NewPassword))
                return BadRequest(new BaseResponse<Account>("Account_Not_Permitted"));

            var result = new BaseResponse<Account>("No_Result");

            try
            {
                // Validate Id is existent?
                var tempAccount = await _accountRepository.GetByIdAsyncToken(id, hasToken: true);
                if (tempAccount is null)
                    result = new BaseResponse<Account>("Account_NoData");
                if (!tempAccount.Password.CheckingPassword(resource.OldPassword))
                    result = new BaseResponse<Account>("Account_Password_Error");

                // Update infomation
                tempAccount.Password = resource.NewPassword;
                tempAccount.LastActivity = DateTime.UtcNow;

                await _accountRepository.UpdatePassword(int.Parse(identifier), resource.NewPassword);
            }
            catch (Exception ex)
            {
                throw new MessageResultException("Account_Updating_Error", ex);
            }

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
    }
}
