# Requirements Document

## Introduction

WhatsApp is the dominant messaging platform for African dev teams and agencies — the primary target market for Nexus 2.0. This feature adds WhatsApp and SMS as notification channels in the existing UtilityService notification system, using the Twilio API for both WhatsApp Business messaging and SMS fallback. It extends the TeamMember profile in ProfileService with a phone number field and adds WhatsApp/SMS channel toggles to the existing per-type notification settings.

The existing notification infrastructure already supports multi-channel dispatch (Email, Push, InApp) with per-type user preferences, template rendering, delivery logging, and retry logic. This feature plugs WhatsApp and SMS into that architecture as two new channels.

## Glossary

- **Notification_Dispatcher**: The component in UtilityService responsible for sending notifications through specific channels (Email, Push, InApp, WhatsApp, SMS)
- **Notification_Service**: The orchestrator in UtilityService that routes notification requests to the Notification_Dispatcher for each enabled channel
- **Template_Renderer**: The component in UtilityService that renders notification content from templates and variable substitutions, selecting the appropriate template format per channel
- **Notification_Setting**: The per-user, per-notification-type channel toggle entity in ProfileService (boolean flags for each channel)
- **Team_Member**: The user profile entity in ProfileService containing contact information and organization membership
- **Twilio_Client**: The new component in UtilityService that wraps the Twilio SDK for sending WhatsApp messages and SMS
- **WhatsApp_Template**: A pre-approved Meta message template used for proactive WhatsApp Business messages, containing numbered placeholders (e.g., {{1}}, {{2}})
- **E164_Format**: The international phone number format required by Twilio (e.g., +2348012345678)
- **Notification_Log**: The entity in UtilityService that tracks delivery status, retry count, and metadata for each sent notification
- **Channel_Routing**: The logic that determines which channels to use for a notification based on the user's Notification_Setting preferences

## Requirements

### Requirement 1: WhatsApp Channel Registration

**User Story:** As a platform developer, I want WhatsApp registered as a notification channel, so that the dispatch system can route notifications through it.

#### Acceptance Criteria

1. THE Notification_Dispatcher SHALL expose a SendWhatsAppAsync method that accepts a recipient phone number, a template name, and a list of template parameter values
2. THE NotificationChannels constants class SHALL include a "WhatsApp" channel constant
3. WHEN the Notification_Service receives a dispatch request with the "WhatsApp" channel, THE Notification_Service SHALL invoke the Notification_Dispatcher SendWhatsAppAsync method
4. WHEN the Notification_Service retries a failed notification with the "WhatsApp" channel, THE Notification_Service SHALL invoke the Notification_Dispatcher SendWhatsAppAsync method

### Requirement 2: SMS Channel Registration

**User Story:** As a platform developer, I want SMS registered as a notification channel, so that users without WhatsApp can still receive mobile notifications.

#### Acceptance Criteria

1. THE Notification_Dispatcher SHALL expose a SendSmsAsync method that accepts a recipient phone number and a message body
2. THE NotificationChannels constants class SHALL include an "SMS" channel constant
3. WHEN the Notification_Service receives a dispatch request with the "SMS" channel, THE Notification_Service SHALL invoke the Notification_Dispatcher SendSmsAsync method
4. WHEN the Notification_Service retries a failed notification with the "SMS" channel, THE Notification_Service SHALL invoke the Notification_Dispatcher SendSmsAsync method

### Requirement 3: Twilio WhatsApp Integration

**User Story:** As a platform developer, I want the WhatsApp channel to send messages via the Twilio WhatsApp API, so that messages are delivered reliably through an approved business channel.

#### Acceptance Criteria

1. THE Twilio_Client SHALL send WhatsApp messages using the Twilio REST API with the "whatsapp:" prefix on sender and recipient numbers
2. THE Twilio_Client SHALL read credentials from environment variables: TWILIO_ACCOUNT_SID, TWILIO_AUTH_TOKEN, TWILIO_WHATSAPP_FROM
3. WHEN the Twilio API returns a successful response, THE Twilio_Client SHALL return true
4. WHEN the Twilio API returns an error response, THE Twilio_Client SHALL log the error details and return false
5. IF the TWILIO_ACCOUNT_SID or TWILIO_AUTH_TOKEN environment variable is missing, THEN THE Twilio_Client SHALL throw a configuration exception at startup

### Requirement 4: Twilio SMS Integration

**User Story:** As a platform developer, I want the SMS channel to send messages via the Twilio SMS API, so that users without WhatsApp receive notifications by text.

#### Acceptance Criteria

1. THE Twilio_Client SHALL send SMS messages using the Twilio REST API with the TWILIO_SMS_FROM number as the sender
2. THE Twilio_Client SHALL read the SMS sender number from the TWILIO_SMS_FROM environment variable
3. WHEN the Twilio SMS API returns a successful response, THE Twilio_Client SHALL return true
4. WHEN the Twilio SMS API returns an error response, THE Twilio_Client SHALL log the error details and return false

### Requirement 5: WhatsApp Message Templates

**User Story:** As a platform developer, I want pre-approved WhatsApp message templates for each notification type, so that proactive WhatsApp messages comply with Meta's template requirements.

#### Acceptance Criteria

1. THE Template_Renderer SHALL support a "WhatsApp" channel that resolves to WhatsApp-specific template files
2. THE Template_Renderer SHALL render WhatsApp templates by substituting numbered placeholders ({{1}}, {{2}}, {{3}}) with provided template variable values
3. THE Template_Renderer SHALL provide a WhatsApp template for each of the 8 notification types: StoryAssigned, TaskAssigned, SprintStarted, SprintEnded, MentionedInComment, StoryStatusChanged, TaskStatusChanged, DueDateApproaching
4. WHEN a WhatsApp template file is missing for a notification type, THE Template_Renderer SHALL throw a TemplateNotFoundException
5. FOR ALL valid WhatsApp templates, rendering a template and then parsing the output to extract parameter values SHALL produce values equivalent to the original input (round-trip property)

