namespace OnlineCourse.Identity.Entities;

public class City
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int ProvinceId { get; set; }
    public Province Province { get; set; }
    public ICollection<UserAddress> UserAddresses { get; set; }
}
