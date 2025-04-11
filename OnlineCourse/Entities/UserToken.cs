using Microsoft.AspNetCore.Identity;

namespace OnlineCourse.Entities;

public class UserToken : IdentityUserToken<int>
{
    public DateTime Expiration { get; set; }
}

public class Currency
{
    public byte Id { get; set; }
    public string Name { get; set; }
    public string Symbol { get; set; }
    public decimal Rate { get; set; }
}

