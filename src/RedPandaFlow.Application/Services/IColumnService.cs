using RedPandaFlow.Application.Common;
using RedPandaFlow.Application.DTOs;

namespace RedPandaFlow.Application.Services
{
    public interface IColumnService
    {
        Task<ServiceResult<List<ColumnDto>>> GetColumnsByBoardIdAsync(Guid boardId, Guid userId);
        Task<ServiceResult<List<ColumnDto>>> GetArchivedColumnsByBoardIdAsync(Guid boardId, Guid userId);
        Task<ServiceResult<ColumnDto>> GetColumnByIdAsync(Guid columnId, Guid userId);
        Task<ServiceResult<ColumnDto>> CreateColumnAsync(Guid boardId, Guid userId, CreateColumnRequest request);
        Task<ServiceResult<ColumnDto>> UpdateColumnAsync(Guid columnId, Guid userId, UpdateColumnRequest request);
        Task<ServiceResult<bool>> DeleteColumnAsync(Guid columnId, Guid userId);
        Task<ServiceResult<bool>> UpdateColumnOrderAsync(Guid columnId, Guid userId, UpdateColumnOrderRequest request);
        Task<ServiceResult<ColumnDto>> ArchiveColumnAsync(Guid columnId, Guid userId);
        Task<ServiceResult<ColumnDto>> RestoreColumnAsync(Guid columnId, Guid userId);
    }
}