-- =====================================================
-- Add Document Library Migration (SQLite)
-- Version: 2024.03
-- Description: Creates Documents table for storing
--              service manuals, technical guides,
--              and business templates
-- =====================================================

CREATE TABLE IF NOT EXISTS Documents (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Description TEXT,
    Category INTEGER NOT NULL DEFAULT 0,
    Tags TEXT,
    SearchKeywords TEXT,
    
    -- File Information
    FileName TEXT NOT NULL,
    FilePath TEXT,
    FileContent BLOB,
    ContentType TEXT NOT NULL DEFAULT 'application/octet-stream',
    FileSizeBytes INTEGER NOT NULL DEFAULT 0,
    FileExtension TEXT,
    
    -- Organization
    FolderPath TEXT,
    SortOrder INTEGER NOT NULL DEFAULT 0,
    IsCustomDocument INTEGER NOT NULL DEFAULT 0,
    IsPrintable INTEGER NOT NULL DEFAULT 1,
    IsFavorite INTEGER NOT NULL DEFAULT 0,
    IsActive INTEGER NOT NULL DEFAULT 1,
    
    -- Associations
    Manufacturer TEXT,
    ModelNumber TEXT,
    EquipmentType TEXT,
    ProductId INTEGER,
    
    -- Tracking
    ViewCount INTEGER NOT NULL DEFAULT 0,
    LastAccessedAt TEXT,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    UpdatedAt TEXT,
    CreatedBy TEXT,
    UpdatedBy TEXT,
    
    FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE SET NULL
);

-- Indexes for efficient searching and filtering
CREATE INDEX IF NOT EXISTS IX_Documents_Name ON Documents(Name);
CREATE INDEX IF NOT EXISTS IX_Documents_Category ON Documents(Category);
CREATE INDEX IF NOT EXISTS IX_Documents_IsActive ON Documents(IsActive);
CREATE INDEX IF NOT EXISTS IX_Documents_IsFavorite ON Documents(IsFavorite);
CREATE INDEX IF NOT EXISTS IX_Documents_Manufacturer ON Documents(Manufacturer);
CREATE INDEX IF NOT EXISTS IX_Documents_EquipmentType ON Documents(EquipmentType);
CREATE INDEX IF NOT EXISTS IX_Documents_ProductId ON Documents(ProductId);
CREATE INDEX IF NOT EXISTS IX_Documents_CreatedAt ON Documents(CreatedAt);

-- Full-text search index (SQLite FTS5 if available)
-- Note: FTS requires separate setup, using LIKE for basic search

SELECT 'Document Library table created successfully' AS Status;
