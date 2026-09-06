using AiDesktopClient.Models;

namespace AiDesktopClient.Services;

public sealed class MockChatService : IChatService
{
    private static readonly string[] MockResponses =
    [
        "Certainly! Here's a comprehensive answer to your question.\n\n## Overview\n\nThe concept you're asking about involves several key aspects that work together to create a cohesive solution.\n\n## Key Points\n\n1. **First consideration** - This is the primary factor to keep in mind when approaching this problem.\n2. **Second consideration** - Equally important, this aspect ensures robustness.\n3. **Third consideration** - Often overlooked, but critical for production systems.\n\n## Code Example\n\n```csharp\npublic class Solution\n{\n    public void Process()\n    {\n        var result = Calculate();\n        Console.WriteLine($\"Result: {result}\");\n    }\n\n    private int Calculate()\n    {\n        return 42;\n    }\n}\n```\n\n## Summary\n\nIn summary, this approach provides a clean, maintainable solution that follows best practices and design patterns.\n\n> **Note:** Remember to handle edge cases in production code.",

        "Great question! Let me break this down step by step.\n\n### Understanding the Problem\n\nFirst, let's understand what we're working with here. The core challenge lies in efficiently managing state while maintaining performance.\n\n### Solution\n\nHere's how you can approach this:\n\n```python\ndef solve(n: int) -> list[int]:\n    if n <= 0:\n        return []\n    if n == 1:\n        return [0]\n    \n    fib = [0, 1]\n    for i in range(2, n):\n        fib.append(fib[i-1] + fib[i-2])\n    return fib\n\nresult = solve(10)\nprint(result)  # [0, 1, 1, 2, 3, 5, 8, 13, 21, 34]\n```\n\n### Performance Analysis\n\n| Metric | Value |\n|--------|-------|\n| Time   | O(n)  |\n| Space  | O(n)  |\n\nThis solution is optimal for the given constraints.",

        "I'd be happy to help with that!\n\n### Step-by-Step Guide\n\n**Step 1: Setup**\n\nFirst, you need to set up your development environment:\n\n- Install the required dependencies\n- Configure your project structure\n- Set up linting and formatting\n\n**Step 2: Implementation**\n\nThe core implementation follows the **Repository Pattern**:\n\n```typescript\ninterface Repository<T> {\n    findById(id: string): Promise<T | null>;\n    findAll(): Promise<T[]>;\n    save(entity: T): Promise<T>;\n    delete(id: string): Promise<void>;\n}\n\nclass UserRepository implements Repository<User> {\n    async findById(id: string): Promise<User | null> {\n        return null;\n    }\n\n    async findAll(): Promise<User[]> {\n        return [];\n    }\n\n    async save(user: User): Promise<User> {\n        return user;\n    }\n\n    async delete(id: string): Promise<void> {\n    }\n}\n```\n\n**Step 3: Testing**\n\nMake sure to write comprehensive tests:\n\n```typescript\ndescribe('UserRepository', () => {\n    it('should find user by id', async () => {\n        const repo = new UserRepository();\n        const user = await repo.findById('123');\n        expect(user).toBeNull();\n    });\n});\n```\n\n> **Tip:** Always follow the SOLID principles when designing your architecture.",

        "Here's my analysis of this topic:\n\n## Background\n\nThis is a well-studied area in computer science with several established approaches.\n\n### Approach 1: Brute Force\nThe simplest approach tries all possibilities:\n\n```java\npublic class BruteForce {\n    public static int findMax(int[] arr) {\n        int max = arr[0];\n        for (int i = 1; i < arr.length; i++) {\n            if (arr[i] > max) {\n                max = arr[i];\n            }\n        }\n        return max;\n    }\n}\n```\n\n**Time Complexity:** O(n)\n\n### Approach 2: Divide and Conquer\n\nA more elegant solution using recursion:\n\n```java\npublic class DivideConquer {\n    public static int findMax(int[] arr, int left, int right) {\n        if (left == right) return arr[left];\n        \n        int mid = (left + right) / 2;\n        int leftMax = findMax(arr, left, mid);\n        int rightMax = findMax(arr, mid + 1, right);\n        \n        return Math.max(leftMax, rightMax);\n    }\n}\n```\n\n---\n\n**Recommendation:** For most practical cases, the simple iteration is sufficient and more readable.",

        "Let me explain this concept thoroughly.\n\n## Definition\n\nIn computer science, this pattern is known as the **Observer Pattern**. It defines a one-to-many dependency between objects so that when one object changes state, all its dependents are notified.\n\n## Implementation\n\n### C# Implementation\n\n```csharp\npublic interface IObserver<T>\n{\n    void OnNext(T value);\n    void OnError(Exception error);\n    void OnCompleted();\n}\n\npublic class Subject<T> : IObservable<T>\n{\n    private readonly List<IObserver<T>> _observers = [];\n\n    public IDisposable Subscribe(IObserver<T> observer)\n    {\n        _observers.Add(observer);\n        return new Unsubscriber(_observers, observer);\n    }\n\n    public void Notify(T value)\n    {\n        foreach (var observer in _observers)\n            observer.OnNext(value);\n    }\n}\n```\n\n### Key Benefits\n\n- **Loose coupling** between subject and observers\n- **Dynamic relationships** at runtime\n- **Event-driven architecture** support\n\n### Use Cases\n\n1. GUI event handling\n2. Real-time data feeds\n3. Message broker systems\n4. Reactive programming"
    ];

    private readonly Random _random = new();

    public async IAsyncEnumerable<string> StreamResponseAsync(
        string chatId,
        string modelId,
        string message,
        IReadOnlyList<Attachment>? attachments = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = MockResponses[_random.Next(MockResponses.Length)];
        var words = response.Split(' ');

        for (int i = 0; i < words.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return words[i] + " ";
            await Task.Delay(_random.Next(20, 80), cancellationToken);
        }
    }

    public async Task<ChatResponse> SendMessageAsync(
        string chatId,
        string modelId,
        string message,
        IReadOnlyList<Attachment>? attachments = null,
        CancellationToken cancellationToken = default)
    {
        var response = MockResponses[_random.Next(MockResponses.Length)];

        await Task.Delay(_random.Next(500, 2000), cancellationToken);

        var inputTokens = message.Split(' ').Length * 2;
        var outputTokens = response.Split(' ').Length * 2;

        return new ChatResponse
        {
            Content = response,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            ResponseTimeMs = _random.Next(500, 2000),
            Cost = (decimal)(inputTokens * 0.000003 + outputTokens * 0.000015)
        };
    }
}
