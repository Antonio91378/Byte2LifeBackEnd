# Test Script for CSV Import Validation

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

# 1. Create a valid CSV file
$validCsvContent = @"
Descrição;LinkFilamento;PrecoFilamento;DescricaoFilamento;CorFilamento;LinkProduto;Qualidade;Massa;Custo;ValorVenda;Lucro;PorcentagemLucro;SexoCliente;Categoria;NumeroCliente;NomeCliente;Impresso;Entregue;Pago;Tempo;StatusImpressao
Vaso Teste;http://loja.com;100;PLA Teste;Azul;http://stl.com;Standard;100;10;50;40;400%;M;Novo;11999999999;Teste Silva;S;N;S;2h;Pendente
"@
$validCsvPath = "$PWD/valid_test.csv"
Set-Content -Path $validCsvPath -Value $validCsvContent -Encoding UTF8

# 2. Create an invalid CSV file (missing columns)
$invalidCsvContent = @"
Descrição;LinkFilamento;PrecoFilamento
Vaso Teste;http://loja.com;100
"@
$invalidCsvPath = "$PWD/invalid_test.csv"
Set-Content -Path $invalidCsvPath -Value $invalidCsvContent -Encoding UTF8

# 3. Test Valid Import
Test-Step "Import Valid CSV" {
    $boundary = [System.Guid]::NewGuid().ToString()
    $LF = "`r`n"
    
    $fileBytes = [System.IO.File]::ReadAllBytes($validCsvPath)
    $fileContent = [System.Text.Encoding]::GetEncoding('iso-8859-1').GetString($fileBytes)

    $bodyLines = (
        "--$boundary",
        "Content-Disposition: form-data; name=`"file`"; filename=`"valid_test.csv`"",
        "Content-Type: text/csv",
        "",
        $fileContent,
        "--$boundary--"
    ) -join $LF

    $response = Invoke-RestMethod -Method Post -Uri "$baseUrl/import" -ContentType "multipart/form-data; boundary=$boundary" -Body $bodyLines
    if ($response.failureCount -ne 0) {
        throw "Expected 0 failures, got $($response.failureCount)"
    }
}

# 4. Test Invalid Import (Backend Validation)
Test-Step "Import Invalid CSV (Missing Columns)" {
    $boundary = [System.Guid]::NewGuid().ToString()
    $LF = "`r`n"
    
    $fileBytes = [System.IO.File]::ReadAllBytes($invalidCsvPath)
    $fileContent = [System.Text.Encoding]::GetEncoding('iso-8859-1').GetString($fileBytes)

    $bodyLines = (
        "--$boundary",
        "Content-Disposition: form-data; name=`"file`"; filename=`"invalid_test.csv`"",
        "Content-Type: text/csv",
        "",
        $fileContent,
        "--$boundary--"
    ) -join $LF

    $response = Invoke-RestMethod -Method Post -Uri "$baseUrl/import" -ContentType "multipart/form-data; boundary=$boundary" -Body $bodyLines
    if ($response.failureCount -eq 0) {
        throw "Expected failures, got 0"
    }
    Write-Host " (Failures: $($response.failureCount))" -NoNewline
}

# Cleanup
Remove-Item $validCsvPath
Remove-Item $invalidCsvPath

Write-Host "`nBackend validation tests passed successfully!" -ForegroundColor Cyan
