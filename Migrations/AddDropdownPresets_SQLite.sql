-- Migration: Add DropdownPresets table for configurable dropdown options
-- Apply to: SQLite

-- Create DropdownPresets table
CREATE TABLE IF NOT EXISTS DropdownPresets (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Category TEXT NOT NULL,
    DisplayValue TEXT NOT NULL,
    StoredValue TEXT NULL,
    SortOrder INTEGER NOT NULL DEFAULT 0,
    IsActive INTEGER NOT NULL DEFAULT 1,
    IsSystemDefault INTEGER NOT NULL DEFAULT 0,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now'))
);

-- Index for fast category lookups
CREATE INDEX IF NOT EXISTS IX_DropdownPresets_Category ON DropdownPresets(Category, IsActive, SortOrder);

-- Seed default presets

-- Manufacturers (common HVAC brands)
INSERT OR IGNORE INTO DropdownPresets (Category, DisplayValue, SortOrder, IsSystemDefault) VALUES
('Manufacturer', 'Carrier', 1, 1),
('Manufacturer', 'Trane', 2, 1),
('Manufacturer', 'Lennox', 3, 1),
('Manufacturer', 'Rheem', 4, 1),
('Manufacturer', 'Goodman', 5, 1),
('Manufacturer', 'Daikin', 6, 1),
('Manufacturer', 'York', 7, 1),
('Manufacturer', 'American Standard', 8, 1),
('Manufacturer', 'Bryant', 9, 1),
('Manufacturer', 'Ruud', 10, 1),
('Manufacturer', 'Amana', 11, 1),
('Manufacturer', 'Heil', 12, 1),
('Manufacturer', 'Mitsubishi', 13, 1),
('Manufacturer', 'Fujitsu', 14, 1),
('Manufacturer', 'LG', 15, 1);

-- Refrigerant Types
INSERT OR IGNORE INTO DropdownPresets (Category, DisplayValue, StoredValue, SortOrder, IsSystemDefault) VALUES
('RefrigerantType', 'R-22 (Legacy)', 'R22', 1, 1),
('RefrigerantType', 'R-410A', 'R410A', 2, 1),
('RefrigerantType', 'R-32', 'R32', 3, 1),
('RefrigerantType', 'R-407C', 'R407C', 4, 1),
('RefrigerantType', 'R-134A', 'R134A', 5, 1),
('RefrigerantType', 'R-454B (Opteon XL41)', 'R454B', 6, 1),
('RefrigerantType', 'R-290 (Propane)', 'R290', 7, 1);

-- Warranty Lengths (in years)
INSERT OR IGNORE INTO DropdownPresets (Category, DisplayValue, StoredValue, SortOrder, IsSystemDefault) VALUES
('WarrantyLength', '1 Year', '1', 1, 1),
('WarrantyLength', '2 Years', '2', 2, 1),
('WarrantyLength', '3 Years', '3', 3, 1),
('WarrantyLength', '5 Years', '5', 4, 1),
('WarrantyLength', '7 Years', '7', 5, 1),
('WarrantyLength', '10 Years', '10', 6, 1),
('WarrantyLength', '12 Years', '12', 7, 1),
('WarrantyLength', '15 Years', '15', 8, 1),
('WarrantyLength', 'Lifetime', '99', 9, 1);

-- Payment Terms
INSERT OR IGNORE INTO DropdownPresets (Category, DisplayValue, StoredValue, SortOrder, IsSystemDefault) VALUES
('PaymentTerms', 'Due on Receipt', '0', 1, 1),
('PaymentTerms', 'Net 15', '15', 2, 1),
('PaymentTerms', 'Net 30', '30', 3, 1),
('PaymentTerms', 'Net 45', '45', 4, 1),
('PaymentTerms', 'Net 60', '60', 5, 1),
('PaymentTerms', '50% Deposit Required', 'DEPOSIT50', 6, 1);

-- Fuel Types
INSERT OR IGNORE INTO DropdownPresets (Category, DisplayValue, StoredValue, SortOrder, IsSystemDefault) VALUES
('FuelType', 'Natural Gas', 'NaturalGas', 1, 1),
('FuelType', 'Propane (LP)', 'Propane', 2, 1),
('FuelType', 'Electric', 'Electric', 3, 1),
('FuelType', 'Oil', 'Oil', 4, 1),
('FuelType', 'Dual Fuel', 'DualFuel', 5, 1);
