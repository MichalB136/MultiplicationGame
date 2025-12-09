try {
    Write-Host "Running dotnet format (verify)..."

    $dotnetFormat = Get-Command dotnet-format -ErrorAction SilentlyContinue
    if (-not $dotnetFormat) {
        Write-Host "dotnet-format not found. Installing global tool..."
        dotnet tool install -g dotnet-format --version 6.0.256901 | Out-Null
        $env:PATH = "$env:USERPROFILE\.dotnet\tools;${env:PATH}"
    }

    $proc = Start-Process -FilePath dotnet -ArgumentList 'format','--verify-no-changes' -NoNewWindow -PassThru -Wait
    if ($proc.ExitCode -eq 0) {
        Write-Host "Code is formatted."
        exit 0
    }
    else {
        Write-Error "Code is not formatted. Run 'dotnet format' and commit the changes."
        exit 1
    }
}
catch {
    Write-Error "Error running dotnet format: $_"
    exit 1
}
