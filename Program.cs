// ToDoLy - Console App
// -------------------------------------------------------------
// STRUCTURE OVERVIEW (for my presentation):
// 1) Main Program (Menu & Logic)        -> while loop + user choices
// 2) Class – TaskItem (Model)           -> defines what a task looks like
// 3) Enum – TaskStatus                  -> Todo / Done
// 4) List of Tasks                      -> in-memory storage (List<TaskItem>)
// 5) Methods                            -> Add, Edit, Show, Save, etc. (modular)
// 6) Saving & Loading (JSON persistence)-> keep data between sessions
// -------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

// =============================================================
// (4) LIST OF TASKS + (6) LOADING PERSISTENT DATA (JSON -> memory)
// -  keep tasks in memory as List<TaskItem>.
// - At startup it try to load existing tasks from tasks.json.
// =============================================================
List<TaskItem> tasks = Load();              // Load tasks from file (or empty list if file doesn't exist)
string dataFile = "tasks.json";             // JSON file that persists tasks between runs

// =============================================================
// (1) MAIN PROGRAM (MENU & LOGIC)
// - Simple while loop keeps the app running
// - Shows a menu and routes the user's choice to functions
// =============================================================
bool running = true;                        // Keeps the app running until the user quits
while (running)
{
    ShowHeader();                           // Header + summary (how many Todo/Done)

    // Menu options (colored for readability)
    WithColor(ConsoleColor.Yellow, () =>
    {
        Console.WriteLine(">> Pick an option:");
        Console.WriteLine(">> (1) Show Task List (by date or project)");
        Console.WriteLine(">> (2) Add New Task");
        Console.WriteLine(">> (3) Edit Task (update, mark as done, remove)");
        Console.WriteLine(">> (4) Save and Quit");
    });
    Prompt("> ");                           // Cyan prompt for input

    // Route the user's selection to the right action (modular methods)
    switch ((Console.ReadLine() ?? "").Trim())
    {
        case "1": ShowTasksMenu(); break;               // View tasks (date/project)
        case "2": AddTask(); break;                     // Create a new task
        case "3": EditTaskMenu(); break;                // Update / toggle / remove
        case "4": Save(); running = false; break;       // Persist to JSON + exit
        default: Error("Invalid selection."); Pause(); break;
    }
}

// =============================================================
// (5) METHODS — small, focused functions that keep code modular
// =============================================================

void ShowHeader()
{
    Console.Clear();
    int todo = tasks.Count(t => t.Status == TaskStatus.Todo); // Count pending tasks
    int done = tasks.Count(t => t.Status == TaskStatus.Done); // Count completed tasks

    Banner("Welcome to ToDoLy"); // Fancy title banner

    Console.Write(">> You have ");
    WithColor(ConsoleColor.Cyan, () => Console.Write(todo));   // Highlight numbers
    Console.Write(" tasks todo and ");
    WithColor(ConsoleColor.Green, () => Console.Write(done));
    Console.WriteLine(" tasks are done!");
    Console.WriteLine();
}

void ShowTasksMenu()
{
    // Choose sorting/grouping method (by date or by project)
    WithColor(ConsoleColor.Yellow, () => Console.WriteLine("Show by: (1) Date   (2) Project"));
    Prompt("> ");
    var choice = (Console.ReadLine() ?? "").Trim();

    // Sorted view to keep output tidy and predictable
    List<TaskItem> list = choice == "2"
        ? tasks.OrderBy(t => t.Project).ThenBy(t => t.DueDate).ThenBy(t => t.Id).ToList()
        : tasks.OrderBy(t => t.DueDate).ThenBy(t => t.Id).ToList();

    PrintTable(list); // Show all tasks

    // Optional: filter further by project name
    if (choice == "2")
    {
        Prompt("Filter by project (enter to skip): ");
        var p = (Console.ReadLine() ?? "").Trim();
        if (!string.IsNullOrEmpty(p))
            PrintTable(list.Where(t => t.Project.Equals(p, StringComparison.OrdinalIgnoreCase)).ToList());
    }

    Pause();
}

void AddTask()
{
    // Gather minimal task info (Title, Project, DueDate)
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

    // Generate next unique Id and add to list in memory
    int nextId = tasks.Count == 0 ? 1 : tasks.Max(t => t.Id) + 1;
    tasks.Add(new TaskItem { Id = nextId, Title = title, Project = project, DueDate = due, Status = TaskStatus.Todo });
    Success("Task added.");
    Pause();
}

