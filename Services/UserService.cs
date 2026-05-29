using LuanVanTotNghiep.Models.Entities;
using LuanVanTotNghiep.Repositories;

namespace LuanVanTotNghiep.Services;
public class UserService(UserRepo userRepo)
{
    //Xử Lý Luôn Điều Kiện 
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
            await userRepo.Add(user);
    }
      public async Task UpdateUser(NsUser user)
        {
           var us1=await userRepo.GetbyId(user.Id);
           if(us1 !=null)
        {
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