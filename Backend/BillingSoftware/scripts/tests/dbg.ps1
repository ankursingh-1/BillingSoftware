param([string]$BaseUrl, [string]$Email, [string]$Password)
. (Join-Path $PSScriptRoot '..\ApiTestHelpers.ps1')
$script:BaseUrl = $BaseUrl
$r = Api POST '/Auth/login' @{ email = $Email; password = $Password } -Anonymous
"direct: status=$($r.Status)"
Connect-Api -BaseUrl $BaseUrl -Email $Email -Password $Password
"connected"
