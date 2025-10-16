// ToDoLy - Simple Console App with enum + JSON 

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

List<TaskItem> tasks = Load();
string dataFile = "tasks.json";

bool running = true;
while (running)
{
    ShowHeader();
    Console.WriteLine(">> Pick an option:");
    Console.WriteLine(">> (1) Show Task List (by date or project)");
    Console.WriteLine(">> (2) Add New Task");
    Console.WriteLine(">> (3) Edit Task (update, mark as done, remove)");
    Console.WriteLine(">> (4) Save and Quit");
    Console.Write("> ");

    switch ((Console.ReadLine() ?? "").Trim())
    {
        case "1": ShowTasksMenu(); break;
        case "2": AddTask(); break;
        case "3": EditTaskMenu(); break;
        case "4": Save(); running = false; break;
        default: Console.WriteLine("Invalid selection."); Pause(); break;
    }
}

// ===== Methods =====

void ShowHeader()
{
    Console.Clear();
    int todo = tasks.Count(t => t.Status == TaskStatus.Todo);
    int done = tasks.Count(t => t.Status == TaskStatus.Done);
    Console.WriteLine(">> Welcome to ToDoLy");
    Console.WriteLine($">> You have {todo} tasks todo and {done} tasks are done!");
    Console.WriteLine();
}

void ShowTasksMenu()
{
    Console.WriteLine("Show by: (1) Date   (2) Project");
    Console.Write("> ");
    var choice = (Console.ReadLine() ?? "").Trim();

    List<TaskItem> list = choice == "2"
        ? tasks.OrderBy(t => t.Project).ThenBy(t => t.DueDate).ThenBy(t => t.Id).ToList()
        : tasks.OrderBy(t => t.DueDate).ThenBy(t => t.Id).ToList();

    PrintTable(list);

    if (choice == "2")
    {
        Console.Write("Filter by project (enter to skip): ");
        var p = (Console.ReadLine() ?? "").Trim();
        if (!string.IsNullOrEmpty(p))
            PrintTable(list.Where(t => t.Project.Equals(p, StringComparison.OrdinalIgnoreCase)).ToList());
    }

    Pause();
}

void AddTask()
{
    Console.Write("Title: ");
    string title = (Console.ReadLine() ?? "").Trim();
    if (string.IsNullOrWhiteSpace(title)) { Console.WriteLine("Title is required."); Pause(); return; }

    Console.Write("Project: ");
    string project = (Console.ReadLine() ?? "").Trim();

    Console.Write("Due date (YYYY-MM-DD): ");
    if (!DateTime.TryParse(Console.ReadLine(), out DateTime due))
    {
        Console.WriteLine("Invalid date."); Pause(); return;
    }

    int nextId = tasks.Count == 0 ? 1 : tasks.Max(t => t.Id) + 1;
    tasks.Add(new TaskItem { Id = nextId, Title = title, Project = project, DueDate = due, Status = TaskStatus.Todo });
    Console.WriteLine("Task added."); Pause();
}

void EditTaskMenu()
{
    PrintTable(tasks.OrderBy(t => t.Id).ToList());
    Console.Write("Enter Task ID: ");
    if (!int.TryParse(Console.ReadLine(), out int id)) { Console.WriteLine("Invalid ID."); Pause(); return; }

    var task = tasks.FirstOrDefault(t => t.Id == id);
    if (task is null) { Console.WriteLine("Task not found."); Pause(); return; }

    Console.WriteLine("(1) Update  (2) Mark done  (3) Mark undone  (4) Remove  (0) Back");
    Console.Write("> ");
    switch ((Console.ReadLine() ?? "").Trim())
    {
        case "1":
            Console.Write($"New title (enter to keep '{task.Title}'): ");
            var newTitle = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(newTitle)) task.Title = newTitle!.Trim();

            Console.Write($"New project (enter to keep '{task.Project}'): ");
            var newProj = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(newProj)) task.Project = newProj!.Trim();

            Console.Write($"New due date (YYYY-MM-DD, enter to keep {task.DueDate:yyyy-MM-dd}): ");
            var s = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(s) && DateTime.TryParse(s, out DateTime newDue))
                task.DueDate = newDue;

            Console.WriteLine("Updated."); Pause();
            break;

        case "2": task.Status = TaskStatus.Done; Console.WriteLine("Marked as DONE.");   Pause(); break;
        case "3": task.Status = TaskStatus.Todo; Console.WriteLine("Marked as TODO.");   Pause(); break;
        case "4": tasks.Remove(task);            Console.WriteLine("Removed.");           Pause(); break;
        default: break;
    }
}

void PrintTable(List<TaskItem> list)
{
    Console.WriteLine();
    Console.WriteLine("ID  Title                   Project      Due Date   Status");
    Console.WriteLine("-----------------------------------------------------------");
    foreach (var t in list)
        Console.WriteLine($"{t.Id,-3} {t.Title,-22} {t.Project,-12} {t.DueDate:yyyy-MM-dd} {t.Status}");
    Console.WriteLine();
}

void Save()
{
    try
    {
        var json = JsonSerializer.Serialize(tasks, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(dataFile, json);
        Console.WriteLine("Saved. Bye!");
    }
    catch (Exception ex)
    {
        Console.WriteLine("Could not save tasks: " + ex.Message);
    }
}

static List<TaskItem> Load()
{
    const string file = "tasks.json";
    try
    {
        if (!File.Exists(file)) return new List<TaskItem>();
        var json = File.ReadAllText(file);
        return JsonSerializer.Deserialize<List<TaskItem>>(json) ?? new List<TaskItem>();
    }
    catch
    {
        Console.WriteLine("Could not load tasks. Starting with an empty list.");
        return new List<TaskItem>();
    }
}

static void Pause()
{
    Console.WriteLine("Press ENTER to continue...");
    Console.ReadLine();
}

// ===== Model =====
enum TaskStatus { Todo, Done }

class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Project { get; set; } = "";
    public DateTime DueDate { get; set; }
    public TaskStatus Status { get; set; } = TaskStatus.Todo;
}

