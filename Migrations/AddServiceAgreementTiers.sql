-- =====================================================
-- Add Service Agreement Tiers Migration (SQLite)
-- Version: 2024.03
-- Description: Adds HVAC service tier fields to 
--              ServiceAgreements table for Basic/Standard/Premium plans
-- =====================================================

-- Add new columns to ServiceAgreements table
ALTER TABLE ServiceAgreements ADD COLUMN ServiceTier INTEGER NOT NULL DEFAULT 1;
ALTER TABLE ServiceAgreements ADD COLUMN NoEmergencyDispatchFee INTEGER NOT NULL DEFAULT 0;
ALTER TABLE ServiceAgreements ADD COLUMN FreeMinorAdjustments INTEGER NOT NULL DEFAULT 0;
ALTER TABLE ServiceAgreements ADD COLUMN FiltersIncludedPerYear INTEGER NOT NULL DEFAULT 0;
ALTER TABLE ServiceAgreements ADD COLUMN AdditionalCheckVisits INTEGER NOT NULL DEFAULT 0;
ALTER TABLE ServiceAgreements ADD COLUMN ResponseTimeHours INTEGER NOT NULL DEFAULT 48;
ALTER TABLE ServiceAgreements ADD COLUMN SpringAcTuneUpTasks TEXT;
ALTER TABLE ServiceAgreements ADD COLUMN FallHeatingTuneUpTasks TEXT;

-- Create index for service tier queries
CREATE INDEX IF NOT EXISTS IX_ServiceAgreements_ServiceTier ON ServiceAgreements(ServiceTier);

-- Update existing agreements to have default tier based on their current settings
UPDATE ServiceAgreements 
SET ServiceTier = CASE 
    WHEN IncludedVisitsPerYear >= 2 AND RepairDiscountPercent >= 20 THEN 2  -- Premium
    WHEN IncludedVisitsPerYear >= 2 AND RepairDiscountPercent >= 15 THEN 1  -- Standard
    ELSE 0  -- Basic
END
WHERE ServiceTier = 1;

-- Set default tune-up tasks for existing agreements
UPDATE ServiceAgreements 
SET SpringAcTuneUpTasks = '["Clean evaporator & condenser coils","Check refrigerant charge & pressures","Inspect/clean filters","Test electrical components & capacitors","Calibrate thermostat","Check blower & airflow","Clear condensate drain"]'
WHERE SpringAcTuneUpTasks IS NULL;

UPDATE ServiceAgreements 
SET FallHeatingTuneUpTasks = '["Inspect heat exchanger for cracks","Clean burners/ignition assembly","Check gas pressure & flue/vent","Inspect belts/pulleys","Measure temperature rise","Test safety controls"]'
WHERE FallHeatingTuneUpTasks IS NULL;

SELECT 'Service Agreement Tiers migration completed successfully' AS Status;
