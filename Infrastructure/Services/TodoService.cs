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
    public class TodoService : ITodoService
    {
        private readonly string _connectionString;

        public TodoService()
        {
            _connectionString = WebConfigurationManager.ConnectionStrings["MsSql"].ConnectionString;
        }

        private IDbConnection Connection => new SqlConnection(_connectionString);

        public async Task<Tuple<int, List<Todo>>> FetchMany(int page = 1, int pageSize = 5,
            TodoShow show = TodoShow.All)
        {
            using (var conn = Connection)
            {
                var offset = (page - 1) * pageSize;
                if (conn.State == ConnectionState.Closed)
                    conn.Open();


                var todos = new List<Todo>();
                int totalCount;
                if (show == TodoShow.All)
                {
                    totalCount = (int) await conn.ExecuteScalarAsync("Select COUNT(*) From [dbo].[Todo]");

                    var reader = await conn.ExecuteReaderAsync(
                        "Select Id, Title, Completed, CreatedAt, UpdatedAt From Todo ORDER BY CreatedAt " +
                        "OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY", new
                        {
                            Offset = offset, PageSize = pageSize
                        });
                    while (reader.Read())
                        todos.Add(new Todo
                        {
                            Id = reader.GetInt32(0),
                            Title = reader.GetString(1),
                            Completed = reader.GetBoolean(2),
                            CreatedAt = reader.GetDateTime(3),
                            UpdatedAt = reader.GetDateTime(4)
                        });

                    // Or
                    /*
                        todos.Add(new Todo
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Title = Convert.ToString(reader["title"]),
                            Completed = Convert.ToBoolean(reader["Completed"]),
                            CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                            UpdatedAt = Convert.ToDateTime(reader["UpdatedAt"]),
                        });
                        */

                    return Tuple.Create(totalCount, todos);
                }

                var parameters = new DynamicParameters();
                parameters.Add("@Completed", show == TodoShow.Completed);

                totalCount = await conn.ExecuteScalarAsync<int>(
                    "Select COUNT(*) From [dbo].[Todo] Where Completed=@Completed", parameters);

                todos = (await conn.QueryAsync<Todo>(
                    "Select Id, Title, Completed, CreatedAt, UpdatedAt From Todo Where Completed = @Completed ORDER BY CreatedAt " +
                    $"OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY", parameters)).ToList();

                return Tuple.Create(totalCount, todos);
            }
        }


        public async Task<Todo> GetById(int id)
        {
            using (var dbConnection = Connection)
            {
                dbConnection.Open();
                return (await dbConnection.QueryAsync<Todo>("SELECT * FROM Todo WHERE Id = @Id", new {Id = id}))
                    .FirstOrDefault();
            }
        }

        public async Task<Todo> FetchProxyById(int id)
        {
            using (var dbConnection = Connection)
            {
                dbConnection.Open();
                return (await dbConnection.QueryAsync<Todo>("SELECT Id FROM Todo WHERE Id = @Id", new {Id = id}))
                    .FirstOrDefault();
            }
        }


        public async Task<Todo> CreateTodo(Todo todo)
        {
            using (var dbConnection = Connection)
            {
                dbConnection.Open();
                var result = await dbConnection.ExecuteScalarAsync(
                    "INSERT INTO Todo (Title,Description,Completed) VALUES (@Title,@Description,@Completed); Select SCOPE_IDENTITY();",
                    todo);
                todo.Id = int.Parse(result.ToString());
                return todo;
            }
        }

        public async Task<Todo> Update(Todo currentTodo, Todo todoFromUser)
        {
            using (var dbConnection = Connection)
            {
                todoFromUser.Id = currentTodo.Id;
                var now = DateTime.UtcNow;
                todoFromUser.UpdatedAt = now;
                dbConnection.Open();
                await dbConnection.QueryAsync(
                    "UPDATE [dbo].[Todo] SET Title = @Title,  Description  = @Description, Completed= @Completed, UpdatedAt= @UpdatedAt WHERE id = @Id",
                    todoFromUser);
                return todoFromUser;
            }
        }

        /// <summary>
        ///     Deletes a To do
        /// </summary>
        /// <param name="todoId"></param>
        /// <returns></returns>
        public async Task Delete(int todoId)
        {
            using (var dbConnection = Connection)
            {
                dbConnection.Open();
                await dbConnection.ExecuteAsync("DELETE FROM Todo WHERE Id=@Id", new {Id = todoId});
            }
        }

        public async Task DeleteAll()
        {
            using (var dbConnection = Connection)
            {
                dbConnection.Open();
                await dbConnection.ExecuteAsync("DELETE FROM Todo");
            }
        }
    }
}