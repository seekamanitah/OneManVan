using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Shared.Services;

/// <summary>
/// Service for optimizing job routes and calculating distances.
/// Uses simple distance calculations for offline use, or can integrate with mapping APIs.
/// </summary>
public class RouteOptimizationService
{
    private readonly IDbContextFactory<OneManVanDbContext> _contextFactory;

    public RouteOptimizationService(IDbContextFactory<OneManVanDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <summary>
    /// Gets jobs for a specific date with their GPS coordinates.
    /// </summary>
    public async Task<List<RouteStop>> GetJobsForDateAsync(DateTime date)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var jobs = await context.Jobs
            .Include(j => j.Customer)
            .Include(j => j.Site)
            .Where(j => j.ScheduledDate.HasValue && j.ScheduledDate.Value.Date == date.Date)
            .Where(j => j.Status == JobStatus.Scheduled || j.Status == JobStatus.Draft || j.Status == JobStatus.InProgress)
            .OrderBy(j => j.ScheduledDate)
            .AsNoTracking()
            .ToListAsync();
        
        return jobs.Select(j => new RouteStop
        {
            JobId = j.Id,
            JobNumber = j.JobNumber ?? $"J-{j.Id}",
            Title = j.Title,
            CustomerName = j.Customer?.DisplayName ?? "Unknown",
            Address = GetJobAddress(j),
            Latitude = j.Site?.Latitude,
            Longitude = j.Site?.Longitude,
            ScheduledTime = j.ScheduledDate,
            EstimatedDuration = TimeSpan.FromHours((double)j.EstimatedHours),
            Priority = j.Priority,
            Status = j.Status
        }).ToList();
    }

    /// <summary>
    /// Optimizes route order using nearest neighbor algorithm.
    /// </summary>
    public RouteOptimizationResult OptimizeRoute(List<RouteStop> stops, decimal? startLat = null, decimal? startLon = null)
    {
        if (stops.Count <= 1)
        {
            return new RouteOptimizationResult
            {
                OptimizedStops = stops,
                TotalDistanceMiles = 0,
                TotalDurationMinutes = stops.Sum(s => (int)s.EstimatedDuration.TotalMinutes),
                Savings = 0
            };
        }

        // Filter to stops with valid coordinates
        var stopsWithCoords = stops.Where(s => s.Latitude.HasValue && s.Longitude.HasValue).ToList();
        var stopsWithoutCoords = stops.Where(s => !s.Latitude.HasValue || !s.Longitude.HasValue).ToList();

        if (stopsWithCoords.Count <= 1)
        {
            // Not enough coordinates to optimize
            return new RouteOptimizationResult
            {
                OptimizedStops = stops,
                TotalDistanceMiles = 0,
                TotalDurationMinutes = stops.Sum(s => (int)s.EstimatedDuration.TotalMinutes),
                Savings = 0,
                Message = "Not enough GPS coordinates for route optimization"
            };
        }

        // Calculate original distance
        var originalDistance = CalculateTotalDistance(stopsWithCoords);

        // Use nearest neighbor algorithm
        var optimized = NearestNeighborOptimization(stopsWithCoords, startLat, startLon);

        // Calculate optimized distance
        var optimizedDistance = CalculateTotalDistance(optimized);

        // Estimate driving time (average 30 mph in urban areas)
        var drivingTimeMinutes = (int)(optimizedDistance / 30 * 60);

        // Add stops without coordinates at the end
        optimized.AddRange(stopsWithoutCoords);

        // Assign route order
        for (int i = 0; i < optimized.Count; i++)
        {
            optimized[i].RouteOrder = i + 1;
        }

        return new RouteOptimizationResult
        {
            OptimizedStops = optimized,
            TotalDistanceMiles = Math.Round(optimizedDistance, 1),
            TotalDurationMinutes = drivingTimeMinutes + stops.Sum(s => (int)s.EstimatedDuration.TotalMinutes),
            DrivingTimeMinutes = drivingTimeMinutes,
            Savings = Math.Round(originalDistance - optimizedDistance, 1),
            OriginalDistanceMiles = Math.Round(originalDistance, 1),
            Message = stopsWithoutCoords.Any() 
                ? $"{stopsWithoutCoords.Count} stop(s) missing GPS coordinates"
                : "Route optimized successfully"
        };
    }

    /// <summary>
    /// Nearest neighbor algorithm for route optimization.
    /// </summary>
    private List<RouteStop> NearestNeighborOptimization(List<RouteStop> stops, decimal? startLat, decimal? startLon)
    {
        var result = new List<RouteStop>();
        var remaining = new List<RouteStop>(stops);

        // Start from provided location or first stop
        decimal currentLat = startLat ?? stops[0].Latitude!.Value;
        decimal currentLon = startLon ?? stops[0].Longitude!.Value;

        while (remaining.Any())
        {
            // Find nearest stop
            RouteStop? nearest = null;
            double minDistance = double.MaxValue;

            foreach (var stop in remaining)
            {
                var distance = CalculateDistance(currentLat, currentLon, 
                    stop.Latitude!.Value, stop.Longitude!.Value);
                
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = stop;
                }
            }

            if (nearest != null)
            {
                result.Add(nearest);
                currentLat = nearest.Latitude!.Value;
                currentLon = nearest.Longitude!.Value;
                remaining.Remove(nearest);
            }
        }

