$proc = Start-Process dotnet -ArgumentList 'run --project C:\Users\DELL\Desktop\RetailFixIT\backend\src\RetailFixIT.API' -PassThru -NoNewWindow
Start-Sleep -Seconds 20
try {
    $resp = Invoke-RestMethod -Uri 'http://localhost:5000/api/v1/auth/login' -Method Post -ContentType 'application/json' -Body '{"email":"dispatcher@acme.com","password":"Test1234!"}'
    Write-Output ("LOGIN OK - " + $resp.user.email)
} catch {
    Write-Output ("LOGIN FAILED: " + $_.Exception.Message)
}
$proc.Kill()
