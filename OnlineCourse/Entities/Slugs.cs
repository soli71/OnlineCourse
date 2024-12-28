using System.Text.RegularExpressions;

namespace OnlineCourse.Entities;

public class SEO
{
    private string _slug;

    public string Slug
    {
        get { return _slug; }
        set
        {
            _slug = Regex.Replace(value.ToLower().Trim(), @"\s+", "-")
                .Replace("'", "")
                .Replace("\"", "");
        }
    }

    public string MetaTitle { get; set; }

    public string MetaTagDescription { get; set; }
    public string[] MetaKeywords { get; set; }
}