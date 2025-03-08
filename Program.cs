using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.ChatCompletion;       // For ChatHistory
using Microsoft.SemanticKernel.Connectors.OpenAI;      // For OpenAIChatCompletionService
using HelperMAS.Agents;                                // To access RemoveSignerAgent and AddSignerAgent

namespace HelperMAS
{
    class Program
    {
        // Helper method: given a prompt, return the first response text from the chat service.
        static async Task<string> GetResponseAsync(OpenAIChatCompletionService chatService, string prompt, int maxTokens, double temperature)
        {
            var responses = await chatService.GetTextContentsAsync(prompt, executionSettings: null, kernel: null);
            return responses.Count > 0 ? responses[0].Text.Trim() : "";
        }

        // This method asks the AI to decide which agent to call based on the conversation.
        static async Task<string> GetRoutingDecisionAsync(OpenAIChatCompletionService chatService, string conversationContext)
        {
            // Construct a prompt that explains the situation and asks the model to answer with either "remove" or "add".
            string routingPrompt =
                "You are an AI that helps route finance requests. Based on the conversation below, decide if the user's intent is to remove a signer or add a signer. " +
                "Respond with exactly one word: 'remove' if the user wants to remove a signer, or 'add' if the user wants to add a signer. " +
                "Do not include any other text.\n\n" +
                "Conversation:\n" + conversationContext;
            
            return await GetResponseAsync(chatService, routingPrompt, 20, 0.5);
        }

        static async Task Main(string[] args)
        {
            // Load configuration from user secrets.
            var configuration = new ConfigurationBuilder()
                .AddUserSecrets<Program>()
                .Build();

            string apiKey = configuration["OpenAI:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("OpenAI API key is not set. Please set it using 'dotnet user-secrets'.");
                return;
            }

            // Create the OpenAI Chat Completion Service instance using model "gpt-4o-mini".
            var chatService = new OpenAIChatCompletionService("gpt-4o-mini", apiKey);

            // Start the conversation with the helper agent.
            string greetingPrompt = "You are a finance helper agent. Greet the user warmly and ask how you can help with professional work in finance.";
            string greeting = await GetResponseAsync(chatService, greetingPrompt, 50, 0.7);
            Console.WriteLine("Helper Agent: " + greeting);

            // Read the user's input.
            string userInput = Console.ReadLine()?.Trim() ?? "";

            // Build the conversation context (you can expand this context as needed).
            string conversationContext = "Helper Agent: " + greeting + "\nUser: " + userInput;

            // Ask the AI (as part of the helper agent) to decide the routing based on the conversation.
            string decision = await GetRoutingDecisionAsync(chatService, conversationContext);
            //Console.WriteLine("Routing Decision (from AI): " + decision);

            // Based on the decision, call the appropriate independent agent.
            if (decision.Equals("remove", StringComparison.OrdinalIgnoreCase))
            {
                var removeAgent = new RemoveSignerAgent(chatService, "signers.xlsx");
                await removeAgent.RunAsync(userInput);
            }
            else if (decision.Equals("add", StringComparison.OrdinalIgnoreCase))
            {
                var addAgent = new AddSignerAgent(chatService, "signers.xlsx");
                await addAgent.RunAsync();
            }
            else
            {
                Console.WriteLine("Helper Agent: I'm sorry, I cannot determine the appropriate action for your request.");
            }

            // Ask whether to continue or exit.
            while (true)
            {
                Console.WriteLine("Type 'continue' to start a new conversation or 'quit' to exit:");
                string decision2 = Console.ReadLine()?.Trim().ToLower() ?? "";
                if (decision2 == "continue")
                {
                    Console.WriteLine("\nStarting a new conversation...\n");
                    await Main(args); // Restart conversation.
                    break;
                }
                else if (decision2 == "quit")
                {
                    Console.WriteLine("Exiting program. Goodbye!");
                    break;
                }
                else
                {
                    Console.WriteLine("Invalid input. Exiting.");
                    break;
                }
            }
        }
    }
}
