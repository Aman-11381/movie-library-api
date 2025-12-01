using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MovieLibrary.DTOs.Auth;
using MovieLibrary.DTOs.Auth.Response;
using MovieLibrary.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MovieLibrary.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _config;
        private readonly MovieLibraryDbContext _db;

        public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration config, MovieLibraryDbContext db)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
            _db = db;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            // 1. Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return BadRequest(new { message = "User already exists." });
            }

            // 2. Create the user object
            var user = new ApplicationUser
            {
                Email = request.Email,
                UserName = request.UserName ?? request.Email,
            };

            // 3. Create user with hashed password
            var result = await _userManager.CreateAsync(user, request.Password);

            // 4. Handle password or user errors
            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    errors = result.Errors.Select(e => e.Description)
                });
            }

            return Ok(new { message = "User registered successfully!" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null)
                return Unauthorized(new { message = "Invalid email or password" });

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);

            if (!result.Succeeded)
                return Unauthorized(new { message = "Invalid email or password" });

            var jwt = GenerateJwtToken(user);

            // 1. Generate refresh token
            var refreshToken = GenerateRefreshToken(user.Id);

            // 2. Save refresh token to DB
            _db.RefreshTokens.Add(refreshToken);
            await _db.SaveChangesAsync();

            // 3. Set refresh token in secure cookie
            SetRefreshTokenCookie(refreshToken.Token);

            return Ok(new
            {
                token = jwt,
                expiresInMinutes = _config["Jwt:ExpiresInMinutes"]
            });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            var refreshToken = Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(refreshToken))
                return Unauthorized("Refresh token missing");

            var storedToken = await _db.RefreshTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == refreshToken);

            if (storedToken == null)
                return Unauthorized("Invalid refresh token");

            if (storedToken.ExpiresAt < DateTime.UtcNow)
                return Unauthorized("Refresh token expired");

            if (storedToken.RevokedAt != null)
                return Unauthorized("Refresh token revoked");

            // ROTATION: generate new refresh token
            var newRefreshToken = GenerateRefreshToken(storedToken.UserId);

            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.ReplacedByToken = newRefreshToken.Token;

            _db.RefreshTokens.Add(newRefreshToken);
            await _db.SaveChangesAsync();

            // Write new refresh token in cookie
            SetRefreshTokenCookie(newRefreshToken.Token);

            // Generate new JWT
            var newJwt = GenerateJwtToken(storedToken.User);

            return Ok(new RefreshResponse
            {
                Token = newJwt,
                ExpiresInMinutes = _config["Jwt:ExpiresInMinutes"]
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["refreshToken"];

            if (refreshToken != null)
            {
                var stored = await _db.RefreshTokens
                    .FirstOrDefaultAsync(t => t.Token == refreshToken);

                if (stored != null)
                    stored.RevokedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                // remove cookie
                Response.Cookies.Delete("refreshToken");
            }

            return Ok(new { message = "Logged out successfully" });
        }


        private string GenerateJwtToken(ApplicationUser user)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"])
            );

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    Convert.ToInt32(_config["Jwt:ExpiresInMinutes"])
                ),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private RefreshToken GenerateRefreshToken(string userId)
        {
            var randomToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            return new RefreshToken
            {
                Token = randomToken,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(
                    Convert.ToInt32(_config["Jwt:RefreshTokenExpiryDays"])
                )
            };
        }
        private void SetRefreshTokenCookie(string refreshToken)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,               // required for HTTPS
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(
                    Convert.ToInt32(_config["Jwt:RefreshTokenExpiryDays"])
                )
            };

            Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
        }

    }
}
