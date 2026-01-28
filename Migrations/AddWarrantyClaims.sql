-- =====================================================
-- Add WarrantyClaims Table
-- Tracks warranty claims and repairs for assets
-- =====================================================

CREATE TABLE WarrantyClaims (
    Id INT PRIMARY KEY IDENTITY(1,1),
    
    -- Asset Link
    AssetId INT NOT NULL,
    
    -- Claim Identification
    ClaimNumber NVARCHAR(50) NULL,
    ClaimDate DATETIME NOT NULL DEFAULT GETDATE(),
    ResolvedDate DATETIME NULL,
    
    -- Issue Details
    IssueDescription NVARCHAR(2000) NOT NULL,
    Resolution NVARCHAR(2000) NULL,
    PartsReplaced NVARCHAR(1000) NULL,
    
    -- Warranty Coverage
    IsCoveredByWarranty BIT NOT NULL DEFAULT 1,
    NotCoveredReason NVARCHAR(500) NULL,
    
    -- Financial
    RepairCost DECIMAL(10,2) NOT NULL DEFAULT 0,
    CustomerCharge DECIMAL(10,2) NOT NULL DEFAULT 0,
    
    -- Status
    Status INT NOT NULL DEFAULT 0, -- 0=Pending, 1=UnderReview, 2=Approved, 3=Denied, 4=Completed, 5=Cancelled
    
    -- Job Link (Optional)
    JobId INT NULL,
    
    -- Manufacturer Response
    ManufacturerResponse NVARCHAR(2000) NULL,
    ManufacturerResponseDate DATETIME NULL,
    ManufacturerClaimNumber NVARCHAR(100) NULL,
    
    -- Additional Info
    TechnicianNotes NVARCHAR(2000) NULL,
    FiledBy NVARCHAR(100) NULL,
    
    -- Timestamps
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME NULL,
    
    -- Foreign Keys
    CONSTRAINT FK_WarrantyClaims_Assets FOREIGN KEY (AssetId) REFERENCES Assets(Id) ON DELETE CASCADE,
    CONSTRAINT FK_WarrantyClaims_Jobs FOREIGN KEY (JobId) REFERENCES Jobs(Id) ON DELETE SET NULL
);

-- Indexes
CREATE INDEX IX_WarrantyClaims_AssetId ON WarrantyClaims(AssetId);
CREATE INDEX IX_WarrantyClaims_ClaimNumber ON WarrantyClaims(ClaimNumber);
CREATE INDEX IX_WarrantyClaims_Status ON WarrantyClaims(Status);
CREATE INDEX IX_WarrantyClaims_ClaimDate ON WarrantyClaims(ClaimDate);
CREATE INDEX IX_WarrantyClaims_IsCoveredByWarranty ON WarrantyClaims(IsCoveredByWarranty);

GO

PRINT 'WarrantyClaims table created successfully';
