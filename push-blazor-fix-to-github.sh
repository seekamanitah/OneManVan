#!/bin/bash
# Complete Blazor SignalR Fix - GitHub Push Script

echo "=========================================="
echo "Blazor Interactive Server Fix"
echo "Pushing to GitHub"
echo "=========================================="
echo ""

# Check if we're in a git repository
if [ ! -d ".git" ]; then
    echo "? Error: Not in a git repository"
    exit 1
fi

# Show what changed
echo "?? Files changed:"
git status --short

echo ""
echo "=========================================="
echo "Changes to commit:"
echo "=========================================="
echo "1. App.razor - Added @rendermode='InteractiveServer' (CRITICAL)"
echo "2. Program.cs - Cookie configuration for SignalR authentication"
echo "3. Program.cs - Fixed middleware order"
echo "4. Routes.razor - Better authentication handling"
echo "5. Removed .AddServerSideBlazor() conflict"
echo ""

# Add all changes
echo "Adding changes to git..."
git add OneManVan.Web/Components/App.razor
git add OneManVan.Web/Program.cs
git add OneManVan.Web/Components/Routes.razor

# Show what will be committed
echo ""
echo "?? Git diff --staged:"
git diff --staged --stat

echo ""
read -p "Proceed with commit? (y/n): " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "? Commit cancelled"
    exit 1
fi

# Commit
COMMIT_MSG="Fix: Enable Blazor Interactive Server for @onclick events

Critical fixes to enable SignalR circuit and button interactivity:

1. App.razor
   - Added @rendermode='InteractiveServer' to <Routes> component
   - Added @using Microsoft.AspNetCore.Components.Web
   - This enables SignalR WebSocket connection

2. Program.cs
   - Added .ConfigureApplicationCookie() with SignalR settings
   - Set SameSite=Lax for SignalR compatibility
   - Prevent auth redirects for /_blazor path
   - Fixed middleware order: Auth ? Authorization ? RateLimiter

3. Routes.razor
   - Better <NotAuthorized> handling
   - Distinguish authenticated vs unauthenticated users

Issue: Buttons with @onclick were non-responsive after adding authentication
Root Cause: Pages rendering as Static SSR without SignalR circuit
Solution: @rendermode='InteractiveServer' enables interactive features

Tested: ? SignalR WebSocket connects (Status 101)
Tested: ? Blazor.platform returns 'server'
Tested: ? All @onclick events work

Closes: Authentication blocking Blazor interactivity
"

echo "Committing..."
git commit -m "$COMMIT_MSG"

if [ $? -ne 0 ]; then
    echo "? Commit failed!"
    exit 1
fi

echo "? Commit successful!"
echo ""

# Push to GitHub
echo "Pushing to GitHub..."
git push origin master

if [ $? -eq 0 ]; then
    echo ""
    echo "=========================================="
    echo "? Successfully pushed to GitHub!"
    echo "=========================================="
    echo ""
    echo "Repository: https://github.com/seekamanitah/OneManVan"
    echo "Branch: master"
    echo ""
    echo "Next steps:"
    echo "1. Review commit on GitHub"
    echo "2. Deploy to server (see SERVER_DEPLOYMENT_GUIDE.md)"
    echo "=========================================="
else
    echo ""
    echo "? Push failed!"
    echo "Common issues:"
    echo "  - Check internet connection"
    echo "  - Verify GitHub credentials"
    echo "  - Check branch permissions"
    echo ""
    echo "To retry: git push origin master"
fi
