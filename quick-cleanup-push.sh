#!/bin/bash
# Quick cleanup and push to GitHub

echo "========================================="
echo "OneManVan - Quick Cleanup and Push"
echo "========================================="
echo ""

# Step 1: Remove temporary files
echo "Removing temporary files..."
rm -f "OneManVan.Web/out.txt" 2>/dev/null
rm -f "employee info.txt" 2>/dev/null
rm -f "employee records.txt" 2>/dev/null
rm -f "audit comands.md" 2>/dev/null
echo "? Cleaned up temporary files"

# Step 2: Stage all changes
echo ""
echo "Staging changes..."
git add -A
echo "? All changes staged"

# Step 3: Show status
echo ""
echo "========================================="
echo "Files to be committed:"
echo "========================================="
git status --short

# Step 4: Confirm
echo ""
echo "========================================="
echo "Ready to commit and push!"
echo "========================================="
echo ""
echo "Commit message:"
echo "  Fix critical Blazor buttons issue and add new features"
echo ""
read -p "Continue? (y/n): " confirm

if [ "$confirm" != "y" ] && [ "$confirm" != "Y" ]; then
    echo "Aborted."
    exit 1
fi

# Step 5: Commit
echo ""
echo "Committing..."
git commit -m "Fix critical Blazor buttons issue and add new features

- Fixed App.razor script tag error causing buttons to not work
- Added Employee Management system
- Added Document Library
- Added Material Lists feature
- Added PWA support
- Added Company Settings
- Enhanced deployment options
- Improved security and data protection
- Updated documentation"

if [ $? -ne 0 ]; then
    echo "Commit failed!"
    exit 1
fi

echo "? Committed successfully"

# Step 6: Push
echo ""
echo "Pushing to GitHub..."
git push origin master

if [ $? -ne 0 ]; then
    echo ""
    echo "Push failed!"
    echo ""
    echo "Common fixes:"
    echo "  1. Pull first: git pull origin master"
    echo "  2. Check credentials: git config --list | grep user"
    echo "  3. Try: git push -u origin master"
    exit 1
fi

echo ""
echo "========================================="
echo "Success!"
echo "========================================="
echo ""
echo "Your changes are now on GitHub!"
echo "View at: https://github.com/seekamanitah/OneManVan"
echo ""
