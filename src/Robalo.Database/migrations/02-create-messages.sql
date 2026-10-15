CREATE TABLE Messages (
    ThreadId TEXT,
    MessageNumber INTEGER,
    Source TEXT,
    Content TEXT,
    CreatedOn TEXT,
    PRIMARY KEY (ThreadId, MessageNumber)
) WITHOUT ROWID;