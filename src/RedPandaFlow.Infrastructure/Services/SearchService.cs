using Microsoft.EntityFrameworkCore;
using RedPandaFlow.Application.Common;
using RedPandaFlow.Application.DTOs;
using RedPandaFlow.Application.Interfaces.Services;
using RedPandaFlow.Infrastructure.Data;

namespace RedPandaFlow.Infrastructure.Services
{
    public class SearchService : ISearchService
    {
        private readonly RedPandaFlowDbContext _context;

        public SearchService(RedPandaFlowDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResult<SearchResultDto>> SearchAsync(string query, Guid userId, int limit = 10)
        {
            var trimmed = (query ?? string.Empty).Trim();
            if (trimmed.Length == 0)
            {
                return ServiceResult<SearchResultDto>.Ok(new SearchResultDto());
            }

            var pattern = $"%{trimmed}%";
            var safeLimit = Math.Clamp(limit, 1, 25);

            var memberWorkspaceIds = await _context.WorkspaceUsers
                .Where(wu => wu.UserId == userId)
                .Select(wu => wu.WorkspaceId)
                .ToListAsync();

            var guestWorkspaceIds = await _context.BoardUser
                .Where(bu => bu.UserId == userId)
                .Select(bu => bu.Board.WorkspaceId)
                .Distinct()
                .ToListAsync();

            var accessibleWorkspaceIds = memberWorkspaceIds
                .Concat(guestWorkspaceIds)
                .ToHashSet();

            var workspaces = await _context.Workspaces
                .Where(w => accessibleWorkspaceIds.Contains(w.Id)
                            && (EF.Functions.ILike(w.Name, pattern)
                                || (w.Description != null && EF.Functions.ILike(w.Description, pattern))))
                .OrderBy(w => w.Name)
                .Take(safeLimit)
                .Select(w => new SearchWorkspaceResult
                {
                    Id = w.Id,
                    Name = w.Name,
                    Description = w.Description
                })
                .ToListAsync();

            var memberWorkspaceIdSet = memberWorkspaceIds.ToHashSet();
            var guestBoardIds = await _context.BoardUser
                .Where(bu => bu.UserId == userId)
                .Select(bu => bu.BoardId)
                .ToListAsync();

            var boards = await _context.Boards
                .Where(b => EF.Functions.ILike(b.Title, pattern)
                            && (memberWorkspaceIdSet.Contains(b.WorkspaceId) || guestBoardIds.Contains(b.Id)))
                .OrderBy(b => b.Title)
                .Take(safeLimit)
                .Select(b => new SearchBoardResult
                {
                    Id = b.Id,
                    WorkspaceId = b.WorkspaceId,
                    Title = b.Title,
                    WorkspaceName = b.Workspace.Name
                })
                .ToListAsync();

            return ServiceResult<SearchResultDto>.Ok(new SearchResultDto
            {
                Workspaces = workspaces,
                Boards = boards
            });
        }
    }
}
