using EnglishCenterSystem.Domain.Common;

namespace EnglishCenterSystem.Domain
    .Entities.Reading;

public class ReadingPassageImage : BaseEntity
{
    public int ReadingPassageImageId
    {
        get;
        set;
    }

    public int ReadingTaskId
    {
        get;
        set;
    }

    public string ImageUrl
    {
        get;
        set;
    } = null!;

    public string ObjectName
    {
        get;
        set;
    } = null!;

    public int PageNumber
    {
        get;
        set;
    }

    public int OrderIndex
    {
        get;
        set;
    }

    public ReadingTask ReadingTask
    {
        get;
        set;
    } = null!;
}