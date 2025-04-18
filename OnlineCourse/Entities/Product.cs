﻿namespace OnlineCourse.Entities;

public class Product : SEO, ICreatedAudit, IModifiedAudit
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public string DefaultImageFileName { get; set; }
    public bool IsPublish { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime ModifiedAt { get; set; }
    public int ModifiedBy { get; set; }
}

public interface ICreatedAudit : IAuditable
{
    DateTime CreatedAt { get; set; }
    int CreatedBy { get; set; }
}

public interface IModifiedAudit : IAuditable
{
    DateTime ModifiedAt { get; set; }
    int ModifiedBy { get; set; }
}

public interface IAuditable
{ }