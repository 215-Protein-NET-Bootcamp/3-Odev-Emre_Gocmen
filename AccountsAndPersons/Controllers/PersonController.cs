using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AccountsAndPersons
{
    [Route("TrackToBeFitSwg/[controller]")]
    [ApiController]
    public class PersonController : ControllerBase
    {
        PersonRepository personRepository;

        public PersonController(PersonRepository personRepository)
        {
            this.personRepository = personRepository;
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            var result = new BaseResponse<IEnumerable<Person>>(await personRepository.GetAllAsync());

            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }


        [HttpGet("GetByAccount")]
        [Authorize]
        public async Task<IActionResult> GetByAccount()
        {
            var userIdentify = (User.Identity as ClaimsIdentity).FindFirst(ClaimTypes.NameIdentifier).Value;

            var result = new BaseResponse<IEnumerable<Person>>(await personRepository.GetByAccountAsync(userIdentify));

            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create(Person person)
        {
            await personRepository.Insert(person);

            return Ok();
        }

        [HttpPut("Update")]
        public async Task<IActionResult> Update(Person person)
        {
            await personRepository.Update(person);

            return Ok();
        }

        [HttpDelete("Delete")]
        public async Task<IActionResult> Delete(Person person)
        {
            await personRepository.Delete(person);

            return Ok();
        }
    }
}
