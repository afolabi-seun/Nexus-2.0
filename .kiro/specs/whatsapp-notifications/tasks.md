# Implementation Plan: WhatsApp Notifications

## Overview

Add WhatsApp and SMS as notification channels to the existing UtilityService notification system using the Twilio API. Extends ProfileService with phone number field and channel toggles. Implementation is organized into UtilityService foundation, implementation, and templates, followed by ProfileService changes, environment config, and testing checkpoints.

## Tasks

- [ ] 1. UtilityService foundation — Twilio NuGet, AppSettings, NotificationChannels, ITwilioClient
  - [ ] 1.1 Add Twilio NuGet package and update NotificationChannels constants
    - Add `<PackageReference Include="Twilio" Version="7.*" />` to `UtilityService.Infrastructure/UtilityService.Infrastructure.csproj`
    - Add `WhatsApp` and `Sms` constants to `UtilityService.Domain/Helpers/NotificationChannels.cs`
    - Update the `All` array to include the two new channels
    - _Requirements: 1.2, 2.2_

  - [ ] 1.2 Add Twilio settings to AppSettings
    - Add `TwilioAccountSid`, `TwilioAuthToken`, `TwilioWhatsAppFrom`, `TwilioSmsFrom` properties to `UtilityService.Infrastructure/Configuration/AppSettings.cs`
    - Read from environment variables in `FromEnvironment()` using `??` defaulting to empty strings (not `GetRequired`)
    - _Requirements: 11.1, 11.2, 11.3_

  - [ ] 1.3 Create ITwilioClient interface
    - Create `UtilityService.Domain/Interfaces/Services/Notifications/ITwilioClient.cs`
    - Define `SendWhatsAppAsync(string toPhoneNumber, string templateName, List<string> templateParams, CancellationToken ct)` returning `Task<bool>`
    - Define `SendSmsAsync(string toPhoneNumber, string messageBody, CancellationToken ct)` returning `Task<bool>`
    - _Requirements: 1.1, 2.1, 3.1, 4.1_

  - [ ] 1.4 Add PhoneNumber field to DispatchNotificationRequest and update validator
    - Add optional `PhoneNumber` property (`string?`) to `UtilityService.Application/DTOs/Notifications/DispatchNotificationRequest.cs`
    - Update `ValidChannels` array in `UtilityService.Application/Validators/DispatchNotificationRequestValidator.cs` to include `"WhatsApp"` and `"SMS"`
    - _Requirements: 12.1, 1.2, 2.2_

- [ ] 2. Checkpoint — Verify foundation compiles
  - Ensure the solution builds with no errors after foundation changes, ask the user if questions arise.

- [ ] 3. UtilityService implementation — TwilioClient, NotificationDispatcher, NotificationService routing
  - [ ] 3.1 Implement TwilioClient service
    - Create `UtilityService.Infrastructure/Services/WhatsApp/TwilioClient.cs` implementing `ITwilioClient`
    - Inject `AppSettings` and `ILogger<TwilioClient>`
    - In constructor: check if `TwilioAccountSid` and `TwilioAuthToken` are non-empty; set internal `_isConfigured` flag; initialize Twilio SDK via `TwilioClient.Init` when configured
    - `SendWhatsAppAsync`: if not configured or `TwilioWhatsAppFrom` empty, log warning and return `false`; otherwise call `MessageResource.CreateAsync` with `from: "whatsapp:{From}"`, `to: "whatsapp:{toPhoneNumber}"`
    - `SendSmsAsync`: if not configured or `TwilioSmsFrom` empty, log warning and return `false`; otherwise call `MessageResource.CreateAsync` with `from: TwilioSmsFrom`, `to: toPhoneNumber`
    - Catch `ApiException` and `ApiConnectionException`, log structured error, return `false`
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 4.1, 4.2, 4.3, 4.4_

  - [ ] 3.2 Update INotificationDispatcher and NotificationDispatcher
    - Add `SendWhatsAppAsync` and `SendSmsAsync` methods to `UtilityService.Domain/Interfaces/Services/Notifications/INotificationDispatcher.cs`
    - Implement both methods in `UtilityService.Infrastructure/Services/Notifications/NotificationDispatcher.cs` by injecting `ITwilioClient` and delegating calls
    - Register `ITwilioClient` → `TwilioClient` in `UtilityService.Infrastructure/Configuration/DependencyInjection.cs`
    - _Requirements: 1.1, 2.1_

  - [ ] 3.3 Update NotificationService channel routing
    - Extend the `switch` expression in `DispatchAsync` in `UtilityService.Infrastructure/Services/Notifications/NotificationService.cs` to handle `NotificationChannels.WhatsApp` and `NotificationChannels.Sms`
    - For WhatsApp: skip with warning log if `PhoneNumber` is null/empty; otherwise call `_dispatcher.SendWhatsAppAsync` with phone number, notification type, and template variable values
    - For SMS: skip with warning log if `PhoneNumber` is null/empty; otherwise call `_dispatcher.SendSmsAsync` with phone number and rendered content
    - Extend the `switch` expression in `RetryFailedAsync` with the same WhatsApp and SMS branches
    - _Requirements: 1.3, 1.4, 2.3, 2.4, 7.1, 7.2, 7.3, 7.4, 7.5, 12.2, 12.3_

  - [ ] 3.4 Update TemplateRenderer for WhatsApp and SMS channels
    - Extend channel-to-path mapping in `UtilityService.Infrastructure/Services/Notifications/TemplateRenderer.cs` to resolve `WhatsApp` channel to `Templates/WhatsApp/{fileName}.txt` and `SMS` channel to `Templates/SMS/{fileName}.txt`
    - For WhatsApp templates: implement numbered placeholder substitution (`{{1}}`, `{{2}}`, ...) by mapping dictionary values to positional indices
    - For SMS templates: reuse existing named placeholder substitution (`{{VariableName}}`)
    - _Requirements: 5.1, 5.2, 5.4, 6.1, 6.2, 6.4_

