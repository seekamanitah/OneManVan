@echo off
REM OneManVan Docker Deployment Package Creator (Windows Batch Version)
REM Run this from the solution root directory

echo ==========================================
echo OneManVan Docker Deployment Package Creator
echo ==========================================
echo.

set OUTPUT_ZIP=deployment.zip
set TEMP_DIR=deployment-temp

REM Clean up previous deployment
if exist %OUTPUT_ZIP% (
    echo Removing old deployment package...
    del /F %OUTPUT_ZIP%
)

if exist %TEMP_DIR% (
    echo Cleaning up temp directory...
    rmdir /S /Q %TEMP_DIR%
)

REM Create temp directory
echo Creating deployment package...
mkdir %TEMP_DIR%

REM Copy necessary files
echo Copying Web project files...
xcopy /E /I /Y /EXCLUDE:exclude.txt OneManVan.Web %TEMP_DIR%\OneManVan.Web

echo Copying Shared project files...
xcopy /E /I /Y /EXCLUDE:exclude.txt OneManVan.Shared %TEMP_DIR%\OneManVan.Shared

echo Copying Docker configuration files...
copy /Y docker-compose-full.yml %TEMP_DIR%\
copy /Y .dockerignore %TEMP_DIR%\
copy /Y OneManVan.Web\Dockerfile %TEMP_DIR%\OneManVan.Web\

echo Copying SQL initialization scripts...
xcopy /E /I /Y docker %TEMP_DIR%\docker

echo Copying deployment scripts...
copy /Y deploy-to-docker.sh %TEMP_DIR%\
copy /Y DEPLOYMENT_INSTRUCTIONS.md %TEMP_DIR%\README.md
copy /Y .env %TEMP_DIR%\

REM Create QUICKSTART.txt
echo # OneManVan Web Docker Deployment Package > %TEMP_DIR%\QUICKSTART.txt
echo. >> %TEMP_DIR%\QUICKSTART.txt
echo ## Quick Start >> %TEMP_DIR%\QUICKSTART.txt
echo. >> %TEMP_DIR%\QUICKSTART.txt
echo 1. Upload this entire package to your Linux server at 192.168.100.107 >> %TEMP_DIR%\QUICKSTART.txt
echo 2. Extract: unzip deployment.zip -d /opt/onemanvan >> %TEMP_DIR%\QUICKSTART.txt
echo 3. Deploy: cd /opt/onemanvan ^&^& chmod +x deploy-to-docker.sh ^&^& ./deploy-to-docker.sh >> %TEMP_DIR%\QUICKSTART.txt
echo 4. Access: http://192.168.100.107:5000 >> %TEMP_DIR%\QUICKSTART.txt

REM Create the zip file using PowerShell
echo.
echo Creating deployment.zip...
powershell -Command "Compress-Archive -Path '%TEMP_DIR%\*' -DestinationPath '%OUTPUT_ZIP%' -CompressionLevel Optimal -Force"

REM Clean up temp directory
echo Cleaning up...
rmdir /S /Q %TEMP_DIR%

echo.
echo ==========================================
echo Deployment Package Created Successfully!
echo ==========================================
echo.
echo Package: %OUTPUT_ZIP%
echo.
echo Next Steps:
echo 1. Transfer deployment.zip to your Linux server:
echo    scp deployment.zip root@192.168.100.107:/root/
echo.
echo 2. SSH into the server:
echo    ssh root@192.168.100.107
echo.
echo 3. Extract and deploy:
echo    unzip deployment.zip -d /opt/onemanvan
echo    cd /opt/onemanvan
echo    chmod +x deploy-to-docker.sh
echo    ./deploy-to-docker.sh
echo.
echo 4. Access Web UI:
echo    http://192.168.100.107:5000
echo.
echo ==========================================
pause
