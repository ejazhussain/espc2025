using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using O365C.ConsoleApp.ACSDemo.Services;

namespace O365C.ConsoleApp.ACSDemo;

/// <summary>
/// Interactive demo showcasing ACS Chat communication between Client and Agent
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        // Set console encoding to UTF-8 for proper character rendering
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // Build host with dependency injection
        using var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(config =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices(services =>
            {
                services.AddScoped<ACSDemoService>();
            })
            .ConfigureLogging(logging =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .Build();

        var demoService = host.Services.GetRequiredService<ACSDemoService>();

        Console.Clear();
        PrintHeader();

        // Validate connection first
        Console.WriteLine("🔍 Validating Azure Communication Services connection...\n");
        if (!await demoService.ValidateConnectionAsync())
        {
            Console.WriteLine("❌ Failed to connect to ACS. Please check your appsettings.json configuration.");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            return;
        }

        Console.WriteLine("✅ Successfully connected to Azure Communication Services!\n");
        Console.WriteLine("Press any key to start the demo...");
        Console.ReadKey();

        // Run the interactive demo
        await RunInteractiveDemoAsync(demoService);
    }

    private static async Task RunInteractiveDemoAsync(ACSDemoService demoService)
    {
        while (true)
        {
            Console.Clear();
            PrintHeader();
            Console.WriteLine("📋 DEMO MENU - Choose a scenario:\n");
            Console.WriteLine("  1️.  Complete Demo - Full Client-Agent Chat Flow");
            Console.WriteLine("  2️.  Step-by-Step Demo - Walk Through Each Step");
            Console.WriteLine("  3️.  Quick Test - Send & Receive Messages");
            Console.WriteLine("  4️.  View Architecture Explanation");
            Console.WriteLine("  5️.  Exit\n");
            Console.Write("Select option (1-5): ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    await demoService.RunCompleteDemoAsync();
                    break;
                case "2":
                    await demoService.RunStepByStepDemoAsync();
                    break;
                case "3":
                    await demoService.RunQuickTestAsync();
                    break;
                case "4":
                    ShowArchitectureExplanation();
                    break;
                case "5":
                    Console.WriteLine("\n👋 Thanks for attending the demo!");
                    return;
                default:
                    Console.WriteLine("\n❌ Invalid option. Please try again.");
                    await Task.Delay(1500);
                    continue;
            }

            Console.WriteLine("\n\nPress any key to return to menu...");
            Console.ReadKey();
        }
    }

    private static void PrintHeader()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("================================================================");
        Console.WriteLine("                                                                ");
        Console.WriteLine("        🚀 Azure Communication Services Chat Demo 🚀           ");
        Console.WriteLine("                                                                ");
        Console.WriteLine("           Client <-> Backend API <-> Agent Flow               ");
        Console.WriteLine("                                                                ");
        Console.WriteLine("================================================================");
        Console.ResetColor();
        Console.WriteLine();
    }

    private static void ShowArchitectureExplanation()
    {
        Console.Clear();
        PrintHeader();
        Console.WriteLine("🏗️  ARCHITECTURE OVERVIEW\n");
        Console.WriteLine("================================================================\n");
        
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("1️.  AUTHENTICATION & TOKEN ISSUANCE");
        Console.ResetColor();
        Console.WriteLine("   • Client A requests access token from Backend API");
        Console.WriteLine("   • Backend API creates ACS identity & token for Client A");
        Console.WriteLine("   • Client B (Agent) requests token");
        Console.WriteLine("   • Backend API creates ACS identity & token for Client B");
        Console.WriteLine("   • Both clients now have secure tokens to communicate\n");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("2️.  THREAD SETUP");
        Console.ResetColor();
        Console.WriteLine("   • Backend API creates a Chat Thread in ACS");
        Console.WriteLine("   • Returns Thread ID to Client A");
        Console.WriteLine("   • Client B (Agent) is added to the thread");
        Console.WriteLine("   • Client A joins the thread");
        Console.WriteLine("   • Both participants can now see each other\n");

        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("3️.  MESSAGING");
        Console.ResetColor();
        Console.WriteLine("   • Client A sends message through ACS");
        Console.WriteLine("   • ACS delivers message to all thread participants");
        Console.WriteLine("   • Client B receives message and can reply");
        Console.WriteLine("   • Messages flow bidirectionally in real-time\n");

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("💡 KEY BENEFITS:");
        Console.ResetColor();
        Console.WriteLine("   ✓ Secure token-based authentication");
        Console.WriteLine("   ✓ Real-time bidirectional communication");
        Console.WriteLine("   ✓ Scalable cloud messaging infrastructure");
        Console.WriteLine("   ✓ Message persistence & history");
        Console.WriteLine("   ✓ Multi-participant support");
        Console.WriteLine("   ✓ Rich metadata (sender name, timestamps, etc.)\n");
    }
}
