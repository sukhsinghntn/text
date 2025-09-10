using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NDAProcesses.Shared.Models;

namespace NDAProcesses.Shared.Services
{
    public interface IUserService
    {
        Task<bool> ValidateUser(UserModel user);
        Task<UserModel> GetUserData(string userName);
    }
}
