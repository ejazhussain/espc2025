using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure;
using Azure.Communication;
using Azure.Communication.Chat;
using Azure.Communication.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace O365C.ConsoleApp.ACSDemo.Services;

/// <summary>
/// Demo service that orchestrates Client-Agent chat scenarios for presentations
/// </summary>
public class ACSDemoService
{
    private readonly CommunicationIdentityClient _identityClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ACSDemoService> _logger;
    private readonly string _endpointUrl;

    // Demo state
    private string? _clientUserId;
    private string? _clientToken;
    private string? _agentUserId;
    private string? _agentToken;
    private string? _threadId;

    public ACSDemoService(IConfiguration configuration, ILogger<ACSDemoService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var connectionString = _configuration.GetConnectionString("AzureCommunicationServices");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Azure Communication Services connection string is not configured");
        }

        _endpointUrl = _configuration["ACS:EndpointUrl"] ?? throw new InvalidOperationException("ACS EndpointUrl is not configured");
        _identityClient = new CommunicationIdentityClient(connectionString);
    }

    #region Public Demo Methods

    /// <summary>
    /// Validates the ACS connection
    /// </summary>
    public async Task<bool> ValidateConnectionAsync()
    {
        try
        {
            var testUser = await _identityClient.CreateUserAsync();
            await _identityClient.DeleteUserAsync(testUser.Value);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ACS connection validation failed");
            return false;
        }
    }

    /// <summary>
    /// Runs the complete demo flow automatically
    /// </summary>
    public async Task RunCompleteDemoAsync()
    {
        Console.Clear();
        PrintDemoHeader("COMPLETE DEMO - Full Client-Agent Chat Flow");

        try
        {
            // Step 1: Authentication
            PrintStep(1, "AUTHENTICATION & TOKEN ISSUANCE", ConsoleColor.Yellow);
            await CreateClientUserAsync("Customer - Sarah");
            await Task.Delay(1000);
            await CreateAgentUserAsync("Support Agent - John");
            await Task.Delay(1500);

            // Step 2: Thread Setup
            PrintStep(2, "THREAD SETUP", ConsoleColor.Green);
            await CreateChatThreadAsync("Customer Support - Order #12345");
            await Task.Delay(1000);
            await AddAgentToThreadAsync();
            await Task.Delay(1500);

            // Step 3: Messaging
            PrintStep(3, "MESSAGING", ConsoleColor.Magenta);
            await SendMessageAsClientAsync("Hello! I need help with my recent order.");
            await Task.Delay(2000);
            await SendMessageAsAgentAsync("Hi Sarah! I'd be happy to help you with order #12345. What seems to be the issue?");
            await Task.Delay(2000);
            await SendMessageAsClientAsync("The delivery date was supposed to be today, but I haven't received it yet.");
            await Task.Delay(2000);
            await SendMessageAsAgentAsync("I apologize for the inconvenience. Let me check the tracking information for you right away.");
            await Task.Delay(2000);

            // Show conversation
            await DisplayConversationHistoryAsync();

            Console.WriteLine("\n✅ Demo completed successfully!");
            PrintSummary();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n❌ Demo failed: {ex.Message}");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Runs step-by-step demo with user prompts
    /// </summary>
    public async Task RunStepByStepDemoAsync()
    {
        Console.Clear();
        PrintDemoHeader("STEP-BY-STEP DEMO - Interactive Walkthrough");

        try
        {
            // Step 1: Authentication
            PrintStep(1, "AUTHENTICATION & TOKEN ISSUANCE", ConsoleColor.Yellow);
            Console.WriteLine("Let's start by creating identities for our two participants...\n");
            Console.Write("Press ENTER to create Client identity...");
            Console.ReadLine();
            
            await CreateClientUserAsync("Student - Alan Partridge");
            
            Console.Write("\nPress ENTER to create Agent identity...");
            Console.ReadLine();

            await CreateAgentUserAsync("Teams Agent - John");
            
            Console.WriteLine("\n💡 Explanation: Both users now have secure tokens to authenticate with ACS.");
            Console.Write("\nPress ENTER to continue to Thread Setup...");
            Console.ReadLine();

            // Step 2: Thread Setup
            //Console.Clear();
            PrintDemoHeader("STEP-BY-STEP DEMO - Thread Setup");
            PrintStep(2, "THREAD SETUP", ConsoleColor.Yellow);
            Console.WriteLine("Now we'll create a chat thread and add participants...\n");
            
            Console.Write("Press ENTER to create chat thread...");
            Console.ReadLine();

            await CreateChatThreadAsync("Student Support - Order #12345");
            
            Console.Write("\nPress ENTER to add Agent to thread...");
            Console.ReadLine();
            
            await AddAgentToThreadAsync();
            
            Console.WriteLine("\n💡 Explanation: Both participants are now in the same chat thread and can exchange messages.");
            Console.Write("\nPress ENTER to continue to Messaging...");
            Console.ReadLine();

            // Step 3: Messaging
            //Console.Clear();
            PrintDemoHeader("STEP-BY-STEP DEMO - Messaging");
            PrintStep(3, "MESSAGING", ConsoleColor.Yellow);
            Console.WriteLine("Let's simulate a conversation between Client and Agent...\n");
            
            Console.Write("Press ENTER for Client to send first message...");
            Console.ReadLine();
            await SendMessageAsClientAsync("Hello! I need help with my recent order.");
            
            Console.Write("\nPress ENTER for Agent to respond...");
            Console.ReadLine();
            await SendMessageAsAgentAsync("Hi Alan! I'd be happy to help you with order #12345. What seems to be the issue?");
            
            Console.Write("\nPress ENTER for Client to reply...");
            Console.ReadLine();
            await SendMessageAsClientAsync("The delivery date was supposed to be today, but I haven't received it yet.");
            
            Console.Write("\nPress ENTER for Agent to respond...");
            Console.ReadLine();
            await SendMessageAsAgentAsync("I apologize for the inconvenience. Let me check the tracking information for you right away.");
            
            Console.Write("\nPress ENTER to view complete conversation history...");
            Console.ReadLine();
            
            await DisplayConversationHistoryAsync();
            
            Console.WriteLine("\n💡 Messages are delivered in real-time with full metadata (sender, timestamp, etc.).");
            Console.WriteLine("\n✅ completed!");
            PrintSummary();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n❌ Demo failed: {ex.Message}");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Quick test for sending and receiving messages
    /// </summary>
    public async Task RunQuickTestAsync()
    {
        Console.Clear();
        PrintDemoHeader("QUICK TEST - Send & Receive Messages");

        try
        {
            // Quick setup
            Console.WriteLine("⚡ Setting up test environment...\n");
            await CreateClientUserAsync("Test Client");
            await CreateAgentUserAsync("Test Agent");
            await CreateChatThreadAsync("Quick Test Chat");
            await AddAgentToThreadAsync();

            Console.WriteLine("\n✅ Setup complete! You can now send messages.\n");
            Console.WriteLine("================================================================\n");

            // Interactive messaging
            while (true)
            {
                Console.WriteLine("Who should send the message?");
                Console.WriteLine("  1 - Client");
                Console.WriteLine("  2 - Agent");
                Console.WriteLine("  3 - View conversation history");
                Console.WriteLine("  4 - Exit test");
                Console.Write("\nSelect option: ");
                
                var choice = Console.ReadLine();
                
                if (choice == "4") break;
                
                if (choice == "3")
                {
                    await DisplayConversationHistoryAsync();
                    continue;
                }

                Console.Write("\nEnter message: ");
                var message = Console.ReadLine();
                
                if (string.IsNullOrWhiteSpace(message))
                {
                    Console.WriteLine("❌ Message cannot be empty.\n");
                    continue;
                }

                if (choice == "1")
                {
                    await SendMessageAsClientAsync(message);
                }
                else if (choice == "2")
                {
                    await SendMessageAsAgentAsync(message);
                }
                else
                {
                    Console.WriteLine("❌ Invalid option.\n");
                    continue;
                }

                Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n❌ Test failed: {ex.Message}");
            Console.ResetColor();
        }
    }

    #endregion

    #region Private Helper Methods

    private async Task CreateClientUserAsync(string displayName)
    {
        Console.WriteLine($"Creating Client identity: {displayName}");
        var userResponse = await _identityClient.CreateUserAsync();
        var tokenResponse = await _identityClient.GetTokenAsync(userResponse.Value, new[] { CommunicationTokenScope.Chat });

        _clientUserId = userResponse.Value.Id;
        _clientToken = tokenResponse.Value.Token;
        _clientDisplayName = displayName;

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"   ✓ Client ID: {MaskId(_clientUserId)}");
        Console.WriteLine($"   ✓ Token expires: {tokenResponse.Value.ExpiresOn:yyyy-MM-dd HH:mm:ss}");
        Console.ResetColor();
    }

    private async Task CreateAgentUserAsync(string displayName)
    {
        Console.WriteLine($" Creating Agent identity: {displayName}");
        var userResponse = await _identityClient.CreateUserAsync();
        var tokenResponse = await _identityClient.GetTokenAsync(userResponse.Value, new[] { CommunicationTokenScope.Chat });

        _agentUserId = userResponse.Value.Id;
        _agentToken = tokenResponse.Value.Token;
        _agentDisplayName = displayName;

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"   ✓ Agent ID: {MaskId(_agentUserId)}");
        Console.WriteLine($"   ✓ Token expires: {tokenResponse.Value.ExpiresOn:yyyy-MM-dd HH:mm:ss}");
        Console.ResetColor();
    }

    private string? _clientDisplayName;
    private string? _agentDisplayName;

    private async Task CreateChatThreadAsync(string topic)
    {
        Console.WriteLine($"Creating chat thread: \"{topic}\"");

        var chatClient = new ChatClient(new Uri(_endpointUrl), new CommunicationTokenCredential(_clientToken!));

        var participants = new List<ChatParticipant>
        {
            new ChatParticipant(new CommunicationUserIdentifier(_clientUserId!))
            {
                DisplayName = _clientDisplayName ?? "Student - Alan Partridge"
            }
        };

        var result = await chatClient.CreateChatThreadAsync(topic, participants);
        _threadId = result.Value.ChatThread?.Id;

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"   ✓ Thread created: {_threadId}");
        Console.WriteLine($"   ✓ {_clientDisplayName ?? "Student - Alan Partridge"} automatically joined as thread creator");
        Console.ResetColor();
    }

    private async Task AddAgentToThreadAsync()
    {
        Console.WriteLine("➕ Adding Agent to chat thread");

        var chatClient = new ChatClient(new Uri(_endpointUrl), new CommunicationTokenCredential(_clientToken!));
        var threadClient = chatClient.GetChatThreadClient(_threadId!);

        var participants = new[]
        {
            new ChatParticipant(new CommunicationUserIdentifier(_agentUserId!))
            {
                DisplayName = _agentDisplayName ?? "Support Agent - John"
            }
        };

        await threadClient.AddParticipantsAsync(participants);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("   ✓ Agent added successfully");
        Console.ResetColor();
    }

    private async Task SendMessageAsClientAsync(string message)
    {
        var chatClient = new ChatClient(new Uri(_endpointUrl), new CommunicationTokenCredential(_clientToken!));
        var threadClient = chatClient.GetChatThreadClient(_threadId!);

        var sendOptions = new SendChatMessageOptions
        {
            Content = message,
            MessageType = ChatMessageType.Text,
            SenderDisplayName = _clientDisplayName ?? "Student - Alan Partridge"
        };

        var result = await threadClient.SendMessageAsync(sendOptions);

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"[CLIENT → AGENT] {_clientDisplayName ?? "Student - Alan Partridge"}: {message}");
        Console.ResetColor();
        Console.WriteLine($"   Message ID: {result.Value.Id}");
    }

    private async Task SendMessageAsAgentAsync(string message)
    {
        var chatClient = new ChatClient(new Uri(_endpointUrl), new CommunicationTokenCredential(_agentToken!));
        var threadClient = chatClient.GetChatThreadClient(_threadId!);

        var sendOptions = new SendChatMessageOptions
        {
            Content = message,
            MessageType = ChatMessageType.Text,
            SenderDisplayName = _agentDisplayName ?? "Support Agent - John"
        };

        var result = await threadClient.SendMessageAsync(sendOptions);

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"📥 [AGENT → CLIENT] {_agentDisplayName ?? "Support Agent - John"}: {message}");
        Console.ResetColor();
        Console.WriteLine($"   Message ID: {result.Value.Id}");
    }

    private async Task DisplayConversationHistoryAsync()
    {
        Console.WriteLine("\n📜 CONVERSATION HISTORY");
        Console.WriteLine("================================================================\n");
        
        var chatClient = new ChatClient(new Uri(_endpointUrl), new CommunicationTokenCredential(_clientToken!));
        var threadClient = chatClient.GetChatThreadClient(_threadId!);
        
        var messages = new List<(string Sender, string Content, DateTime Time)>();
        
        await foreach (var message in threadClient.GetMessagesAsync())
        {
            if (message.Type == ChatMessageType.Text && !string.IsNullOrEmpty(message.Content?.Message))
            {
                messages.Add((
                    message.SenderDisplayName ?? "Unknown",
                    message.Content.Message,
                    message.CreatedOn.DateTime
                ));
            }
        }

        messages.Reverse();

        foreach (var msg in messages)
        {
            var color = msg.Sender.Contains("Student") || msg.Sender.Contains("Customer") ? ConsoleColor.Cyan : ConsoleColor.Yellow;
            Console.ForegroundColor = color;
            Console.WriteLine($"[{msg.Time:HH:mm:ss}] {msg.Sender}:");
            Console.ResetColor();
            Console.WriteLine($"  {msg.Content}\n");
        }
    }

    private void PrintStep(int stepNumber, string title, ConsoleColor color)
    {
        Console.WriteLine();
        Console.ForegroundColor = color;
        Console.WriteLine($"  STEP {stepNumber}: {title}");
        Console.ResetColor();
        Console.WriteLine("================================================================\n");
    }

    private void PrintDemoHeader(string title)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("================================================================");
        Console.WriteLine($"  {title}");
        Console.WriteLine("================================================================");
        Console.ResetColor();
        Console.WriteLine();
    }

    private void PrintSummary()
    {
        Console.WriteLine("\n================================================================");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n📊 DEMO SUMMARY");
        Console.ResetColor();
        Console.WriteLine($"   • Client User ID: {MaskId(_clientUserId)}");
        Console.WriteLine($"   • Agent User ID: {MaskId(_agentUserId)}");
        Console.WriteLine($"   • Thread ID: {_threadId}");
        Console.WriteLine($"   • Communication: Bidirectional ACS Chat");
        Console.WriteLine($"   • Authentication: Token-based (ACS Identity)");
        Console.WriteLine("\n================================================================");
    }

    private string MaskId(string? id)
    {
        if (string.IsNullOrEmpty(id) || id.Length <= 12)
            return id ?? "Unknown";
        
        return id.Substring(0, 8) + "****" + id.Substring(id.Length - 4);
    }

    #endregion
}
