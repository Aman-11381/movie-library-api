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
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _db = db ?? throw new ArgumentNullException(nameof(db));

            ValidateConfiguration();
        }

        // Validate required configuration at startup
        private void ValidateConfiguration()
        {
            var requiredKeys = new[]
            {
                "Jwt:Key",
                "Jwt:Issuer",
                "Jwt:Audience",
                "Jwt:ExpiresInMinutes",
                "Jwt:RefreshTokenExpiryDays"
            };

            foreach (var key in requiredKeys)
            {
                if (string.IsNullOrEmpty(_config[key]))
                {
                    throw new InvalidOperationException($"Configuration key '{key}' is missing or empty.");
                }
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Request cannot be null." });

            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest(new { message = "Email is required." });

            if (string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Password is required." });

            try
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
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred during registration." });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Request cannot be null." });

            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Email and password are required." });

            try
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
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred during login." });
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            var refreshToken = Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(refreshToken))
                return Unauthorized("Refresh token missing");

            try
            {
                var storedToken = await _db.RefreshTokens
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Token == refreshToken);

                if (storedToken == null)
                    return Unauthorized(new { message = "Invalid refresh token" });

                if (storedToken.User == null)
                {
                    return Unauthorized(new { message = "Invalid refresh token" });
                }

                if (storedToken.ExpiresAt < DateTime.UtcNow)
                    return Unauthorized(new { message = "Refresh token expired" });

                if (storedToken.RevokedAt != null)
                    return Unauthorized(new { message = "Refresh token revoked" });

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
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred during token refresh." });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["refreshToken"];

            if (!string.IsNullOrEmpty(refreshToken))
            {
                try
                {
                    var stored = await _db.RefreshTokens
                        .FirstOrDefaultAsync(t => t.Token == refreshToken);

                    if (stored != null && stored.RevokedAt == null)
                    {
                        stored.RevokedAt = DateTime.UtcNow;
                        await _db.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                }

                // remove cookie
                Response.Cookies.Delete("refreshToken");
            }

            return Ok(new { message = "Logged out successfully" });
        }


        private string GenerateJwtToken(ApplicationUser user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (string.IsNullOrEmpty(user.Email))
                throw new InvalidOperationException("User email cannot be null or empty.");

            var jwtKey = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured.");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var expiresInMinutes = Convert.ToInt32(_config["Jwt:ExpiresInMinutes"] ?? "60");

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private RefreshToken GenerateRefreshToken(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentNullException(nameof(userId));

            var randomToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var expiryDays = Convert.ToInt32(_config["Jwt:RefreshTokenExpiryDays"] ?? "7");

            return new RefreshToken
            {
                Token = randomToken,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(expiryDays)
            };
        }

        private void SetRefreshTokenCookie(string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
                throw new ArgumentNullException(nameof(refreshToken));

            var expiryDays = Convert.ToInt32(_config["Jwt:RefreshTokenExpiryDays"] ?? "7");

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,               // required for HTTPS
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(expiryDays)
            };

            Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
        }
    }
}
