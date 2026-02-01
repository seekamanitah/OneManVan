#!/bin/bash
# Fix Line Endings on Server
# Run this after uploading files from Windows

echo "============================================"
echo "   Fixing Line Endings"
echo "============================================"
echo ""

# Install dos2unix if not present
if ! command -v dos2unix &> /dev/null; then
    echo "?? Installing dos2unix..."
    sudo apt-get update -qq
    sudo apt-get install -y dos2unix
    echo "? dos2unix installed"
else
    echo "? dos2unix already installed"
fi
echo ""

# Fix all shell scripts in current directory
echo "?? Converting line endings..."
find . -type f -name "*.sh" -exec dos2unix {} \; 2>/dev/null

echo "? Line endings fixed"
echo ""

# Make all shell scripts executable
echo "?? Making scripts executable..."
chmod +x *.sh 2>/dev/null || true
echo "? Scripts are executable"
echo ""

echo "============================================"
echo "   ? Ready to Deploy!"
echo "============================================"
echo ""
echo "Run: ./deploy.sh"
