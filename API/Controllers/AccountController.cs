using System.Security.Cryptography;
using System.Text;
using System.Text.Unicode;
using API.Controllers;
using API.Data;
using API.DTOs;
using API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API;

public class AccountController:BaseApiController
{
    private readonly DataContext _dataContext;

    public AccountController(DataContext dataContext)
    {
        _dataContext = dataContext;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AppUser>> Register(RegisterDTO registerDTO)
    {
        if(await UserExist(registerDTO.Username)) return BadRequest("User already exists with that username!");
        
        using var hmac = new HMACSHA512();
        var user = new AppUser{
            UserName = registerDTO.Username,
            PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDTO.Password)),
            PasswordSalt = hmac.Key
        };
        _dataContext.Add(user);
        await _dataContext.SaveChangesAsync();
        return user;
    }

    private async Task<bool> UserExist(string userName){
        return await _dataContext.Users.AnyAsync(u => u.UserName == userName);
    }
}
