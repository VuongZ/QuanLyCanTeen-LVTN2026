using LuanVanTotNghiep.Models.Entities;
using LuanVanTotNghiep.Repositories;

namespace LuanVanTotNghiep.Services;
public class UserService(UserRepo userRepo)
{
    //Xử Lý Luôn Điều Kiện 
    public async Task<IEnumerable<NsUser>> GetAllUser()
    {
        return await userRepo.GetAllUser();
    }
     public async Task<NsUser?> GettUserbyId(int id)
    {
        var user= await userRepo.GettUserbyId(id);
        if(user == null) return null;
        return user;
    }
    public async Task AddUser(NsUser user)
    {
            await userRepo.AddUser(user);
    }
      public async Task UpdateUser(NsUser user)
        {
           var us1=await userRepo.GettUserbyId(user.Id);
           if(us1 !=null)
        {
            await userRepo.UpdateUser(us1);
        }
        }
          public async Task DeleteUser(int id)
        {
            var us1=await userRepo.GettUserbyId(id);
           if(us1 !=null)
        {
            await userRepo.DeleteUser(id);
        }
        
        }
}