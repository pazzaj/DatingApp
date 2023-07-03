using System.Security.Cryptography;
using System.Text;
using System.Text.Unicode;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly ITokenService _tokenService;
        private readonly DataContext _context;
        public ITokenService TokenService { get; }
        public AccountController(DataContext context, ITokenService tokenService)
        {
            _tokenService = tokenService;
            _context = context;
        }

        [HttpPost("register")] // api/account/register
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
        {
            //Check if user exists
            if(await UserExists(registerDto.Username)) return BadRequest("Username already exists");

            //HMACSHA512 Will be the passowrd salt, "using" will dispose of the object once we're done with it, garbage collection can normally handle this but if we know we don't need it then we might as well dispose of it ourselves
            //HMACSHA515 dervies from a class that implements IDisposable, that lets us use the "using" keyword to dispose of it after we're done with it
            using var hmac = new HMACSHA512();            

            //Save username in lower to make it like for like when comparing
            var user = new AppUser
            {
                UserName = registerDto.Username.ToLower(),
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)),
                PasswordSalt = hmac.Key
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return new UserDto
            {
                Username = user.UserName,
                Token = _tokenService.CreateToken(user)
            };

        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            var user = await _context.Users
            .Include(p => p.Photos)
            .SingleOrDefaultAsync(x => x.UserName == loginDto.Username);
        
            if(user == null) return Unauthorized();
            
            using var hmac = new HMACSHA512(user.PasswordSalt);

            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

            for (int i = 0; i < computedHash.Length; i++)
            {
                if(computedHash[i] != user.PasswordHash[i]) return Unauthorized("Invalid Password");
            }        

             return new UserDto
            {
                Username = user.UserName,
                Token = _tokenService.CreateToken(user),
                PhotoUrl = user.Photos.FirstOrDefault(x => x.IsMain)?.Url
            };
        }

        private async Task<bool> UserExists(string username)
        {
            //the x refers to a single entity or "user" in that table
            //compare tolower against tolower as we don't know what casing the user will use
            return await _context.Users.AnyAsync(x => x.UserName.ToLower() == username.ToLower());
        }
    }
}