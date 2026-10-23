CREATE TABLE ThreadEvents (
    ThreadId TEXT,
    MessageId TEXT UNIQUE,
    MessageNumber INTEGER,
    EventType TEXT,
    Content TEXT,
    RepliedToMessage INTEGER,
    CreatedOn TEXT,
    PRIMARY KEY (ThreadId, MessageNumber)
) WITHOUT ROWID;