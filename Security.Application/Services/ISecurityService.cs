using Security.Application.Dto;
using Security.Application.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Security.Application.Services
{
    public interface ISecurityService
    {
        Task<Result<string>> Login(string username, string password);

        Task<Result> Register(RegisterAplicationUserModel model, bool isAdmin, bool emailConfirmationRequired);


    }
}
