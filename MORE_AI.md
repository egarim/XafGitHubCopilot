# More AI: Enabling AI-Powered Reporting in XAF

This document covers the DevExpress AI-powered reporting extensions available in v25.2 and how to activate them in this project.

## Current State

The project has **ReportsV2 infrastructure** fully set up but with **no AI features enabled**:

- `DevExpress.ExpressApp.ReportsV2` packages installed (Module, Blazor, Win)
- `ReportsModuleV2` registered in `Module.cs`
- Inplace reports enabled with XML storage mode
- `ReportDataV2` DbSet in the EF Core context
- `ReportDataV2` excluded from Copilot SDK schema discovery (intentionally — report definitions are not business data)

The XAF runtime Report Designer and Report Viewer work out of the box, but none of the AI-powered extensions are wired up.

## What DevExpress 25.2 Offers

DevExpress ships a comprehensive set of AI-powered report extensions in v25.2. All follow the **"bring your own key"** principle — DevExpress does not ship LLMs; you register an `IChatClient` from `Microsoft.Extensions.AI` at startup.

### Blazor / Web Report Designer

| Feature | Status | Description |
|---------|--------|-------------|
| **Prompt-to-Report** (CTP) | Not enabled | Generate report layouts from natural language in the Report Wizard |
| **AI Expression Editor** | Not enabled | Write Criteria Language expressions in plain text |
| **AI-generated Test Data** | Not enabled | Preview reports with LLM-generated sample data (no live data source needed) |
| **Report Localization** | Not enabled | Translate report elements to any language via AI |
| **Summarize & Translate** (Blazor Report Viewer) | Not enabled | Summarize or translate a rendered report in the viewer |

### WinForms / Desktop Report Designer

| Feature | Status | Description |
|---------|--------|-------------|
| **Prompt-to-Report** (CTP) | Not enabled | Same as above, for the WinForms designer |
| **Modify Report via Chat** (CTP) | Not enabled | AI Assistant panel — chat to add/remove bands, controls, adjust layout, group/sort/filter |
| **AI Expression Editor** | Not enabled | Natural language to Criteria Language expressions |
| **AI-generated Test Data** | Not enabled | Preview reports with LLM-generated sample data |
| **Report Localization** | Not enabled | Translate report elements via AI |
| **Summarize & Translate** (WinForms Document Viewer) | Not enabled | Summarize or translate rendered reports |
| **Inline Translation** (WinForms Document Viewer) | Not enabled | Translate reports inline in the viewer |

### Visual Studio Report Designer

These features are part of the **DevExpress AI-powered Assistant** VS extension (installed via the DevExpress Unified Component Installer) and work at design time:

- AI-powered Report Generation (Prompt-to-Report in the Report Wizard)
- AI-powered Report Localization
- Generate Expressions from Prompts
- Preview Reports with AI-generated Test Data

## How to Enable (Blazor Server)

### Step 1: Install NuGet Packages

Add to `XafGitHubCopilot.Blazor.Server.csproj`:

```xml
<PackageReference Include="DevExpress.AIIntegration.AspNetCore.Reporting" Version="25.2.*" />
<PackageReference Include="Microsoft.Extensions.AI" Version="9.7.1" />
<PackageReference Include="Microsoft.Extensions.AI.OpenAI" Version="9.7.1-preview.1.25365.4" />
<PackageReference Include="Azure.AI.OpenAI" Version="2.2.0-beta.5" />
```

> Substitute the Azure.AI.OpenAI package with your preferred provider if not using Azure OpenAI.

### Step 2: Register the AI Client and Reporting Extensions

In `Startup.cs`, after `services.AddXaf(...)`:

```csharp
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using DevExpress.AIIntegration.Reporting;

// Register an IChatClient for the reporting extensions
string azureEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
string azureKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
string modelName = "gpt-4o";

IChatClient chatClient = new AzureOpenAIClient(
    new Uri(azureEndpoint),
    new System.ClientModel.ApiKeyCredential(azureKey))
    .GetChatClient(modelName).AsIChatClient();

services.AddSingleton(chatClient);

services.AddDevExpressAI(config => {
    config.AddWebReportingAIIntegration(aiConfig => {
        // Prompt-to-Report in the Report Wizard
        aiConfig.AddPromptToReportConverter(options => {
            options.FixLayoutErrors()
                   .SetRetryAttemptCount(5);
        });

        // Add more AI features here as needed:
        // aiConfig.AddReportLocalization();
        // aiConfig.AddSummarizeAndTranslate();
        // aiConfig.AddExpressionGenerator();
        // aiConfig.AddTestDataSource();
    });
});
```

