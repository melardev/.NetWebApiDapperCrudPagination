using WebApiDapperCrudPagination.Models;

namespace WebApiDapperCrudPagination.Dtos.Responses.Shared
{
    public abstract class PagedDto : SuccessResponse
    {
        public PageMeta PageMeta { get; set; }
    }
}