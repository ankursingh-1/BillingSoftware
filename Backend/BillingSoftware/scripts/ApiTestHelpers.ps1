# Helpers for scripted API checks (Windows PowerShell 5.1 and PowerShell 7).
#
# Usage:   . .\ApiTestHelpers.ps1 ; Connect-Api -BaseUrl http://localhost:7190/api -Email x -Password y
#
# Point BaseUrl at a TEST API running against a COPY of the database, never at production.

Add-Type -AssemblyName System.Net.Http

$script:Http = New-Object System.Net.Http.HttpClient
$script:Http.Timeout = [TimeSpan]::FromSeconds(60)
$script:BaseUrl = $null
$script:Token = $null
$script:Passed = 0
$script:Failed = 0
$script:Failures = @()

function Connect-Api {
    param([string]$BaseUrl, [string]$Email, [string]$Password)

    $script:BaseUrl = $BaseUrl.TrimEnd('/')
    $script:Token = $null
    $r = Api POST '/Auth/login' @{ email = $Email; password = $Password } -Anonymous
    if ($r.Status -ne 200 -or -not $r.Json.token) { throw "Login failed (HTTP $($r.Status))." }
    $script:Token = $r.Json.token
}

function Use-Token([string]$Token) { $script:Token = $Token }

function Api {
    param([string]$Method, [string]$Path, $Body = $null, [switch]$Anonymous, [hashtable]$Headers = @{})

    $request = New-Object System.Net.Http.HttpRequestMessage ([System.Net.Http.HttpMethod]::new($Method)), ($script:BaseUrl + $Path)

    if (-not $Anonymous -and $script:Token) {
        $request.Headers.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue('Bearer', $script:Token)
    }
    foreach ($k in $Headers.Keys) { $request.Headers.TryAddWithoutValidation($k, [string]$Headers[$k]) | Out-Null }

    if ($null -ne $Body) {
        $json = if ($Body -is [string]) { $Body } else { $Body | ConvertTo-Json -Depth 12 -Compress }
        $request.Content = New-Object System.Net.Http.StringContent($json, [Text.Encoding]::UTF8, 'application/json')
    }

    $response = $script:Http.SendAsync($request).GetAwaiter().GetResult()
    $type = $response.Content.Headers.ContentType
    $mediaType = if ($type) { $type.MediaType } else { $null }

    $raw = $null; $obj = $null; $bytes = $null

    if ($mediaType -in @('application/pdf', 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet', 'image/png', 'image/jpeg')) {
        $bytes = $response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()
    }
    else {
        $raw = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        if ($raw) { try { $obj = $raw | ConvertFrom-Json } catch { } }
    }

    [pscustomobject]@{ Status = [int]$response.StatusCode; Json = $obj; Raw = $raw; Bytes = $bytes; Type = $mediaType }
}

function Api-Parallel {
    # Sends all requests at (almost) the same moment and returns one result per request.
    param([object[]]$Requests)   # each: @{ Method = 'POST'; Path = '/Sales'; Body = @{...} }

    $tasks = foreach ($item in $Requests) {
        $request = New-Object System.Net.Http.HttpRequestMessage ([System.Net.Http.HttpMethod]::new($item.Method)), ($script:BaseUrl + $item.Path)
        if ($script:Token) {
            $request.Headers.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue('Bearer', $script:Token)
        }
        if ($null -ne $item.Body) {
            $json = if ($item.Body -is [string]) { $item.Body } else { $item.Body | ConvertTo-Json -Depth 12 -Compress }
            $request.Content = New-Object System.Net.Http.StringContent($json, [Text.Encoding]::UTF8, 'application/json')
        }
        $script:Http.SendAsync($request)
    }

    [System.Threading.Tasks.Task]::WaitAll([System.Threading.Tasks.Task[]]@($tasks))

    foreach ($task in $tasks) {
        $response = $task.Result
        $raw = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        $obj = $null
        if ($raw) { try { $obj = $raw | ConvertFrom-Json } catch { } }
        [pscustomobject]@{ Status = [int]$response.StatusCode; Json = $obj; Raw = $raw }
    }
}

function Api-Upload {
    param([string]$Path, [string]$FilePath, [string]$FileName, [string]$ContentType, [string]$Field = 'file')

    $request = New-Object System.Net.Http.HttpRequestMessage ([System.Net.Http.HttpMethod]::Post), ($script:BaseUrl + $Path)
    if ($script:Token) {
        $request.Headers.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue('Bearer', $script:Token)
    }

    $content = New-Object System.Net.Http.MultipartFormDataContent
    $file = New-Object System.Net.Http.ByteArrayContent (, [IO.File]::ReadAllBytes($FilePath))
    $file.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse($ContentType)
    $content.Add($file, $Field, $FileName)
    $request.Content = $content

    $response = $script:Http.SendAsync($request).GetAwaiter().GetResult()
    $raw = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    $obj = $null
    if ($raw) { try { $obj = $raw | ConvertFrom-Json } catch { } }

    [pscustomobject]@{ Status = [int]$response.StatusCode; Json = $obj; Raw = $raw }
}

function Check {
    param([string]$Name, $Condition, [string]$Detail = '')

    if ($Condition) { $script:Passed++ }
    else {
        $script:Failed++
        $script:Failures += "$Name  $Detail"
        Write-Host "FAIL: $Name $Detail" -ForegroundColor Red
    }
}

function Test-Summary {
    Write-Host ("Checks passed: {0}   failed: {1}" -f $script:Passed, $script:Failed)
    if ($script:Failed -gt 0) { $script:Failures | ForEach-Object { Write-Host "  - $_" } }
}

function Get-Pdf-Valid($Result) {
    $Result.Status -eq 200 -and $Result.Bytes -and $Result.Bytes.Length -gt 500 -and
    [Text.Encoding]::ASCII.GetString($Result.Bytes, 0, 4) -eq '%PDF'
}
