using Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Api.Services
{
    public class JWTService
    {
        private readonly IConfiguration _config;
        private readonly SymmetricSecurityKey _jwtKey;
        private readonly UserManager<User> _userManager;

        public JWTService(IConfiguration config, UserManager<User> userManager)
        {
            _config = config;
            _userManager = userManager;
            _jwtKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT:Key"]));
        }

        public async Task<string> CreateJWTAsync(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var userClaims = new List<Claim>();

            // Add non-null claims
            if (!string.IsNullOrEmpty(user.Id))
            {
                userClaims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id));
            }

            if (!string.IsNullOrEmpty(user.Email))
            {
                userClaims.Add(new Claim(ClaimTypes.Email, user.Email)); // Add email claim
            }

            if (!string.IsNullOrEmpty(user.FirstName))
            {
                userClaims.Add(new Claim(ClaimTypes.GivenName, user.FirstName));
            }

            if (!string.IsNullOrEmpty(user.LastName))
            {
                userClaims.Add(new Claim(ClaimTypes.Surname, user.LastName));
            }

            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                userClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            var credentials = new SigningCredentials(_jwtKey, SecurityAlgorithms.HmacSha512); // Use HmacSha512
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(userClaims),
                Expires = DateTime.UtcNow.AddDays(int.Parse(_config["JWT:ExpiresInDays"])),
                SigningCredentials = credentials,
                Issuer = _config["JWT:Issuer"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var jwt = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(jwt);
        }
    }

}
