namespace RedPandaFlow.Application.DTOs
{
    public class SearchResultDto
    {
        public List<SearchWorkspaceResult> Workspaces { get; set; } = new();
        public List<SearchBoardResult> Boards { get; set; } = new();
    }

    public class SearchWorkspaceResult
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class SearchBoardResult
    {
        public Guid Id { get; set; }
        public Guid WorkspaceId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string WorkspaceName { get; set; } = string.Empty;
    }
}
