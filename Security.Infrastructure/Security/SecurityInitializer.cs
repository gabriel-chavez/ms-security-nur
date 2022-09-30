using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Security.Infrastructure.Security
{
    public class SecurityInitializer
    {
        private readonly ILogger<SecurityInitializer> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public SecurityInitializer(ILogger<SecurityInitializer> logger, UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager, IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _roleManager = roleManager;
            _userManager = userManager;
            _hostingEnvironment = webHostEnvironment;
        }

        private RoleManager<ApplicationRole> RoleManager => _roleManager;

        public async Task Initialize(string permissionJsonFilePath, string securityInitializationJsonFilePath)
        {
            _logger.LogInformation("=========== Starting Application Initialization Check =============");

            InitializePermissions(permissionJsonFilePath);

            string initializerJson = "";
            try
            {
                initializerJson = System.IO.File.ReadAllText(securityInitializationJsonFilePath);
                _logger.LogDebug("The file with the menu configuration has been read: " + initializerJson.Length + " chars");
            }
            catch (Exception q)
            {
                _logger.LogError(q, "The menu configuration couldnot be loaded, maybe missing file or malformed");
            }

            InitializerJsonConfig initJsonObj = JsonConvert.DeserializeObject<InitializerJsonConfig>(initializerJson);
            if (initJsonObj == null)
            {
                _logger.LogError("The configuration for the inisitailization is not well formed, skipping");
                return;
            }

            // Creates the users
            bool errorConnectingDatabase = await InitializeUsers(initJsonObj);

            if (errorConnectingDatabase)
            {
                _logger.LogError("Skipping all initialization because could not connect to database");
                return;
            }

            // Creates the roles              
            await InitializeRoles(initJsonObj);

            await AssignPermissionsToRoles(initJsonObj);

            await AssignRolesToUsers(initJsonObj);
            
        }

        private async Task AssignRolesToUsers(InitializerJsonConfig initJsonObj)
        {
            //Asigna los roles a los usuarios en caso que no los tenga
            //Get User admin
            foreach (var userJson in initJsonObj.users)
            {
                ApplicationUser user = await _userManager.FindByNameAsync(userJson.user.ToString());

                foreach (var roleJson in userJson.userroles)
                {
                    ApplicationRole identyRole = await RoleManager.FindByNameAsync(roleJson.role.ToString());
                    IList<ApplicationUser> usersInRole = await _userManager.GetUsersInRoleAsync(identyRole.Name);

                    if (!usersInRole.Any(x => x.Id == user.Id))
                    {
                        await _userManager.AddToRoleAsync(user, identyRole.Name);
                        _logger.LogInformation("Added role { RoleName } to User { UserName }", identyRole.Name, user.UserName);
                    }
                }
            }
        }

        private void InitializePermissions(string permissionJsonFilePath)
        {
            string permissionsJson = "";
            try
            {
                permissionsJson = System.IO.File.ReadAllText(permissionJsonFilePath);
                _logger.LogDebug("The file with the permission configuration has been read: " + permissionsJson.Length + " chars");
            }
            catch (Exception q)
            {
                _logger.LogError(q, "The permission configuration could not be loaded, maybe missing file or malformed");
            }

            try
            {
                ApplicationPermission.ReadPermissions(permissionsJson);
                _logger.LogDebug("Permissions was loaded");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Permissions has not been loaded");
            }            
        }

        private async Task<bool> InitializeUsers(InitializerJsonConfig initJsonObj)
        {
            bool errorConnectingDatabase = false;

            // Creates the users
            foreach (var userJson in initJsonObj.users)
            {
                string userName = userJson.user;
                if (string.IsNullOrWhiteSpace(userName))
                {
                    _logger.LogError("User entry in initializer json config file is not well formed, skipping user");
                    continue;
                }

                ApplicationUser user = null;
                try
                {
                    user = await _userManager.FindByNameAsync(userName);
                }
                catch (Exception q)
                {
                    errorConnectingDatabase = true;
                    _logger.LogError(q, "Error obtaining users from the database");
                    break;
                }

                if (user == null)
                {
                    // User doesnt exist so we create it
                    user = new ApplicationUser(userJson.user.ToString(), userJson.user.ToString(), userJson.user.ToString(), true, true);

                    IdentityResult userCreated = await _userManager.CreateAsync(user, userJson.password.ToString());
                    if (!userCreated.Succeeded)
                    {
                        _logger.LogError($"Didn't create user {user.UserName}");
                        continue;
                    }
                    _logger.LogInformation("Created user " + user.UserName);
                }
            }
            return errorConnectingDatabase;
        }


        private async Task InitializeRoles(InitializerJsonConfig initJsonObj)
        {
            try
            {
                IdentityResult result;

                foreach (var role in initJsonObj.roles)
                {
                    string rolName = role.role;
                    if (string.IsNullOrWhiteSpace(rolName))
                    {
                        _logger.LogError("The role name was not set or empty in Initializer Json Config file, skipping");
                        continue;
                    }

                    if (RoleManager.Roles.Any(x => x.Name == rolName))
                    {
                        _logger.LogInformation($"The role {rolName} is already created");
                        continue;
                    }
                    result = await RoleManager.CreateAsync(new ApplicationRole() { Name = rolName });
                    if (result.Succeeded)
                        _logger.LogInformation("The new role [" + rolName + "] has been created");
                    else
                        _logger.LogError("Error in the Identity module, first: " + result.Errors.First().Description);
                }
            }
            catch (Exception q)
            {
                _logger.LogError(q, "Error initializing the new roles ");
            }
        }

        private async Task AssignPermissionsToRoles(InitializerJsonConfig initJsonObj)
        {
            // Assigns permissions to roles
            foreach (var role in initJsonObj.roles)
            {
                foreach (var permissionJson in role.permissions)
                {
                    ApplicationPermission ap = ApplicationPermission.GetPermission(permissionJson.permission.ToString());
                    string rolName = role.role.ToString();
                    ApplicationRole objRole = await RoleManager.FindByNameAsync(rolName);
                    IList<Claim> claimsInRole = await RoleManager.GetClaimsAsync(objRole);
                    if (claimsInRole.Any(x => x.Value.Equals(ap.Mnemonic.ToString())))
                        continue;
                    Claim claim = new Claim("Permission_" + ap.Mnemonic.ToString(), ap.Mnemonic.ToString());
                    await RoleManager.AddClaimAsync(objRole, claim);
                    _logger.LogInformation("Added claim " + ap.Mnemonic);
                }
            }
        }
    }
}
