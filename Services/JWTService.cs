using Api.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Api.Services
{
    public class JWTService
    {
        private readonly IConfiguration _config;
        private readonly SymmetricSecurityKey _jwKey;

        public JWTService(IConfiguration config)
        {
            _config = config;
            var random = new RNGCryptoServiceProvider();
            var keyBytes = new byte[64]; // 512 bits
            random.GetBytes(keyBytes);
            var base64Key = Convert.ToBase64String(keyBytes);

            _jwKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(base64Key));
        }
        public string CreateJWT(User user) 
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

            var credentials = new SigningCredentials(_jwKey, SecurityAlgorithms.HmacSha512Signature);
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
