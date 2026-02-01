# PWA Icon Generator for OneManVan
# This script generates PNG icons from the base SVG using ImageMagick or .NET
# Run this script to generate all required PWA icon sizes

$sizes = @(72, 96, 128, 144, 152, 192, 384, 512)
$sourceDir = "OneManVan.Web\wwwroot\icons"
$baseSvg = "$sourceDir\icon-base.svg"

Write-Host "PWA Icon Generator for OneManVan" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan

# Check if ImageMagick is available
$magickAvailable = Get-Command "magick" -ErrorAction SilentlyContinue

if ($magickAvailable) {
    Write-Host "Using ImageMagick to generate icons..." -ForegroundColor Green
    
    foreach ($size in $sizes) {
        $output = "$sourceDir\icon-${size}x${size}.png"
        Write-Host "  Generating $output..."
        magick convert -background none -resize "${size}x${size}" $baseSvg $output
    }
    
    Write-Host "All icons generated successfully!" -ForegroundColor Green
}
else {
    Write-Host "ImageMagick not found. Please install it or manually create PNG icons." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Required icon sizes:" -ForegroundColor White
    foreach ($size in $sizes) {
        Write-Host "  - icon-${size}x${size}.png"
    }
    Write-Host ""
    Write-Host "You can use online tools like:" -ForegroundColor White
    Write-Host "  - https://realfavicongenerator.net/" -ForegroundColor Cyan
    Write-Host "  - https://www.pwabuilder.com/imageGenerator" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Or install ImageMagick:" -ForegroundColor White
    Write-Host "  winget install ImageMagick.ImageMagick" -ForegroundColor Cyan
}
