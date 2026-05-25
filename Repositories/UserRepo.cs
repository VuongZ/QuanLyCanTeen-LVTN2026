using LuanVanTotNghiep.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Repositories;
public class UserRepo
{
    //Xử Lý Với DataBase Ở Đây Thôi Khỏi Điều Kiện Cũng Được 
    //Không Thích Viết Giống DƯới Thì Viết Kiểu UserRepo(AppDbContext Context) Cũng Được
    private readonly AppDbContext Context;
    public UserRepo(AppDbContext appContexters)
    {
        Context= appContexters;
        
    }
    public async Task<IEnumerable<NsUser>> GetAllUser()
    {
        return await Context.NsUsers.ToListAsync();
    }
    public async Task<NsUser> GettUserbyId(int id)
    {
        return Context.NsUsers.FirstOrDefault(user => user.Id == id);
    }
    public async Task AddUser(NsUser user)
    {
        Context.Add(user);
        await Context.SaveChangesAsync();
    }
      public async Task UpdateUser(NsUser user)
        {
            Context.NsUsers.Update(user);
            await Context.SaveChangesAsync();
        }
          public async Task DeleteUser(int id)
        {
            var user = await Context.NsUsers.FindAsync(id);
             Context.NsUsers.Remove(user);
                await Context.SaveChangesAsync();
        
        }
}