namespace Dmb.Model.Dtos;

public class ResumeDto
{
    public ResumePersonalInfoDto PersonalInfo { get; set; } = new();
    public List<ResumeWorkHistoryDto> WorkHistory { get; set; } = [];
    public List<ResumeEducationDto> Education { get; set; } = [];
}

public class ResumePersonalInfoDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ContactNo { get; set; }
    public string? Address { get; set; }
    public string? Summary { get; set; }
}

public class ResumeWorkHistoryDto
{
    public int WorkHistoryId { get; set; }
    public string Company { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? JobDescription { get; set; }
}

public class ResumeEducationDto
{
    public int EducationId { get; set; }
    public string School { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? CourseTaken { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class UpdateResumeDto
{
    public ResumePersonalInfoDto PersonalInfo { get; set; } = new();
    public List<ResumeWorkHistoryInputDto> WorkHistory { get; set; } = [];
    public List<ResumeEducationInputDto> Education { get; set; } = [];
}

public class ResumeWorkHistoryInputDto
{
    public string Company { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? JobDescription { get; set; }
}

public class ResumeEducationInputDto
{
    public string School { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? CourseTaken { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
