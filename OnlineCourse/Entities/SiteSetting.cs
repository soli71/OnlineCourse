namespace OnlineCourse.Entities;

public class SiteSetting
{
    public byte Id { get; set; }
    public string FooterContent { get; set; }

    public string PostalCode { get; set; }

    public string PhoneNumber { get; set; }
    public string Email { get; set; }
    public string Address { get; set; }
    public string Map { get; set; }
    public string AboutUs { get; set; }

    public string TelegramLink { get; set; }
    public string InstagramLink { get; set; }

    public string MainPageContent { get; set; }
    public string MainPageImage { get; set; }
    public bool VisibleMainPageContent { get; set; }

    public bool VisibleAboutUs { get; set; }

    public bool VisibleAddress { get; set; }

    public bool VisibleMap { get; set; }
    public bool VisibleEmail { get; set; }
    public bool VisiblePhoneNumber { get; set; }
    public bool VisiblePostalCode { get; set; }
    public bool VisibleTelegramLink { get; set; }
    public bool VisibleInstagramLink { get; set; }
    public bool VisibleFooterContent { get; set; }

    public bool VisibleMainPageImage { get; set; }
    public bool VisibleMainPageBlogs { get; set; }

    public bool VisibleMainPageCourses { get; set; }
    public string Title { get; set; }
}