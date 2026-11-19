CREATE TABLE ThreadEvents (
    ThreadId TEXT,
    MessageId TEXT UNIQUE,
    MessageNumber INTEGER,
    EventType TEXT,
    Content TEXT,
    RepliedToMessageId TEXT,
    ReferencedMessageIds TEXT,
    OriginalMessageId TEXT,
    CreatedOn TEXT,
    PRIMARY KEY (ThreadId, MessageNumber)
) WITHOUT ROWID;