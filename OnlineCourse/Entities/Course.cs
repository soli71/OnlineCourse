namespace OnlineCourse.Entities;

public class Cart
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public CartStatus Status { get; set; }

    public List<CartItem> CartItems { get; set; }
}

public class CartItem
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; }
    public decimal Price { get; set; }
    public Cart Cart { get; set; }
    public int CartId { get; set; }
    public bool IsDelete { get; set; }
    public string Message { get; set; }
}

public enum CartStatus
{
    Active,
    Close
}

public class Course
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public int DurationTime { get; set; }
    public string ImageFileName { get; set; }
    public string SpotPlayerCourseId { get; set; }
    public string PreviewVideoName { get; set; }
    public byte Limit { get; set; }
    public int FakeStudentsCount { get; set; }
    public bool IsPublish { get; set; }
    public ICollection<CourseSeason> CourseSeasons { get; set; }
}

public class CourseSeason
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; }
    public byte Order { get; set; }
    public ICollection<HeadLines> HeadLines { get; set; }
}

public class HeadLines
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public byte Order { get; set; }
    public int DurationTime { get; set; }
    public int CourseSeasonId { get; set; }
    public CourseSeason CourseSeason { get; set; }
}

public class Blog
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public string ImageFileName { get; set; }
    public string[] Tags { get; set; }
    public DateTime CreateDate { get; set; }
    public bool IsPublish { get; set; }
    public int Visit { get; set; }
}

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
}