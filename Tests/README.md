# GUI regression checks

Run on Windows with the .NET 10 SDK, from the repository root:

```powershell
dotnet run --project .\Tests\GuiRegressionTests.csproj
```

The checks instantiate the application's real WPF controls and exercise message
binding, live chunks, completion, cancellation, errors, chat switching, event
subscriptions, attachments, and the SSE parser. No visible window or running
backend is required. These are programmatic WPF checks, not a screenshot test.

To also check the real local SSE backend, start it in another terminal:

```powershell
dotnet run --project .\Server\AiBackend.csproj
dotnet run --project .\Tests\GuiRegressionTests.csproj -- --live
```

The optional live check submits a synthetic message to `http://localhost:5000/stream`.
