$ErrorActionPreference="Stop"
$d="C:\Users\m_bos\Downloads\NOPE\Assets\Art\Documents\Stamps"
[IO.File]::WriteAllBytes("$d\stamp_accept.png", [Convert]::FromBase64String("PLACEHOLDER"))
Write-Host "test"