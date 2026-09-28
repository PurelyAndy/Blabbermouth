@echo off
setlocal

set "BASE=R:\repos\Blabbermouth\Blabbermouth\bin\Release\net10.0"

7z a -tzip -mx9 -mm=Deflate64 -mfb257 -mmt32 "%BASE%\osx-x64\publish\Blabbermouth_osx-x64.zip" "%BASE%\osx-x64\publish\Blabbermouth"
7z a -tzip -mx9 -mm=Deflate64 -mfb257 -mmt32 "%BASE%\osx-arm64\publish\Blabbermouth_osx-arm64.zip" "%BASE%\osx-arm64\publish\Blabbermouth"
7z a -tzip -mx9 -mm=Deflate64 -mfb257 -mmt32 "%BASE%\linux-x64\publish\Blabbermouth_linux-x64.zip" "%BASE%\linux-x64\publish\Blabbermouth"
7z a -tzip -mx9 -mm=Deflate64 -mfb257 -mmt32 "%BASE%\win-x64\publish\Blabbermouth_win-x64.zip" "%BASE%\win-x64\publish\Blabbermouth.exe"

if errorlevel 1 (
    echo.
    echo One or more archive operations failed.
    exit /b 1
)

echo.
echo All archives created successfully.
endlocal