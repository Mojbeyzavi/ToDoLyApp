// ToDoLy - Console App

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

// ===== Data section =====
List<TaskItem> tasks = Load();          // Load tasks from file (or empty list if file doesn't exist)
string dataFile = "tasks.json";         // Name of the JSON file where tasks are saved

// ===== Main program loop =====
bool running = true;                    //  keeps the app running until the user quits
while (running)
{
    ShowHeader();                       // Show colorful header + task summary

    // Print menu options in yellow for better visibility
    WithColor(ConsoleColor.Yellow, () =>
    {
        Console.WriteLine(">> Pick an option:");
        Console.WriteLine(">> (1) Show Task List (by date or project)");
        Console.WriteLine(">> (2) Add New Task");
        Console.WriteLine(">> (3) Edit Task (update, mark as done, remove)");
        Console.WriteLine(">> (4) Save and Quit");
    });
    Prompt("> ");                       // Cyan-colored input prompt

    // Read the user's choice and handle it
    switch ((Console.ReadLine() ?? "").Trim())
    {
        case "1": ShowTasksMenu(); break;   // Show list of tasks
        case "2": AddTask(); break;         // Add a new task
        case "3": EditTaskMenu(); break;    // Edit or delete an existing task
        case "4": Save(); running = false; break; // Save and exit program
        default: Error("Invalid selection."); Pause(); break; // Handle wrong input
    }
}

// ===== Functions ===== :)

void ShowHeader()
{
    Console.Clear(); // Clear screen
    int todo = tasks.Count(t => t.Status == TaskStatus.Todo); // Count tasks not done yet
    int done = tasks.Count(t => t.Status == TaskStatus.Done); // Count completed tasks

    Banner("Welcome to ToDoLy"); // Pretty cyan title banner

    Console.Write(">> You have ");
    WithColor(ConsoleColor.Cyan, () => Console.Write(todo)); // Cyan number for todo
    Console.Write(" tasks todo and ");
    WithColor(ConsoleColor.Green, () => Console.Write(done)); // Green number for done
    Console.WriteLine(" tasks are done!");
    Console.WriteLine();
}

void ShowTasksMenu()
{
    WithColor(ConsoleColor.Yellow, () => Console.WriteLine("Show by: (1) Date   (2) Project"));
    Prompt("> ");
    var choice = (Console.ReadLine() ?? "").Trim(); // Read user choice

    // Sort tasks based on user selection
    List<TaskItem> list = choice == "2"
        ? tasks.OrderBy(t => t.Project).ThenBy(t => t.DueDate).ThenBy(t => t.Id).ToList()
        : tasks.OrderBy(t => t.DueDate).ThenBy(t => t.Id).ToList();

    PrintTable(list); // Print all tasks

    // Optional project filter
    if (choice == "2")
    {
        Prompt("Filter by project (enter to skip): ");
        var p = (Console.ReadLine() ?? "").Trim();
        if (!string.IsNullOrEmpty(p))
            PrintTable(list.Where(t => t.Project.Equals(p, StringComparison.OrdinalIgnoreCase)).ToList());
    }

    Pause(); // Wait before returning to menu
}

void AddTask()
{
    Prompt("Title: ");
    string title = (Console.ReadLine() ?? "").Trim();
    if (string.IsNullOrWhiteSpace(title)) { Error("Title is required."); Pause(); return; }

    Prompt("Project: ");
    string project = (Console.ReadLine() ?? "").Trim();

    Prompt("Due date (YYYY-MM-DD): ");
    if (!DateTime.TryParse(Console.ReadLine(), out DateTime due))
    {
        Error("Invalid date."); Pause(); return;
    }

    // Generate the next available ID
    int nextId = tasks.Count == 0 ? 1 : tasks.Max(t => t.Id) + 1;
    tasks.Add(new TaskItem { Id = nextId, Title = title, Project = project, DueDate = due, Status = TaskStatus.Todo });
    Success("Task added."); Pause();
}

void EditTaskMenu()
{
    PrintTable(tasks.OrderBy(t => t.Id).ToList()); // Show all tasks before editing
    Prompt("Enter Task ID: ");
    if (!int.TryParse(Console.ReadLine(), out int id)) { Error("Invalid ID."); Pause(); return; }

    var task = tasks.FirstOrDefault(t => t.Id == id);
    if (task is null) { Error("Task not found."); Pause(); return; }

    // Menu for editing options
    WithColor(ConsoleColor.Yellow, () => Console.WriteLine("(1) Update  (2) Mark done  (3) Mark undone  (4) Remove  (0) Back"));
    Prompt("> ");
    switch ((Console.ReadLine() ?? "").Trim())
    {
        case "1": // Update title, project or due date
            Prompt($"New title (enter to keep '{task.Title}'): ");
            var newTitle = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(newTitle)) task.Title = newTitle!.Trim();

            Prompt($"New project (enter to keep '{task.Project}'): ");
            var newProj = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(newProj)) task.Project = newProj!.Trim();

            Prompt($"New due date (YYYY-MM-DD, enter to keep {task.DueDate:yyyy-MM-dd}): ");
            var s = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(s) && DateTime.TryParse(s, out DateTime newDue))
                task.DueDate = newDue;

            Success("Updated."); Pause();
            break;

        case "2": task.Status = TaskStatus.Done; Success("Marked as DONE.");   Pause(); break;
        case "3": task.Status = TaskStatus.Todo; Info("Marked as TODO.");      Pause(); break;
        case "4": tasks.Remove(task);            Success("Removed.");           Pause(); break;
        default: break;
    }
}