### Requirement 6: SMS Message Rendering

**User Story:** As a platform developer, I want plain-text SMS message rendering for each notification type, so that SMS notifications contain concise, readable content.

#### Acceptance Criteria

1. THE Template_Renderer SHALL support an "SMS" channel that resolves to SMS-specific plain-text template files
2. THE Template_Renderer SHALL render SMS templates by substituting named placeholders ({{VariableName}}) with provided template variable values
3. THE Template_Renderer SHALL provide an SMS template for each of the 8 notification types
4. WHEN an SMS template file is missing for a notification type, THE Template_Renderer SHALL throw a TemplateNotFoundException

### Requirement 7: Channel Routing Logic

**User Story:** As a platform developer, I want the notification service to route to WhatsApp and SMS channels based on user preferences, so that users receive notifications on their chosen channels.

#### Acceptance Criteria

1. WHEN a dispatch request includes the "WhatsApp" channel, THE Notification_Service SHALL use the recipient's stored phone number in E164_Format as the WhatsApp recipient
2. WHEN a dispatch request includes the "SMS" channel, THE Notification_Service SHALL use the recipient's stored phone number in E164_Format as the SMS recipient
3. THE Notification_Service SHALL log each WhatsApp and SMS dispatch attempt in the Notification_Log with the correct channel value
4. WHEN a WhatsApp or SMS dispatch fails, THE Notification_Service SHALL mark the Notification_Log status as "Failed" and the existing retry mechanism SHALL process the retry
5. FOR ALL combinations of enabled channels in a dispatch request, THE Notification_Service SHALL dispatch to each enabled channel independently without one channel failure affecting the others

### Requirement 8: Phone Number on Team Member Profile

**User Story:** As a team member, I want to add my WhatsApp phone number to my profile, so that I can receive WhatsApp and SMS notifications.

#### Acceptance Criteria

1. THE Team_Member entity SHALL include an optional WhatsAppPhoneNumber field
2. THE Team_Member WhatsAppPhoneNumber field SHALL store phone numbers in E164_Format
3. WHEN a team member updates their profile with a WhatsAppPhoneNumber, THE ProfileService SHALL validate that the number matches E164_Format (a "+" followed by 7 to 15 digits)
4. IF a team member provides a phone number that does not match E164_Format, THEN THE ProfileService SHALL return a validation error
5. WHEN a team member clears their WhatsAppPhoneNumber, THE ProfileService SHALL set the field to null

### Requirement 9: Notification Setting Channel Toggles

**User Story:** As a team member, I want to enable or disable WhatsApp and SMS notifications per notification type, so that I control which notifications I receive on each channel.

#### Acceptance Criteria

1. THE Notification_Setting entity SHALL include an IsWhatsApp boolean field defaulting to false
2. THE Notification_Setting entity SHALL include an IsSms boolean field defaulting to false
3. WHEN a team member updates their notification settings, THE ProfileService SHALL persist the IsWhatsApp and IsSms values alongside the existing IsEmail, IsPush, and IsInApp values
4. THE NotificationSettingResponse DTO SHALL include IsWhatsApp and IsSms fields
5. THE UpdateNotificationSettingRequest DTO SHALL include IsWhatsApp and IsSms fields

### Requirement 10: WhatsApp Channel Prerequisite Validation

**User Story:** As a team member, I want the system to prevent enabling WhatsApp or SMS notifications when I have no phone number on file, so that I don't miss notifications due to misconfiguration.

#### Acceptance Criteria

1. WHEN a team member enables IsWhatsApp or IsSms on any notification type and the team member has no WhatsAppPhoneNumber stored, THEN THE ProfileService SHALL return a validation error indicating a phone number is required
2. WHEN a team member clears their WhatsAppPhoneNumber while having IsWhatsApp or IsSms enabled on any notification type, THEN THE ProfileService SHALL disable IsWhatsApp and IsSms on all notification types for that team member

### Requirement 11: Twilio Configuration in AppSettings

**User Story:** As a platform operator, I want Twilio credentials managed through environment variables, so that secrets are not hardcoded and can vary per deployment environment.

#### Acceptance Criteria

1. THE AppSettings class SHALL include properties for TwilioAccountSid, TwilioAuthToken, TwilioWhatsAppFrom, and TwilioSmsFrom
2. THE AppSettings FromEnvironment method SHALL read TWILIO_ACCOUNT_SID, TWILIO_AUTH_TOKEN, TWILIO_WHATSAPP_FROM, and TWILIO_SMS_FROM from environment variables
3. WHEN TWILIO_WHATSAPP_FROM or TWILIO_SMS_FROM is not set, THE AppSettings SHALL default the values to empty strings to allow graceful degradation
4. THE utility-service environment configuration files SHALL include placeholder entries for all four Twilio environment variables

### Requirement 12: Notification Dispatch Request Phone Number

**User Story:** As a platform developer, I want the dispatch request to carry the recipient's phone number, so that WhatsApp and SMS channels have the information needed to deliver messages.

#### Acceptance Criteria

1. THE DispatchNotificationRequest DTO SHALL include an optional PhoneNumber field
2. WHEN the Notification_Service dispatches to the "WhatsApp" or "SMS" channel and the PhoneNumber field is null or empty, THE Notification_Service SHALL skip that channel and log a warning
3. WHEN the Notification_Service dispatches to the "Email" channel, THE Notification_Service SHALL continue to use the existing Recipient field as the email address
