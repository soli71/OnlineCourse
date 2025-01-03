﻿using Microsoft.AspNetCore.Identity;

namespace OnlineCourse.Entities;

public class User : IdentityUser<int>
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Mobile { get; set; }
    public UserType Type { get; set; }
    public bool Inactive { get; set; }
}

public enum UserType
{
    Admin = 1,
    Site = 2
}