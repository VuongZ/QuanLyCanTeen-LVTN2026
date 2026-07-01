using LuanVanTotNghiep.Models.Entities;
using LuanVanTotNghiep.Repositories;

namespace LuanVanTotNghiep.Services;
public class UserService(UserRepo userRepo)
{
    public async Task<IEnumerable<NsUser>> GetAllUser()
    {
        return await userRepo.GetAll();
    }
     public async Task<NsUser?> GettUserbyId(int id)
    {
        var user= await userRepo.GetbyId(id);
        if(user == null) return null;
        return user;
    }
    public async Task AddUser(NsUser user)
    {
            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
            await userRepo.Add(user);
    }
    public async Task UpdateUser(NsUser user)
        {
            var us1=await userRepo.GetbyId(user.Id);
            if(us1 !=null)
        {
            us1.Username = user.Username;
            if (!string.IsNullOrWhiteSpace(user.Password))
                us1.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
            us1.FullName = user.FullName;
            us1.PhoneNumber = user.PhoneNumber;
            us1.BankName = user.BankName;
            us1.BankAccountNumber = user.BankAccountNumber;
            us1.BankAccountName = user.BankAccountName;
            us1.BranchId = user.BranchId;
            us1.RoleId = user.RoleId;
            us1.HireDate = user.HireDate;
            await userRepo.Update(us1);
        }
        }
        public async Task DeleteUser(int id)
        {
            var us1=await userRepo.GetbyId(id);
            if(us1 !=null)
        {
            await userRepo.Delete(id);
        }
        
        }
}

