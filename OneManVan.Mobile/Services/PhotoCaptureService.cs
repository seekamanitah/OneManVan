using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Media;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Mobile.Services;

/// <summary>
/// Service for capturing and managing photos with metadata.
/// </summary>
public class PhotoCaptureService
{
    private readonly OneManVanDbContext _dbContext;
    private readonly string _photosDirectory;

    public PhotoCaptureService(OneManVanDbContext dbContext)
    {
        _dbContext = dbContext;
        _photosDirectory = Path.Combine(FileSystem.AppDataDirectory, "Photos");
        Directory.CreateDirectory(_photosDirectory);
    }

    /// <summary>
    /// Captures a photo for a job using the camera.
    /// </summary>
    public async Task<JobPhoto?> CaptureJobPhotoAsync(int jobId, PhotoType photoType, string? caption = null)
    {
        try
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                throw new NotSupportedException("Camera is not supported on this device.");
            }

            var photo = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
            {
                Title = $"Capture {photoType} Photo"
            });

            if (photo == null) return null;

            return await SaveJobPhotoAsync(jobId, photo, photoType, caption);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to capture photo: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Picks an existing photo from gallery for a job.
    /// </summary>
    public async Task<JobPhoto?> PickJobPhotoAsync(int jobId, PhotoType photoType, string? caption = null)
    {
        try
        {
            var photos = await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions
            {
                Title = "Select Photo"
            });

            var photo = photos?.FirstOrDefault();
            if (photo == null) return null;

            return await SaveJobPhotoAsync(jobId, photo, photoType, caption);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to pick photo: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Saves a photo file and creates JobPhoto record with metadata.
    /// </summary>
    private async Task<JobPhoto> SaveJobPhotoAsync(int jobId, FileResult fileResult, PhotoType photoType, string? caption)
    {
        // Generate unique filename
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var fileName = $"job_{jobId}_{timestamp}_{photoType}.jpg";
        var filePath = Path.Combine(_photosDirectory, fileName);

        // Save file
        using (var sourceStream = await fileResult.OpenReadAsync())
        using (var destStream = File.Create(filePath))
        {
            await sourceStream.CopyToAsync(destStream);
        }

        // Capture GPS location
        decimal? latitude = null;
        decimal? longitude = null;
        try
        {
            var location = await GetCurrentLocationAsync();
            if (location != null)
            {
                latitude = (decimal)location.Latitude;
                longitude = (decimal)location.Longitude;
            }
        }
        catch
        {
            // Location capture failed - continue without
        }

        // Create JobPhoto record
        var jobPhoto = new JobPhoto
        {
            JobId = jobId,
            FilePath = filePath,
            Type = photoType,
            Caption = caption,
            Latitude = latitude,
            Longitude = longitude,
            TakenAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.JobPhotos.Add(jobPhoto);
        await _dbContext.SaveChangesAsync();

        return jobPhoto;
    }

    /// <summary>
    /// Gets all photos for a job.
    /// </summary>
    public async Task<List<JobPhoto>> GetJobPhotosAsync(int jobId)
    {
        return await _dbContext.JobPhotos
            .Where(p => p.JobId == jobId)
            .OrderByDescending(p => p.TakenAt)
            .ToListAsync();
    }

    /// <summary>
    /// Deletes a job photo.
    /// </summary>
    public async Task DeleteJobPhotoAsync(int photoId)
    {
        var photo = await _dbContext.JobPhotos.FindAsync(photoId);
        if (photo != null)
        {
            // Delete file
            if (File.Exists(photo.FilePath))
            {
                File.Delete(photo.FilePath);
            }

            // Delete thumbnail if exists
            if (!string.IsNullOrEmpty(photo.ThumbnailPath) && File.Exists(photo.ThumbnailPath))
            {
                File.Delete(photo.ThumbnailPath);
            }

            _dbContext.JobPhotos.Remove(photo);
            await _dbContext.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Captures a photo for an asset.
    /// </summary>
    public async Task<AssetPhoto?> CaptureAssetPhotoAsync(int assetId, PhotoType photoType, string? caption = null, bool isPrimary = false)
    {
        try
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                throw new NotSupportedException("Camera is not supported on this device.");
            }

            var photo = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
            {
                Title = $"Capture {photoType} Photo"
            });

            if (photo == null) return null;

            return await SaveAssetPhotoAsync(assetId, photo, photoType, caption, isPrimary);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to capture photo: {ex.Message}", ex);
        }
    }

    private async Task<AssetPhoto> SaveAssetPhotoAsync(int assetId, FileResult fileResult, PhotoType photoType, string? caption, bool isPrimary)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var fileName = $"asset_{assetId}_{timestamp}_{photoType}.jpg";
        var filePath = Path.Combine(_photosDirectory, fileName);

        using (var sourceStream = await fileResult.OpenReadAsync())
        using (var destStream = File.Create(filePath))
        {
            await sourceStream.CopyToAsync(destStream);
        }

        // If setting as primary, unset existing primary
        if (isPrimary)
        {
            var existingPrimary = await _dbContext.AssetPhotos
                .Where(p => p.AssetId == assetId && p.IsPrimary)
                .ToListAsync();
            foreach (var p in existingPrimary)
            {
                p.IsPrimary = false;
            }
        }

        var assetPhoto = new AssetPhoto
        {
            AssetId = assetId,
            FilePath = filePath,
            Type = photoType,
            Caption = caption,
            IsPrimary = isPrimary,
            TakenAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.AssetPhotos.Add(assetPhoto);
        await _dbContext.SaveChangesAsync();

        return assetPhoto;
    }

    /// <summary>
    /// Gets current GPS location with timeout.
    /// </summary>
    private async Task<Location?> GetCurrentLocationAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (status != PermissionStatus.Granted)
            {
                return null;
            }

            return await Geolocation.Default.GetLocationAsync(new GeolocationRequest
            {
                DesiredAccuracy = GeolocationAccuracy.Medium,
                Timeout = TimeSpan.FromSeconds(10)
            });
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets total storage used by photos.
    /// </summary>
    public long GetTotalStorageUsed()
    {
        if (!Directory.Exists(_photosDirectory))
            return 0;

        return Directory.GetFiles(_photosDirectory)
            .Sum(f => new FileInfo(f).Length);
    }

    /// <summary>
    /// Cleans up orphaned photos (files without database records).
    /// </summary>
    public async Task CleanupOrphanedPhotosAsync()
    {
        if (!Directory.Exists(_photosDirectory)) return;

        var allPhotoPaths = new HashSet<string>();

        // Get all photo paths from database
        var jobPhotoPaths = await _dbContext.JobPhotos.Select(p => p.FilePath).ToListAsync();
        var assetPhotoPaths = await _dbContext.AssetPhotos.Select(p => p.FilePath).ToListAsync();
        var sitePhotoPaths = await _dbContext.SitePhotos.Select(p => p.FilePath).ToListAsync();

        foreach (var path in jobPhotoPaths.Concat(assetPhotoPaths).Concat(sitePhotoPaths))
        {
            allPhotoPaths.Add(path);
        }

        // Delete files not in database
        var files = Directory.GetFiles(_photosDirectory);
        foreach (var file in files)
        {
            if (!allPhotoPaths.Contains(file))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // Ignore deletion errors
                }
            }
        }
    }
}
