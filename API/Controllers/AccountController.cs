using System.Security.Cryptography;
using System.Text;
using System.Text.Unicode;
using API.Controllers;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API;

public class AccountController:BaseApiController
{
    private readonly DataContext _dataContext;
    private readonly ITokenService _tokenService;
    private readonly IMapper _mapper;

    public AccountController(DataContext dataContext, ITokenService tokenService, IMapper mapper)
    {
        _dataContext = dataContext;
        _tokenService = tokenService;
        _mapper = mapper;
    }

    [HttpPost("register")]
    public async Task<ActionResult<UserDTO>> Register(RegisterDTO registerDTO)
    {
        if(await UserExist(registerDTO.Username)) return BadRequest("Username is taken");

        var user = _mapper.Map<AppUser>(registerDTO);

        using var hmac = new HMACSHA512();
        
        user.UserName = registerDTO.Username.ToLower();
        user.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDTO.Password));
        user.PasswordSalt = hmac.Key;
        
        _dataContext.Add(user);
        await _dataContext.SaveChangesAsync();
        return new UserDTO(){
            
            Username = registerDTO.Username,
            Token = _tokenService.CreateToken(user),
            KnownAs = user.KnownAs
        };
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserDTO>> Login(LoginDTO loginDTO){
        var user  = await _dataContext.Users
            .Include(p => p.Photos)
            .SingleOrDefaultAsync(x => x.UserName == loginDTO.Username);

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
            Token = _tokenService.CreateToken(user),
            PhotoUrl = user.Photos.FirstOrDefault(x => x.IsMain).Url,
            KnownAs = user.KnownAs
        };
    }

    private async Task<bool> UserExist(string userName){
        return await _dataContext.Users.AnyAsync(u => u.UserName == userName);
    }
}
