using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AuthenticationApp.Entities;
using AuthenticationApp.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthenticationApp.Controllers
{
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        private IOptions<AppSettings> _secretKey;

        public AuthController( IOptions<AppSettings> secretKey)
        {
            _secretKey = secretKey;
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (model.Username == "Kratos" && model.Password == "Password@123")
            {
                var claims = new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, "Kratos"),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim("Department", "Human Resources"),
                    new Claim("CanCrud", "false")
                };

                var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey.Value.Secret));

                var token = new JwtSecurityToken(
                    issuer: "http://google.com",
                    audience: "http://google.com",
                    expires: DateTime.UtcNow.AddSeconds(20),
                    claims: claims,
                    signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
                    );

                

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    expiration = token.ValidTo
                });
            }
            return Unauthorized();
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey.Value.Secret)),
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken securityToken;
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;
            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;
        }

        private string GenerateToken(IEnumerable<Claim> claims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey.Value.Secret));

            var token = new JwtSecurityToken(
                   issuer: "http://google.com",
                   audience: "http://google.com",
                   expires: DateTime.UtcNow.AddSeconds(5),
                   claims: claims,
                   signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
                   );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public class RefreshTokenModel
        {
            public string ExpiredToken { get; set; }
        }

        [HttpPost]
        [Route("refresh")]
        public IActionResult Refresh([FromBody] RefreshTokenModel model)
        {
            var principal = GetPrincipalFromExpiredToken(model.ExpiredToken);
            var username = principal.Identity.Name;
            return new ObjectResult(new
            {
                token = GenerateToken(principal.Claims)
            });
        }
    }
}