### Step 3: Use It

1. Navigate to **Reports** in the XAF sidebar
2. Click **New** to open the Report Wizard
3. The wizard now shows an **AI Prompt-to-Report** option
4. Describe the report in natural language and click **Finish**

## How to Enable (WinForms)

### Step 1: Install NuGet Packages

Add to `XafGitHubCopilot.Win.csproj`:

```xml
<PackageReference Include="DevExpress.AIIntegration.WinForms.Reporting" Version="25.2.*" />
<PackageReference Include="DevExpress.Win.Design" Version="25.2.*" />
<PackageReference Include="Microsoft.Extensions.AI.OpenAI" Version="9.7.1-preview.1.25365.4" />
<PackageReference Include="Azure.AI.OpenAI" Version="2.2.0-beta.5" />
```

### Step 2: Register the AI Client

Register an `IChatClient` at application startup using `AIExtensionsContainerDesktop.Default`:

```csharp
using Azure.AI.OpenAI;
using DevExpress.AIIntegration;
using Microsoft.Extensions.AI;

var chatClient = new AzureOpenAIClient(
    new Uri(azureEndpoint),
    new System.ClientModel.ApiKeyCredential(azureKey))
    .GetChatClient(modelName).AsIChatClient();

AIExtensionsContainerDesktop.Default.RegisterChatClient(chatClient);
```

### Step 3: Attach AI Behaviors to the Report Designer

Use a `BehaviorManager` to attach AI behaviors:

```csharp
using DevExpress.AIIntegration.WinForms.Reporting;

// Prompt-to-Report
behaviorManager.Attach<ReportPromptToReportBehavior>(reportDesigner, behavior => {
    behavior.Properties.FixLayoutErrors = true;
    behavior.Properties.Temperature = 0.5f;
    behavior.Properties.RetryAttemptCount = 2;
});

// Modify Report via Chat (CTP)
behaviorManager.Attach<ReportModifyBehavior>(reportDesigner.DesignRibbonForm.DesignMdiController, behavior => {
    behavior.Properties.FixLayoutErrors = true;
    behavior.Properties.Temperature = 0.5f;
    behavior.Properties.RetryAttemptCount = 2;
});

// Localization
behaviorManager.Attach<ReportLocalizationBehavior>(reportDesigner, behavior => { });
```

## Architecture Note: Two Separate AI Pipelines

This project has **two independent AI integrations**:

1. **GitHub Copilot SDK** — Powers the in-app chat assistant (`CopilotChatService`). Uses the Copilot SDK session model with function calling for data queries and record creation. This is the core feature of the project.

2. **DevExpress AI Reporting Extensions** — Powers the Report Designer and Viewer AI features. Uses `Microsoft.Extensions.AI` `IChatClient` registered in DI, connecting directly to Azure OpenAI / OpenAI.

These two pipelines are independent. Enabling AI reporting does not affect the Copilot chat assistant, and vice versa. They can use different models and different authentication.

## DevExpress Documentation References

- [AI-powered Extensions for DevExpress Reporting](https://docs.devexpress.com/XtraReports/405211) — Overview of all AI reporting features
- [Generate Reports From Prompts (Web Report Designer)](https://docs.devexpress.com/XtraReports/405485) — Blazor/ASP.NET Core setup guide
- [Summarize and Translate (Blazor Report Viewer)](https://docs.devexpress.com/XtraReports/405197) — Blazor viewer AI features
- [Modify Report Behavior (WinForms)](https://docs.devexpress.com/XtraReports/405498) — WinForms chat-to-modify-report
- [Prompt to Report (WinForms)](https://docs.devexpress.com/XtraReports/405460) — WinForms prompt-to-report
- [AI-powered Report Localization (WinForms)](https://docs.devexpress.com/XtraReports/405435) — WinForms report localization
- [DevExpress AI-powered Extensions for Blazor](https://docs.devexpress.com/Blazor/405228) — Blazor AI overview (includes reporting, HTML editor, Rich Text, Memo)
- [XAF: Create and View Reports (Blazor)](https://docs.devexpress.com/eXpressAppFramework/402306) — XAF-specific report setup
- [v25.2 Release Notes (Reporting)](https://docs.devexpress.com/XtraReports/405279) — Full list of AI reporting features in v25.2
