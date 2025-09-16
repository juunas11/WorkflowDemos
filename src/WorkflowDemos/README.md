# Workflow demos for a conference presentation

This repository contains a collection of workflow demos designed for a conference presentation.

Technologies used:

- Durable Functions
- Elsa
- MassTransit
- NServiceBus
- Temporal
- Logic Apps
- Mailgun
- Azure Content Safety
- Azure Table Storage

To run the demos, you will need either a Storage emulator or an actual Azure Storage account.
For the emulator, you can use [Azurite](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite).

You also need a Mailgun account to send emails.
And you need to set up Azure Content Safety for content moderation.

The Temporal demo requires a running Temporal server.
You can find the setup instructions [here](https://docs.temporal.io/develop/dotnet/set-up-your-local-dotnet#install-temporal-cli-and-start-the-development-server).

Durable Functions requires adding a `local.settings.json` file in the project like this:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=True",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "MailgunFromEmail": "<your-mailgun-sender-email>",
    "MailgunDomain": "<your-mailgun-domain>.mailgun.org",
    "MailgunApiKey": "<your-mailgun-api-key>",
    "AzureContentSafetyEndpoint": "https://<your-content-safety-resource>.cognitiveservices.azure.com/",
    "AzureContentSafetyApiKey": "<your-content-safety-api-key>",
    "StorageConnectionString": "UseDevelopmentStorage=True",
    "ModeratorEmail": "<your-email>",
    "ModerationPortalUrl": "https://localhost:7190",
    "APPLICATIONINSIGHTS_CONNECTION_STRING": "<your-application-insights-connection-string>"
  }
}
```

Most of the other projects use user secrets.
Something like this:

```json
{
  "ModeratorEmail": "<your-email>",
  "ModerationPortalUrl": "https://localhost:7190",
  "Mailgun": {
    "ApiKey": "<your-mailgun-api-key>",
    "FromEmail": "<your-mailgun-sender-email>",
    "Domain": "<your-mailgun-domain>.mailgun.org"
  },
  "AzureContentSafety": {
    "Endpoint": "https://<your-content-safety-resource>.cognitiveservices.azure.com/",
    "ApiKey": "<your-content-safety-api-key>"
  },
  "Storage": {
    "ConnectionString": "UseDevelopmentStorage=True"
  }
}
```

The Logic Apps can be setup using the JSON found in the WorkflowDemos.LogicApps folder.

The moderation portal requires some user secrets as well:

```json
{
  "Storage": {
    "ConnectionString": "UseDevelopmentStorage=True"
  },
  "LogicApps": {
    "ContentModerationWorkflowStartUrl": "<your-moderate-comments-logic-app-trigger-url>",
    "ModerationDecisionUrl": "<your-manual-moderation-logic-app-trigger-url>"
  },
  "PowerAutomate": {
    "ContentModerationWorkflowStartUrl": "<your-moderate-comments-power-automate-trigger-url>",
    "ModerationDecisionUrl": "<your-manual-moderation-power-automate-trigger-url>"
  }
}
```