using System;
using System.Collections.Generic;
using NCBA.DCL.Models;

namespace NCBA.DCL.DTOs;

public class UpdateChecklistWithDocsRequest
{
    public ChecklistStatus? Status { get; set; }
    public string? GeneralComment { get; set; }
    public List<DocumentCategoryDto>? Documents { get; set; }
}

public class DocumentCategoryDto
{
    public string Category { get; set; } = string.Empty;
    public List<DocumentDto> DocList { get; set; } = new();
}

public class DocumentDto
{
    public Guid Id { get; set; }
    public string? FileUrl { get; set; }
    public string? Comment { get; set; }
    public DocumentStatus? Status { get; set; }
    public string? DeferralReason { get; set; }
    public string? DeferralNumber { get; set; }
}