void PrintTable(List<TaskItem> list)
{
    Console.WriteLine();

    // Fixed column widths for easy layout
    int idW = 4;
    int titleW = 30;
    int projW = 18;
    int dueW = 10;
    int statusW = 6;

    int total = idW + titleW + projW + dueW + statusW + 8;

    // Print table header
    WithColor(ConsoleColor.DarkGray, () => Console.WriteLine(new string('─', total)));
    WithColor(ConsoleColor.Cyan, () =>
    {
        Console.WriteLine($"{Pad("ID", idW)}  {Pad("Title", titleW)}  {Pad("Project", projW)}  {Pad("Due Date", dueW)}  {Pad("Status", statusW)}");
    });
    WithColor(ConsoleColor.DarkGray, () => Console.WriteLine(new string('─', total)));

    var today = DateTime.Now.Date;

    // Loop through all tasks and print each row
    foreach (var t in list)
    {
        bool isDone = t.Status == TaskStatus.Done;
        bool isOverdue = !isDone && t.DueDate.Date < today;
        bool isSoon = !isDone && t.DueDate.Date >= today && t.DueDate.Date <= today.AddDays(2);

        string title = Truncate(t.Title, titleW);
        string proj = Truncate(t.Project, projW);
        string due = t.DueDate.ToString("yyyy-MM-dd");
        string statusText = t.Status.ToString();

        // Write row data
        Action writeRow = () =>
        {
            Console.Write($"{Pad(t.Id.ToString(), idW)}  ");
            Console.Write($"{Pad(title, titleW)}  ");
            Console.Write($"{Pad(proj, projW)}  ");
            Console.Write($"{Pad(due, dueW)}  ");

            // Color status for better visibility
            if (isDone) WithColor(ConsoleColor.Green, () => Console.Write(Pad(statusText, statusW)));
            else if (isOverdue) WithColor(ConsoleColor.Red, () => Console.Write(Pad(statusText, statusW)));
            else if (isSoon) WithColor(ConsoleColor.Yellow, () => Console.Write(Pad(statusText, statusW)));
            else WithColor(ConsoleColor.Gray, () => Console.Write(Pad(statusText, statusW)));

            Console.WriteLine();
        };

        // Done tasks are printed dim (gray)
        if (isDone) WithColor(ConsoleColor.DarkGray, writeRow);
        else writeRow();
    }

    WithColor(ConsoleColor.DarkGray, () => Console.WriteLine(new string('─', total)));
    Console.WriteLine();
}

void Save()
{
    try
    {
        // Convert tasks to JSON and write to file
        var json = JsonSerializer.Serialize(tasks, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(dataFile, json);
        Success("Saved. Bye!");
    }
    catch (Exception ex)
    {
        Error("Could not save tasks: " + ex.Message);
    }
}

static List<TaskItem> Load()
{
    const string file = "tasks.json";
    try
    {
        // If file doesn’t exist, start with an empty list
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
    WithColor(ConsoleColor.DarkGray, () => Console.WriteLine("Press ENTER to continue..."));
    Console.ReadLine();
}

// ===== Helper methods =====
static void WithColor(ConsoleColor color, Action action)
{
    // Temporarily change console color for better readability
    var prev = Console.ForegroundColor;
    Console.ForegroundColor = color;
    try { action(); }
    finally { Console.ForegroundColor = prev; }
}

static void Banner(string text)
{
    // Draws a simple colored box around title text
    WithColor(ConsoleColor.Cyan, () =>
    {
        Console.WriteLine("╔══════════════════════════════════════════╗");
        Console.WriteLine($"║  {text.PadRight(38)}║");
        Console.WriteLine("╚══════════════════════════════════════════╝");
    });
}

static void Prompt(string text) => WithColor(ConsoleColor.Cyan, () => Console.Write(text)); // Cyan prompt
static void Success(string text) => WithColor(ConsoleColor.Green, () => Console.WriteLine(text)); // Green success message
static void Info(string text) => WithColor(ConsoleColor.Yellow, () => Console.WriteLine(text));   // Yellow info message
static void Error(string text) => WithColor(ConsoleColor.Red, () => Console.WriteLine(text));     // Red error message

// Pads text to fit table column width
static string Pad(string s, int width) => (s ?? "").PadRight(Math.Max(width, 0));

// Shortens long text with "…" to avoid table overflow
static string Truncate(string s, int width)
{
    s ??= "";
    if (s.Length <= width) return s;
    if (width <= 3) return s.Substring(0, Math.Max(width, 0));
    return s.Substring(0, width - 1).TrimEnd() + "…";
}

// ===== Model =====

// Defines possible task states
enum TaskStatus { Todo, Done }

// Class that represents one task record
class TaskItem
{
    public int Id { get; set; }              // Unique number for each task
    public string Title { get; set; } = "";  // Task title
    public string Project { get; set; } = ""; // Optional project name
    public DateTime DueDate { get; set; }    // Deadline date
    public TaskStatus Status { get; set; } = TaskStatus.Todo; // Todo or Done
}