        return result;
    }

    /// <summary>
    /// Calculates total distance for a route.
    /// </summary>
    private double CalculateTotalDistance(List<RouteStop> stops)
    {
        if (stops.Count < 2) return 0;

        double total = 0;
        for (int i = 0; i < stops.Count - 1; i++)
        {
            total += CalculateDistance(
                stops[i].Latitude!.Value, stops[i].Longitude!.Value,
                stops[i + 1].Latitude!.Value, stops[i + 1].Longitude!.Value);
        }
        return total;
    }

    /// <summary>
    /// Calculates distance between two GPS points using Haversine formula.
    /// </summary>
    public double CalculateDistance(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
    {
        const double R = 3959; // Earth's radius in miles

        var dLat = ToRadians((double)(lat2 - lat1));
        var dLon = ToRadians((double)(lon2 - lon1));

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians((double)lat1)) * Math.Cos(ToRadians((double)lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c;
    }

    private double ToRadians(double degrees) => degrees * Math.PI / 180;

    /// <summary>
    /// Gets the best address for a job.
    /// </summary>
    private string GetJobAddress(Job job)
    {
        if (job.Site != null)
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(job.Site.Address)) parts.Add(job.Site.Address);
            if (!string.IsNullOrEmpty(job.Site.City)) parts.Add(job.Site.City);
            if (!string.IsNullOrEmpty(job.Site.State)) parts.Add(job.Site.State);
            if (!string.IsNullOrEmpty(job.Site.ZipCode)) parts.Add(job.Site.ZipCode);
            return string.Join(", ", parts);
        }
        
        return job.Customer?.HomeAddress ?? "Address not available";
    }

    /// <summary>
    /// Generates a Google Maps URL for the optimized route.
    /// </summary>
    public string GenerateGoogleMapsUrl(List<RouteStop> stops, decimal? startLat = null, decimal? startLon = null)
    {
        var stopsWithCoords = stops.Where(s => s.Latitude.HasValue && s.Longitude.HasValue).ToList();
        
        if (!stopsWithCoords.Any())
            return string.Empty;

        var baseUrl = "https://www.google.com/maps/dir/";
        var waypoints = new List<string>();

        // Add starting point if provided
        if (startLat.HasValue && startLon.HasValue)
        {
            waypoints.Add($"{startLat},{startLon}");
        }

        // Add all stops
        foreach (var stop in stopsWithCoords.OrderBy(s => s.RouteOrder))
        {
            waypoints.Add($"{stop.Latitude},{stop.Longitude}");
        }

        return baseUrl + string.Join("/", waypoints);
    }

    /// <summary>
    /// Gets jobs for the next N days grouped by date.
    /// </summary>
    public async Task<Dictionary<DateTime, List<RouteStop>>> GetUpcomingJobsAsync(int days = 7)
    {
        var result = new Dictionary<DateTime, List<RouteStop>>();
        var today = DateTime.Today;

        for (int i = 0; i < days; i++)
        {
            var date = today.AddDays(i);
            var jobs = await GetJobsForDateAsync(date);
            if (jobs.Any())
            {
                result[date] = jobs;
            }
        }

        return result;
    }
}

#region Route Models

public class RouteStop
{
    public int JobId { get; set; }
    public string JobNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public DateTime? ScheduledTime { get; set; }
    public TimeSpan EstimatedDuration { get; set; }
    public JobPriority Priority { get; set; }
    public JobStatus Status { get; set; }
    public int RouteOrder { get; set; }
    
    public bool HasCoordinates => Latitude.HasValue && Longitude.HasValue;
    
    public string GoogleMapsUrl => HasCoordinates 
        ? $"https://www.google.com/maps/search/?api=1&query={Latitude},{Longitude}"
        : string.Empty;
}

public class RouteOptimizationResult
{
    public List<RouteStop> OptimizedStops { get; set; } = new();
    public double TotalDistanceMiles { get; set; }
    public int TotalDurationMinutes { get; set; }
    public int DrivingTimeMinutes { get; set; }
    public double OriginalDistanceMiles { get; set; }
    public double Savings { get; set; }
    public string? Message { get; set; }
    
    public string FormattedTotalTime
    {
        get
        {
            var hours = TotalDurationMinutes / 60;
            var minutes = TotalDurationMinutes % 60;
            return hours > 0 ? $"{hours}h {minutes}m" : $"{minutes}m";
        }
    }
    
    public string FormattedDrivingTime
    {
        get
        {
            var hours = DrivingTimeMinutes / 60;
            var minutes = DrivingTimeMinutes % 60;
            return hours > 0 ? $"{hours}h {minutes}m" : $"{minutes}m";
        }
    }
}

#endregion
