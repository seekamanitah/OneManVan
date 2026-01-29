-- Add QuickNotes table for quick note-taking feature

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'QuickNotes')
BEGIN
    CREATE TABLE QuickNotes (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Title NVARCHAR(200) NULL,
        Content NVARCHAR(MAX) NOT NULL,
        Category NVARCHAR(50) NULL,
        IsPinned BIT NOT NULL DEFAULT 0,
        IsArchived BIT NOT NULL DEFAULT 0,
        Color NVARCHAR(20) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );

    PRINT 'QuickNotes table created successfully';
END
ELSE
BEGIN
    PRINT 'QuickNotes table already exists';
END
GO
