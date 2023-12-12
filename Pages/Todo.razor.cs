namespace BlazeApp.Pages
{
    public partial class Todo
    {
        private List<TodoItem> todos = new();
        private string? newTodo;

        private void AddTodo()
        {
            if (!string.IsNullOrWhiteSpace(newTodo))
            {
                todos.Add(new TodoItem { Title = newTodo });
                newTodo = string.Empty;
            }
        }
    }
}