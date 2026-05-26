using RedPandaFlow.Application.Common;
using RedPandaFlow.Application.DTOs;

namespace RedPandaFlow.Application.Interfaces.Services
{
    public interface ISearchService
    {
        Task<ServiceResult<SearchResultDto>> SearchAsync(string query, Guid userId, int limit = 10);
    }
}
