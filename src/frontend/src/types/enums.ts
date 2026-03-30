export enum StoryStatus {
    Backlog = 'Backlog',
    Ready = 'Ready',
    InProgress = 'InProgress',
    InReview = 'InReview',
    QA = 'QA',
    Done = 'Done',
    Closed = 'Closed',
}

export enum TaskStatus {
    ToDo = 'ToDo',
    InProgress = 'InProgress',
    InReview = 'InReview',
    Done = 'Done',
}

export enum SprintStatus {
    Planning = 'Planning',
    Active = 'Active',
    Completed = 'Completed',
    Cancelled = 'Cancelled',
}

export enum Priority {
    Critical = 'Critical',
    High = 'High',
    Medium = 'Medium',
    Low = 'Low',
}

export enum TaskType {
    Development = 'Development',
    Testing = 'Testing',
    DevOps = 'DevOps',
    Design = 'Design',
    Documentation = 'Documentation',
    Bug = 'Bug',
}

export enum LinkType {
    Blocks = 'Blocks',
    IsBlockedBy = 'IsBlockedBy',
    RelatesTo = 'RelatesTo',
    Duplicates = 'Duplicates',
}

export enum Role {
    OrgAdmin = 'OrgAdmin',
    DeptLead = 'DeptLead',
    Member = 'Member',
    Viewer = 'Viewer',
}

export enum Availability {
    Available = 'Available',
    Busy = 'Busy',
    Away = 'Away',
    Offline = 'Offline',
}

export enum Theme {
    Light = 'Light',
    Dark = 'Dark',
    System = 'System',
}

export enum DateFormat {
    ISO = 'ISO',
    US = 'US',
    EU = 'EU',
}

export enum TimeFormat {
    H24 = '24h',
    H12 = '12h',
}

export enum DigestFrequency {
    Realtime = 'Realtime',
    Hourly = 'Hourly',
    Daily = 'Daily',
    Off = 'Off',
}

export enum BoardView {
    Kanban = 'Kanban',
    Sprint = 'Sprint',
    Backlog = 'Backlog',
}

export enum NotificationType {
    StoryAssigned = 'StoryAssigned',
    TaskAssigned = 'TaskAssigned',
    SprintStarted = 'SprintStarted',
    SprintEnded = 'SprintEnded',
    MentionedInComment = 'MentionedInComment',
    StoryStatusChanged = 'StoryStatusChanged',
    TaskStatusChanged = 'TaskStatusChanged',
    DueDateApproaching = 'DueDateApproaching',
}

export enum FlgStatus {
    Active = 'A',
    Suspended = 'S',
    Deactivated = 'D',
}
