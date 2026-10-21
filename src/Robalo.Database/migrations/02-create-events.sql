CREATE TABLE ThreadEvents (
    ThreadId TEXT,
    MessageNumber INTEGER,
    EventType TEXT,
    Content TEXT,
    RepliedToMessage INTEGER,
    CreatedOn TEXT,
    PRIMARY KEY (ThreadId, MessageNumber)
) WITHOUT ROWID;