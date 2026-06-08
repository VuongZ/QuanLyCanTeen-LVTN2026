using Microsoft.Net.Http.Headers;

namespace LuanVanTotNghiep.DTOs;
public class ShiftDto
{
    public int Id{get ;set ;}
    public string ShiftName{get;set;}=null!;
    public TimeOnly StartTime {get;set;}
    public TimeOnly EndTime{get;set;}
    public int? MaxStaff{get;set;}
    public bool? IsOt{get;set;}
    public string? BranchName{get;set;}
}