$env:NUGET_FALLBACK_PACKAGES = ""
Set-Location "D:\COURSE_2\Course_OOP"

Write-Host "=== Restoring in separate process ==="
Start-Process -FilePath "dotnet" -ArgumentList "restore", "/p:NuGetFallbackFolder=", "/p:RestoreFallbackFolders=" -Wait -NoNewWindow

Write-Host "=== Building in same process ==="
& dotnet build --no-restore "/p:NuGetFallbackFolder=" "/p:RestoreFallbackFolders="
