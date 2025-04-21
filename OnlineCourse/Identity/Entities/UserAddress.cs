using OnlineCourse.Orders;

namespace OnlineCourse.Identity.Entities;

public class UserAddress
{
    public int Id { get; set; }
    public string Address { get; set; }
    public string PostalCode { get; set; }
    public int CityId { get; set; }
    public City City { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }

    public ICollection<Order> Orders { get; set; }
}