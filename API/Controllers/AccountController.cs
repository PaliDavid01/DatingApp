using System.Security.Cryptography;
using System.Text;
using System.Text.Unicode;
using API.Controllers;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API;

public class AccountController:BaseApiController
{
    private readonly DataContext _dataContext;
    private readonly ITokenService _tokenService;

    public AccountController(DataContext dataContext, ITokenService tokenService)
    {
        _dataContext = dataContext;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<UserDTO>> Register(RegisterDTO registerDTO)
    {
        //if(await UserExist(registerDTO.Username)) 
        //{
        //return BadRequest("User already exists with that username!");
        //}
        using var hmac = new HMACSHA512();
        var user = new AppUser{
            UserName = registerDTO.Username,
            PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDTO.Password)),
            PasswordSalt = hmac.Key
        };
        _dataContext.Add(user);
        await _dataContext.SaveChangesAsync();
        return new UserDTO(){
            Username = registerDTO.Username,
            Token = _tokenService.CreateToken(user)
        };
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserDTO>> Login(LoginDTO loginDTO){
        var user  = await _dataContext.Users.SingleOrDefaultAsync(x => x.UserName == loginDTO.Username);
        
        if(user == null)
        {
             return Unauthorized("User not exists");
        }
        using var hmac = new HMACSHA512(user.PasswordSalt);
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDTO.Password));
        for(int i = 0; i < computedHash.Length;i++){
            if(computedHash[i] != user.PasswordHash[i]){
                return Unauthorized("Invalid password");
            }
        }
        return new UserDTO(){
            Username = loginDTO.Username,
            Token = _tokenService.CreateToken(user) 
        };
    }

    private async Task<bool> UserExist(string userName){
        return await _dataContext.Users.AnyAsync(u => u.UserName == userName);
    }
}
