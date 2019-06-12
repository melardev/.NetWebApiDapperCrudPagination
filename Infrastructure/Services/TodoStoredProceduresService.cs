using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Configuration;
using Dapper;
using WebApiDapperCrudPagination.Entities;
using WebApiDapperCrudPagination.Enums;

namespace WebApiDapperCrudPagination.Infrastructure.Services
{
    public class TodoStoredProceduresService : ITodoService
    {
        private readonly string _connectionString;

        public TodoStoredProceduresService()
        {
            _connectionString = WebConfigurationManager.ConnectionStrings["MsSql"].ConnectionString;
        }

        public async Task<Tuple<int, List<Todo>>> FetchMany(int page, int pageSize, TodoShow show = TodoShow.All)
        {
            using (IDbConnection con = new SqlConnection(_connectionString))
            {
                if (con.State == ConnectionState.Closed)
                    con.Open();

                List<Todo> todos;
                if (show == TodoShow.All)
                {
                    var totalCount = await con.ExecuteScalarAsync<int>("GetAllTodosCount",
                        commandType: CommandType.StoredProcedure);

                    todos = (await con.QueryAsync<Todo>("GetAllTodosWithPagination", new
                    {
                        Page = page, PageSize = pageSize
                    }, commandType: CommandType.StoredProcedure)).ToList();
                    return Tuple.Create(totalCount, todos);
                }
                else
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@Page", page);
                    parameters.Add("@PageSize", pageSize);

                    var totalCount = show == TodoShow.Completed
                        ? await con.ExecuteScalarAsync<int>("GetAllCompletedTodosCount")
                        : await con.ExecuteScalarAsync<int>("GetAllPendingTodosCount");


                    todos = show == TodoShow.Completed
                        ? (await con.QueryAsync<Todo>("GetCompletedWithPagination", parameters,
                            commandType: CommandType.StoredProcedure)).ToList()
                        : (await con.QueryAsync<Todo>("GetPendingWithPagination", parameters,
                            commandType: CommandType.StoredProcedure)).ToList();

                    return Tuple.Create(totalCount, todos);
                }
            }
        }

        public async Task<Todo> GetById(int id)
        {
            var todo = new Todo();

            using (IDbConnection con = new SqlConnection(_connectionString))
            {
                if (con.State == ConnectionState.Closed)
                    con.Open();

                var parameter = new DynamicParameters();
                parameter.Add("@Id", id);
                // todo = con.Query<Todo>("GetTodoById", parameter, commandType: CommandType.StoredProcedure).FirstOrDefault();

                todo = (await con.QueryAsync<Todo>("GetTodoById", new {Id = id},
                    commandType: CommandType.StoredProcedure)).FirstOrDefault();
            }

            return todo;
        }

        public async Task<Todo> FetchProxyById(int id)
        {
            using (IDbConnection con = new SqlConnection(_connectionString))
            {
                if (con.State == ConnectionState.Closed)
                    con.Open();

                var parameter = new DynamicParameters();
                parameter.Add("@Id", id);

                return (await con.QueryAsync<Todo>("GetTodoProxyById", parameter,
                        commandType: CommandType.StoredProcedure))
                    .FirstOrDefault();
            }
        }

        public async Task<Todo> CreateTodo(Todo todo)
        {
            using (IDbConnection con = new SqlConnection(_connectionString))
            {
                if (con.State == ConnectionState.Closed)
                    con.Open();


                var parameters = new DynamicParameters();
                parameters.Add("@Title", todo.Title);
                parameters.Add("@Description", todo.Description);
                parameters.Add("@Completed", todo.Completed);

                var result = await con.ExecuteScalarAsync("CreateTodo", parameters,
                    commandType: CommandType.StoredProcedure);

                var todoId = int.Parse(result.ToString());
                todo.Id = todoId;
            }

            return todo;
        }

        public async Task<Todo> Update(Todo todoFromDb, Todo todoInput)
        {
            using (IDbConnection con = new SqlConnection(_connectionString))
            {
                if (con.State == ConnectionState.Closed)
                    con.Open();

                var now = DateTime.UtcNow;
                var parameters = new DynamicParameters();
                parameters.Add("@Id", todoFromDb.Id);
                parameters.Add("@Title", todoInput.Title);
                parameters.Add("@Description", todoInput.Description);
                parameters.Add("@Completed", todoInput.Completed);
                parameters.Add("@UpdatedAt", now);

                var rowAffected =
                    await con.ExecuteAsync("UpdateTodo", parameters, commandType: CommandType.StoredProcedure);

                todoInput.Id = todoFromDb.Id;
                todoInput.UpdatedAt = now;
            }

            return todoInput;
        }

        public async Task Delete(int todoId)
        {
            using (IDbConnection con = new SqlConnection(_connectionString))
            {
                if (con.State == ConnectionState.Closed)
                    con.Open();
                var parameters = new DynamicParameters();
                parameters.Add("@Id", todoId);
                var rowAffected =
                    await con.ExecuteAsync("DeleteTodo", parameters, commandType: CommandType.StoredProcedure);
            }
        }

        public async Task DeleteAll()
        {
            using (IDbConnection connection = new SqlConnection(_connectionString))
            {
                if (connection.State == ConnectionState.Closed)
                    connection.Open();

                await connection.ExecuteAsync("DeleteAllTodos", null, commandType: CommandType.StoredProcedure);
            }
        }
    }
}