- [ ] 4. Checkpoint — Verify UtilityService implementation compiles
  - Ensure the solution builds with no errors after implementation changes, ask the user if questions arise.

- [ ] 5. UtilityService templates — WhatsApp and SMS template files for all 8 notification types
  - [ ] 5.1 Create WhatsApp template files
    - Create directory `UtilityService.Infrastructure/Templates/WhatsApp/`
    - Create 8 `.txt` files using numbered placeholders (`{{1}}`, `{{2}}`, etc.): `story-assigned.txt`, `task-assigned.txt`, `sprint-started.txt`, `sprint-ended.txt`, `mentioned-in-comment.txt`, `story-status-changed.txt`, `task-status-changed.txt`, `due-date-approaching.txt`
    - Match placeholder positions to the template variables used in existing Push/Email templates
    - _Requirements: 5.1, 5.2, 5.3_

  - [ ] 5.2 Create SMS template files
    - Create directory `UtilityService.Infrastructure/Templates/SMS/`
    - Create 8 `.txt` files using named placeholders (`{{VariableName}}`): same 8 notification types as WhatsApp
    - Use concise plain-text format suitable for SMS character limits
    - _Requirements: 6.1, 6.2, 6.3_

- [ ] 6. ProfileService changes — TeamMember phone field, NotificationSetting fields, validation, migration
  - [ ] 6.1 Add WhatsAppPhoneNumber to TeamMember entity and DTOs
    - Add `WhatsAppPhoneNumber` property (`string?`, nullable) to `ProfileService.Domain/Entities/TeamMember.cs`
    - Add `WhatsAppPhoneNumber` field to `TeamMemberResponse`, `TeamMemberDetailResponse`, and `UpdateTeamMemberRequest` DTOs
    - Add E.164 validation rule to `UpdateTeamMemberRequestValidator`: regex `^\+[1-9]\d{6,14}$`, only when value is non-null
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5_

  - [ ] 6.2 Add IsWhatsApp and IsSms to NotificationSetting entity and DTOs
    - Add `IsWhatsApp` (default `false`) and `IsSms` (default `false`) boolean properties to `ProfileService.Domain/Entities/NotificationSetting.cs`
    - Add `IsWhatsApp` and `IsSms` fields to `NotificationSettingResponse` DTO
    - Add `IsWhatsApp` and `IsSms` fields to `UpdateNotificationSettingRequest` DTO
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5_

  - [ ] 6.3 Implement cross-field validation for WhatsApp/SMS toggles
    - Update the notification setting update logic: when `IsWhatsApp` or `IsSms` is `true`, validate that the `TeamMember` has a non-null `WhatsAppPhoneNumber`; return 400 validation error if missing
    - Update the team member update logic: when `WhatsAppPhoneNumber` is cleared to null, cascade-disable `IsWhatsApp` and `IsSms` on all `NotificationSetting` records for that team member while leaving `IsEmail`, `IsPush`, `IsInApp` unchanged
    - _Requirements: 10.1, 10.2_

  - [ ] 6.4 Create EF Core migration for new columns
    - Add EF Core migration for ProfileService adding `WhatsAppPhoneNumber` (nullable `varchar(16)`) to `team_members` table
    - Add `IsWhatsApp` (boolean, default `false`) and `IsSms` (boolean, default `false`) columns to `notification_settings` table
    - Update `ProfileDbContext` entity configuration if needed
    - _Requirements: 8.1, 9.1, 9.2_

- [ ] 7. Checkpoint — Verify ProfileService changes compile
  - Ensure the solution builds with no errors after ProfileService changes, ask the user if questions arise.

