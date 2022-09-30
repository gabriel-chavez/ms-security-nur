using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Security.Application.Dto;
using Security.Application.Services;
using Security.Application.Utils;
using Security.Infrastructure.Security;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Security.Infrastructure.Services
{
    internal class SecurityService : ISecurityService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly JwtOptions _jwtOptions;

        private readonly ILogger<SecurityService> _logger;

        public SecurityService(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            JwtOptions jwtOptions,
            ILogger<SecurityService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _jwtOptions = jwtOptions;
            _logger = logger;
        }

        /// <summary>
        /// Logged in user base on username and password
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns>Returns a JWT for user logged if username and password provided are correct, otherwise returns an unsuccessful result </returns>
        public async Task<Result<string>> Login(string username, string password)
        {
            var user = await _userManager.FindByNameAsync(username);
            _logger.LogInformation("{username} is trying to login", username);
            if (user == null)
            {
                _logger.LogWarning("Username {username} is not registered", username);
                user = await _userManager.FindByEmailAsync(username);
                if (user == null)
                {
                    _logger.LogWarning("Email {email} is not registered", username);
                    return new Result<string>(false, "Usuario no encontrado");
                }
            }

            if (!user.Active)
            {
                _logger.LogWarning("{username} is not active", username);
                return new Result<string>(false, "El usuario no está activo");
            }
            var signInResult = await _signInManager.PasswordSignInAsync(user, password, false, true);
            if (signInResult.Succeeded)
            {
                _logger.LogInformation("{username} has logged in", username);
                var jwt = await GenerateJwt(user);
                return new Result<string>(jwt, true, "Usuario logueado correctamente");
            }
            return new Result<string>(false, "Usuario no encontrado");
        }

        private async Task<string> GenerateJwt(ApplicationUser user)
        {
            _logger.LogInformation($"Generating JWT for user {user.UserName}");
            var authClaims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.UserName),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    };

            var claims = await _userManager.GetClaimsAsync(user);
            foreach (var item in claims)
            {
                authClaims.Add(item);
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            foreach (var userRoleName in userRoles)
            {
                var userRole = await _roleManager.FindByNameAsync(userRoleName);
                var listOfClaims = await _roleManager.GetClaimsAsync(userRole);

                foreach (var item in listOfClaims)
                {
                    authClaims.Add(item);
                }
            }

            authClaims.Add(new Claim("FullName", user.FullName));
            authClaims.Add(new Claim("IsStaff", user.Staff.ToString()));

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var lifeTime = _jwtOptions.ValidateLifetime ? _jwtOptions.Lifetime : 60 * 24 * 365;

            var token = new JwtSecurityToken(
                issuer: _jwtOptions.ValidateIssuer ? _jwtOptions.ValidIssuer : null,
                audience: _jwtOptions.ValidateAudience ? _jwtOptions.ValidAudience : null,
                claims: authClaims,
                expires: DateTime.Now.AddMinutes(lifeTime),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<Result> Register(RegisterAplicationUserModel model, bool isAdmin, bool emailConfirmationRequired)
        {
            _logger.LogInformation($"{model.Email} is trying to register");
            var newUser = new ApplicationUser(model.Username, model.FirstName, model.LastName, true, isAdmin);

            IdentityResult userCreated = await _userManager.CreateAsync(newUser, model.Password);
            if (userCreated.Succeeded)
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(newUser);
                if (emailConfirmationRequired)
                {
                    //TODO: Send email confirmation
                }
                else
                {
                    IdentityResult result = await _userManager.ConfirmEmailAsync(newUser, token);
                    if (result.Succeeded)
                    {

                        await _userManager.AddToRolesAsync(newUser, model.Roles.AsEnumerable());

                        return new Result(true, "Usuario registrado correctamente");
                    }
                    else
                    {
                        return new Result(false, "El usuario lo creó pero la confirmación por correo electrónico falló");
                    }
                }
            }

            userCreated.Errors.ToList().ForEach(error => _logger.LogError("Error { ErrorCode }: { Description }", error.Code, error.Description));
            string errores="";
            foreach (var item in userCreated.Errors.ToList())
            {
                errores += " " + item.Description;
            }
            
            return new Result(false, errores);
        }

    }
}
