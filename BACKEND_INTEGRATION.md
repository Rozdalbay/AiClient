# Backend Integration Guide

## Overview

This document describes how the AI Desktop Client frontend should be integrated with a backend API. The frontend currently uses Mock Services and is designed so that mock implementations can be replaced with real backend services without modifying ViewModels or UI code.

## Architecture

```
Desktop Client (WPF)
      ↓ (HTTP/SSE)
Backend API
      ↓
AI Provider (OpenAI, Anthropic, Google, etc.)
      ↓
Backend API
      ↓ (SSE chunks)
Desktop Client
```

**Important:** The frontend NEVER communicates directly with AI providers. All communication goes through the backend.

## Service Interfaces

The frontend defines these service interfaces that must be implemented by the backend:

### IChatService

Location: `Services/IChatService.cs`

```csharp
public interface IChatService
{
    IAsyncEnumerable<string> StreamResponseAsync(
        string chatId,
        string modelId,
        string message,
        IReadOnlyList<Attachment>? attachments = null,
        CancellationToken cancellationToken = default);

    Task<ChatResponse> SendMessageAsync(
        string chatId,
        string modelId,
        string message,
        IReadOnlyList<Attachment>? attachments = null,
        CancellationToken cancellationToken = default);
}
```

**Streaming:** The frontend expects an `IAsyncEnumerable<string>` for streaming responses. Each string is a chunk of the response text. The backend should use Server-Sent Events (SSE) or a similar mechanism to stream chunks.

### IModelService

Location: `Services/IModelService.cs`

```csharp
public interface IModelService
{
    Task<IReadOnlyList<ModelInfo>> GetModelsAsync(CancellationToken cancellationToken = default);
    Task<ModelInfo?> GetModelByIdAsync(string modelId, CancellationToken cancellationToken = default);
}
```

### IUsageService

Location: `Services/IUsageService.cs`

```csharp
public interface IUsageService
{
    Task<UsageInfo> GetUsageAsync(DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ModelUsageStat>> GetModelStatsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DailyCostPoint>> GetDailyCostsAsync(int days = 7, CancellationToken cancellationToken = default);
}
```

### IBackendService

Location: `Services/IBackendService.cs`

```csharp
public interface IBackendService
{
    Task<BackendStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<bool> TestConnectionAsync(string backendUrl, CancellationToken cancellationToken = default);
}
```

## DTOs (Contracts)

All DTOs are in the `Contracts/` folder.

### ChatRequest

```json
{
    "chatId": "guid",
    "modelId": "string",
    "message": "string",
    "attachments": [
        {
            "id": "guid",
            "fileName": "string",
            "fileSize": 12345,
            "mimeType": "string",
            "base64Content": "string (optional)"
        }
    ],
    "systemPrompt": "string (optional)"
}
```

### ChatResponse

```json
{
    "chatId": "guid",
    "content": "string",
    "modelId": "string",
    "tokenUsage": {
        "inputTokens": 100,
        "outputTokens": 200
    },
    "costInfo": {
        "inputCost": 0.001,
        "outputCost": 0.003
    },
    "responseTimeMs": 1500
}
```

### TokenUsageDto

The backend MUST return actual token usage after processing the request. The frontend cannot determine real token counts.

```json
{
    "inputTokens": 100,
    "outputTokens": 200
}
```

### CostInfoDto

The backend MUST calculate costs based on the model pricing and actual token usage.

```json
{
    "inputCost": 0.001,
    "outputCost": 0.003
}
```

### ModelInfoDto

```json
{
    "id": "string",
    "displayName": "string",
    "provider": "string",
    "inputTokenPrice": 0.000003,
    "outputTokenPrice": 0.000015,
    "isAvailable": true,
    "contextWindow": 128000
}
```

### BackendStatusDto

```json
{
    "status": "Connected|Connecting|Disconnected|Error",
    "latencyMs": 142,
    "lastChecked": "2026-01-01T00:00:00Z",
    "errorMessage": "string (nullable)",
    "backendVersion": "1.0.0"
}
```

## Proposed API Endpoints

These are suggested endpoints. The frontend does NOT reference these URLs directly.

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/chat` | Send a message (non-streaming) |
| POST | `/api/chat/stream` | Send a message (SSE streaming) |
| GET | `/api/models` | Get available models |
| GET | `/api/usage` | Get usage statistics |
| GET | `/api/usage/models` | Get per-model usage stats |
| GET | `/api/usage/daily` | Get daily cost history |
| GET | `/api/backend/status` | Health check |
| POST | `/api/files` | Upload a file attachment |

## Streaming Protocol

For the streaming endpoint (`/api/chat/stream`), the backend should:

1. Accept a POST request with `ChatRequest` body
2. Return `Content-Type: text/event-stream`
3. Send chunks as SSE events:

```
data: {"chunk": "Hello"}

data: {"chunk": " world"}

data: {"chunk": "!"}

data: [DONE]
```

The frontend uses `IAsyncEnumerable<string>` internally. Each `string` yielded is one chunk.

## Token Usage

**Frontend behavior:** The frontend displays token counts received from the backend. It does NOT calculate tokens locally.

**Backend responsibility:** After processing a request, the backend must return the actual `TokenUsageDto` with:
- `inputTokens`: tokens in the user message
- `outputTokens`: tokens in the AI response
- `TotalTokens` (computed): `inputTokens + outputTokens`

## Cost Calculation

**Frontend behavior:** The frontend displays cost values received from the backend.

**Backend responsibility:** Calculate costs using:
- Model's `InputTokenPrice` per input token
- Model's `OutputTokenPrice` per output token
- Return the result as `CostInfoDto`

## Attachments

**Frontend behavior:**
- Users can attach files via button or drag & drop
- Frontend creates `Attachment` objects with metadata
- Frontend sends attachment metadata to backend via `ChatRequest`
- Frontend does NOT send files directly to AI providers

**Backend responsibility:**
- Receive file uploads via `/api/files`
- Store files temporarily
- Include file content in the AI provider request (if supported)
- Clean up temporary files

## Security

- **API keys** of AI providers must NEVER be in the desktop frontend
- All secrets and authorization are handled by the backend
- The frontend only communicates with the backend API
- Consider adding JWT/bearer token authentication

## Error Handling

The frontend expects:
- HTTP 200 for successful responses
- HTTP 4xx for client errors (invalid request, etc.)
- HTTP 5xx for server errors
- Streaming connections may drop; frontend handles `OperationCanceledException`

## Cancellation

The frontend supports cancellation via `CancellationToken`:
- User can click "Stop" during streaming
- Frontend cancels the HTTP request
- Backend should stop generation and clean up resources
