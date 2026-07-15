@echo off
cls
echo =========================================
echo  SibangGenerator - Single EXE Build
echo =========================================
echo.

where dotnet >nul 2>&1
if errorlevel 1 goto nosdk

echo .NET SDK Version:
dotnet --version
echo.

echo [1/3] Restoring packages...
dotnet restore
if errorlevel 1 goto fail

echo.
echo [2/3] Converting logo.png to logo.ico (for application icon)...
if exist logo.png (
    if not exist logo.ico (
        powershell -NoProfile -ExecutionPolicy Bypass -Command "try { Add-Type -AssemblyName System.Drawing; $src=[System.Drawing.Image]::FromFile((Join-Path $PWD 'logo.png')); $bmp=New-Object System.Drawing.Bitmap $src,256,256; $h=$bmp.GetHicon(); $ico=[System.Drawing.Icon]::FromHandle($h); $fs=New-Object System.IO.FileStream((Join-Path $PWD 'logo.ico'),[System.IO.FileMode]::Create); $ico.Save($fs); $fs.Close(); $ico.Dispose(); $bmp.Dispose(); $src.Dispose(); Write-Host '   logo.ico generated successfully.' } catch { Write-Host '   [Warning] Icon conversion failed (proceeding without logo):' $_.Exception.Message }"
    ) else (
        echo    logo.ico already exists. Skipping conversion.
    )
) else (
    echo    logo.png not found. Skipping icon conversion.
)

echo.
echo [3/3] Publishing single file...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfContained=true -o bin\Release\net8.0-windows\win-x64\publish
if errorlevel 1 goto fail

echo.
echo =========================================
echo  SUCCESS!
echo =========================================
echo.

explorer bin\Release\net8.0-windows\win-x64\publish

pause
exit /b 0

:nosdk
echo [ERROR] .NET 8 SDK not found.
pause
exit /b 1

:fail
echo.
echo [FAIL] Build failed. Please check the error messages above.
pause
exit /b 1