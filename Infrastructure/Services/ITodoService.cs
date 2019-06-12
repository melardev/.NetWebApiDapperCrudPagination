using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApiDapperCrudPagination.Entities;
using WebApiDapperCrudPagination.Enums;

namespace WebApiDapperCrudPagination.Infrastructure.Services
{
    public interface ITodoService
    {
        Task<Tuple<int, List<Todo>>> FetchMany(int page, int pageSize, TodoShow show = TodoShow.All);
        Task<Todo> CreateTodo(Todo todo);
        Task<Todo> Update(Todo currentTodo, Todo todoFromUser);
        Task Delete(int todoId);
        Task DeleteAll();
        Task<Todo> GetById(int id);
        Task<Todo> FetchProxyById(int id);
    }
}