- [ ] 8. Environment configuration — Twilio env vars in all config files
  - [ ] 8.1 Add Twilio env vars to all utility-service config files
    - Add `TWILIO_ACCOUNT_SID`, `TWILIO_AUTH_TOKEN`, `TWILIO_WHATSAPP_FROM`, `TWILIO_SMS_FROM` placeholder entries to `config/development/utility-service.env`, `config/staging/utility-service.env`, `config/production/utility-service.env`
    - Add the same entries to `src/backend/UtilityService/UtilityService.Api/.env.example`
    - Use empty values for development/staging, `CHANGE_ME_*` placeholders for production
    - _Requirements: 11.4_

- [ ] 9. Checkpoint — Full build verification
  - Ensure the entire solution builds with no errors and all existing tests still pass, ask the user if questions arise.

- [ ]* 10. Property-based tests — UtilityService
  - [ ]* 10.1 Write property test for WhatsApp numbered placeholder substitution
    - **Property 1: WhatsApp Numbered Placeholder Substitution**
    - Create test in `UtilityService.Tests/Property/WhatsAppNotificationProperties.cs`
    - For any list of N string values matching a template's placeholder count, rendering SHALL replace all `{{n}}` placeholders with corresponding values and no numbered placeholders remain
    - **Validates: Requirements 5.2**

  - [ ]* 10.2 Write property test for SMS named placeholder substitution
    - **Property 2: SMS Named Placeholder Substitution**
    - Add test to `UtilityService.Tests/Property/WhatsAppNotificationProperties.cs`
    - For any dictionary of key-value pairs matching template placeholders, rendering SHALL replace all `{{Key}}` patterns with values and no known-key placeholders remain
    - **Validates: Requirements 6.2**

  - [ ]* 10.3 Write property test for channel dispatch independence
    - **Property 3: Channel Dispatch Independence**
    - Add test to `UtilityService.Tests/Property/WhatsAppNotificationProperties.cs`
    - For any dispatch request targeting multiple channels with any combination of success/failure, each channel's NotificationLog status SHALL reflect only that channel's own result
    - **Validates: Requirements 7.5**

  - [ ]* 10.4 Write property test for skip dispatch when phone number missing
    - **Property 8: Skip Dispatch When Phone Number Missing**
    - Add test to `UtilityService.Tests/Property/WhatsAppNotificationProperties.cs`
    - For any dispatch request with WhatsApp/SMS channel and null/empty PhoneNumber, the channel SHALL be skipped while other channels dispatch normally
    - **Validates: Requirements 12.2**

- [ ]* 11. Property-based tests — ProfileService
  - [ ]* 11.1 Write property test for E.164 phone number validation
    - **Property 4: E.164 Phone Number Validation**
    - Create test in `ProfileService.Tests/Property/WhatsAppNotificationProperties.cs`
    - For any string input, the validator SHALL accept if and only if it matches `^\+[1-9]\d{6,14}$`
    - **Validates: Requirements 8.2, 8.3, 8.4**

  - [ ]* 11.2 Write property test for notification setting channel flags persistence
    - **Property 5: Notification Setting Channel Flags Persistence**
    - Add test to `ProfileService.Tests/Property/WhatsAppNotificationProperties.cs`
    - For any combination of 5 boolean channel flags, persisting and reloading a NotificationSetting SHALL produce identical flag values
    - **Validates: Requirements 9.3**

  - [ ]* 11.3 Write property test for phone number prerequisite validation
    - **Property 6: Phone Number Prerequisite for WhatsApp/SMS Toggles**
    - Add test to `ProfileService.Tests/Property/WhatsAppNotificationProperties.cs`
    - For any update with IsWhatsApp=true or IsSms=true when TeamMember has null WhatsAppPhoneNumber, the update SHALL be rejected and existing settings unchanged
    - **Validates: Requirements 10.1**

  - [ ]* 11.4 Write property test for cascade disable on phone number removal
    - **Property 7: Cascade Disable on Phone Number Removal**
    - Add test to `ProfileService.Tests/Property/WhatsAppNotificationProperties.cs`
    - For any TeamMember with N notification settings, clearing WhatsAppPhoneNumber SHALL set IsWhatsApp=false and IsSms=false on all N settings while IsEmail, IsPush, IsInApp remain unchanged
    - **Validates: Requirements 10.2**

- [ ] 12. Final checkpoint — Full integration verification
  - Ensure all tests pass and the full solution builds cleanly, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- The design uses C# throughout — all implementation tasks target the existing .NET 8 / C# codebase
- Property tests use FsCheck.Xunit 3.3.2 (already in test projects)
- Twilio credentials default to empty strings for graceful degradation — the app starts without Twilio configured
- Checkpoints are placed after each logical group to catch issues incrementally