void EditTaskMenu()
{
    // Show tasks first so the user can see IDs clearly
    PrintTable(tasks.OrderBy(t => t.Id).ToList());
    Prompt("Enter Task ID: ");
    if (!int.TryParse(Console.ReadLine(), out int id)) { Error("Invalid ID."); Pause(); return; }

    var task = tasks.FirstOrDefault(t => t.Id == id);
    if (task is null) { Error("Task not found."); Pause(); return; }

    // Offer common edit actions
    WithColor(ConsoleColor.Yellow, () => Console.WriteLine("(1) Update  (2) Mark done  (3) Mark undone  (4) Remove  (0) Back"));
    Prompt("> ");
    switch ((Console.ReadLine() ?? "").Trim())
    {
        case "1":
            // Update selected fields (keep old values if input is empty)
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

            Success("Updated.");
            Pause();
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

    // Fixed column widths for consistent layout
    int idW = 4;
    int titleW = 30;
    int projW = 18;
    int dueW = 10;
    int statusW = 6;
    int total = idW + titleW + projW + dueW + statusW + 8;

    // Header lines
    WithColor(ConsoleColor.DarkGray, () => Console.WriteLine(new string('─', total)));
    WithColor(ConsoleColor.Cyan, () =>
    {
        Console.WriteLine($"{Pad("ID", idW)}  {Pad("Title", titleW)}  {Pad("Project", projW)}  {Pad("Due Date", dueW)}  {Pad("Status", statusW)}");
    });
    WithColor(ConsoleColor.DarkGray, () => Console.WriteLine(new string('─', total)));

    var today = DateTime.Now.Date;

    foreach (var t in list)
    {
        bool isDone = t.Status == TaskStatus.Done;
        bool isOverdue = !isDone && t.DueDate.Date < today;
        bool isSoon = !isDone && t.DueDate.Date >= today && t.DueDate.Date <= today.AddDays(2);

        string title = Truncate(t.Title, titleW);
        string proj = Truncate(t.Project, projW);
        string due = t.DueDate.ToString("yyyy-MM-dd");
        string statusText = t.Status.ToString();

        // One small writer action so we can wrap with color when needed
        Action writeRow = () =>
        {
            Console.Write($"{Pad(t.Id.ToString(), idW)}  ");
            Console.Write($"{Pad(title, titleW)}  ");
            Console.Write($"{Pad(proj, projW)}  ");
            Console.Write($"{Pad(due, dueW)}  ");

            // Status color hints: Done=Green, Overdue=Red, Soon=Yellow, Else=Gray
            if (isDone) WithColor(ConsoleColor.Green, () => Console.Write(Pad(statusText, statusW)));
            else if (isOverdue) WithColor(ConsoleColor.Red, () => Console.Write(Pad(statusText, statusW)));
            else if (isSoon) WithColor(ConsoleColor.Yellow, () => Console.Write(Pad(statusText, statusW)));
            else WithColor(ConsoleColor.Gray, () => Console.Write(Pad(statusText, statusW)));

            Console.WriteLine();
        };

        // Completed rows appear dim to de-emphasize
        if (isDone) WithColor(ConsoleColor.DarkGray, writeRow);
        else writeRow();
    }

    WithColor(ConsoleColor.DarkGray, () => Console.WriteLine(new string('─', total)));
    Console.WriteLine();
}

void Save()
{
    // =========================================================
    // (6) SAVING (memory -> JSON file)
    // - Serialize the in-memory list and write to tasks.json.
    // =========================================================
    try
    {
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
    // =========================================================
    // (6) LOADING (JSON file -> memory)
    // - If file doesn't exist, start with an empty list.
    // =========================================================
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
    WithColor(ConsoleColor.DarkGray, () => Console.WriteLine("Press ENTER to continue..."));
    Console.ReadLine();
}

// =============================================================
// Helper methods (UI polish: colors, prompts, padded columns)
// =============================================================
static void WithColor(ConsoleColor color, Action action)
{
    var prev = Console.ForegroundColor;
    Console.ForegroundColor = color;
    try { action(); }
    finally { Console.ForegroundColor = prev; }
}

static void Banner(string text)
{
    WithColor(ConsoleColor.Cyan, () =>
    {
        Console.WriteLine("╔══════════════════════════════════════════╗");
        Console.WriteLine($"║  {text.PadRight(38)}║");
        Console.WriteLine("╚══════════════════════════════════════════╝");
    });
}

static void Prompt(string text)  => WithColor(ConsoleColor.Cyan,  () => Console.Write(text));
static void Success(string text) => WithColor(ConsoleColor.Green, () => Console.WriteLine(text));
static void Info(string text)    => WithColor(ConsoleColor.Yellow,() => Console.WriteLine(text));
static void Error(string text)   => WithColor(ConsoleColor.Red,   () => Console.WriteLine(text));

// Simple padding & truncation for table layout
static string Pad(string s, int width) => (s ?? "").PadRight(Math.Max(width, 0));
static string Truncate(string s, int width)
{
    s ??= "";
    if (s.Length <= width) return s;
    if (width <= 3) return s.Substring(0, Math.Max(width, 0));
    return s.Substring(0, width - 1).TrimEnd() + "…";
}

// =============================================================
// (3) ENUM – TaskStatus (restricts state to valid values)
// =============================================================
enum TaskStatus { Todo, Done }

// =============================================================
// (2) CLASS – TaskItem (MODEL)
// - Blueprint for a task record used throughout the app.
// - Works great with JSON (auto-serialize/deserialize).
// =============================================================
class TaskItem
{
    public int Id { get; set; }                  // Unique identifier
    public string Title { get; set; } = "";      // Task title
    public string Project { get; set; } = "";    // Optional project/category
    public DateTime DueDate { get; set; }        // Deadline
    public TaskStatus Status { get; set; } = TaskStatus.Todo; // Current state
}
