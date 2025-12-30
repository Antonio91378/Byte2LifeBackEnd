# Simulation Script for Byte2Life Logic Validation

$baseUrl = "http://localhost:5000/api"

function Test-Step {
    param($name, $block)
    Write-Host "[$name] Executing..." -NoNewline
    try {
        & $block
        Write-Host " OK" -ForegroundColor Green
    } catch {
        Write-Host " FAILED" -ForegroundColor Red
        Write-Host "Error Message: $($_.Exception.Message)"
        if ($_.Exception.Response) {
            $reader = New-Object System.IO.StreamReader $_.Exception.Response.GetResponseStream()
            $responseBody = $reader.ReadToEnd()
            Write-Host "Response Body: $responseBody" -ForegroundColor Yellow
        }
        exit 1
    }
}

# 1. Reset Database
Test-Step "Reset Database" {
    Invoke-RestMethod -Method Delete -Uri "$baseUrl/debug/reset-database"
}

# 2. Create Filament
$script:filamentId = ""
Test-Step "Create Filament (1000g)" {
    $body = @{
        Description = "Filamento Teste Lógica"
        Link = "http://teste.com"
        Price = 100.0
        InitialMassGrams = 1000
        RemainingMassGrams = 1000
        Color = "Verde"
    } | ConvertTo-Json
    
    $filament = Invoke-RestMethod -Method Post -Uri "$baseUrl/filaments" -Body $body -ContentType "application/json"
    $script:filamentId = $filament.id
    Write-Host "Filament ID: $script:filamentId" -ForegroundColor Gray
}

# 3. Create Client (Required for Sale)
$script:clientId = ""
Test-Step "Create Client" {
    $body = @{
        Name = "Cliente Simulação"
        PhoneNumber = "000000000"
        Sex = "M"
        Category = "Teste"
    } | ConvertTo-Json
    
    $client = Invoke-RestMethod -Method Post -Uri "$baseUrl/clients" -Body $body -ContentType "application/json"
    $script:clientId = $client.id
    Write-Host "Client ID: $script:clientId" -ForegroundColor Gray
}

# 4. Create Sale (Consuming 200g)
Test-Step "Create Sale (Consuming 200g)" {
    $body = @{
        Description = "Venda Consumo"
        ProductLink = "http://stl.com"
        PrintQuality = "Standard"
        MassGrams = 200
        Cost = 20.0
        SaleValue = 50.0
        Profit = 30.0
        ProfitPercentage = "150%"
        DesignPrintTime = "2h"
        IsPrintConcluded = $true
        IsDelivered = $false
        IsPaid = $true
        FilamentId = $script:filamentId
        ClientId = $script:clientId
    } | ConvertTo-Json
    
    Write-Host "Sale Body: $body" -ForegroundColor Gray

    Invoke-RestMethod -Method Post -Uri "$baseUrl/sales" -Body $body -ContentType "application/json"
}

# 5. Verify Filament Mass
Test-Step "Verify Filament Mass (Should be 800g)" {
    $filament = Invoke-RestMethod -Method Get -Uri "$baseUrl/filaments/$script:filamentId"
    if ($filament.remainingMassGrams -ne 800) {
        throw "Expected 800g, but got $($filament.remainingMassGrams)g"
    }
}

# 6. Verify History
Test-Step "Verify Filament History" {
    $sales = Invoke-RestMethod -Method Get -Uri "$baseUrl/filaments/$script:filamentId/sales"
    if ($sales.Count -ne 1) {
        throw "Expected 1 sale in history, got $($sales.Count)"
    }
    if ($sales[0].massGrams -ne 200) {
        throw "Sale in history has wrong mass"
    }
    $script:saleId = $sales[0].id
}

# 7. Delete Sale (Restoring 200g)
Test-Step "Delete Sale (Restoring 200g)" {
    Invoke-RestMethod -Method Delete -Uri "$baseUrl/sales/$script:saleId"
}

# 8. Verify Filament Mass Restored
Test-Step "Verify Filament Mass Restored (Should be 1000g)" {
    $filament = Invoke-RestMethod -Method Get -Uri "$baseUrl/filaments/$script:filamentId"
    if ($filament.remainingMassGrams -ne 1000) {
        throw "Expected 1000g, but got $($filament.remainingMassGrams)g"
    }
}

# 9. Verify History Empty
Test-Step "Verify History Empty" {
    $sales = Invoke-RestMethod -Method Get -Uri "$baseUrl/filaments/$script:filamentId/sales"
    if ($sales.Count -ne 0) {
        throw "Expected 0 sales in history, got $($sales.Count)"
    }
}

Write-Host "`nAll simulation tests passed successfully!" -ForegroundColor Cyan
