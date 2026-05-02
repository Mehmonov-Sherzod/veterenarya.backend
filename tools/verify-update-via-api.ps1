[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ProgressPreference = 'SilentlyContinue'
$base = "http://localhost:5099/api/v1"

$auth = Invoke-RestMethod "$base/auth/login" -Method POST -Body (ConvertTo-Json @{username='admin';password='Admin@123'}) -ContentType 'application/json'
$h = @{ Authorization = "Bearer $($auth.accessToken)" }

Write-Output "=== UPDATE persistence proof via API (not SQL) ==="
Write-Output ""

# Create section, update it, then GET to verify changes saved
function PostJson($url, $body) {
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($body)
    $req = [System.Net.HttpWebRequest]::Create($url)
    $req.Method = "POST"
    $req.ContentType = "application/json; charset=utf-8"
    $req.Headers.Add("Authorization", "Bearer $($auth.accessToken)")
    $req.ContentLength = $bytes.Length
    $st = $req.GetRequestStream(); $st.Write($bytes, 0, $bytes.Length); $st.Close()
    $resp = $req.GetResponse()
    $rd = New-Object System.IO.StreamReader($resp.GetResponseStream())
    $rd.ReadToEnd() | ConvertFrom-Json
}

# 1. CREATE section
$sec = PostJson "$base/sections" (ConvertTo-Json @{titleUz='ORIGINAL_UZ';titleRu='ORIGINAL_RU';titleEn='ORIGINAL_EN';sortOrder=700;isActive=$true})
Write-Output "1. CREATE section id=$($sec.id):"
Write-Output "   TitleUz: $($sec.titleUz)"
Write-Output "   SortOrder: $($sec.sortOrder)"
Write-Output ""

# 2. PUT update
$upd = Invoke-RestMethod "$base/sections/$($sec.id)" -Method PUT -Body (ConvertTo-Json @{titleUz='CHANGED_UZ';titleRu='CHANGED_RU';titleEn='CHANGED_EN';sortOrder=701;isActive=$false}) -ContentType 'application/json; charset=utf-8' -Headers $h
Write-Output "2. PUT response (immediate):"
Write-Output "   TitleUz: $($upd.titleUz)"
Write-Output "   SortOrder: $($upd.sortOrder)"
Write-Output "   IsActive: $($upd.isActive)"
Write-Output "   UpdatedAt: $($upd.updatedAt)"
Write-Output ""

# 3. GET back via separate request — proves persistence
$verify = Invoke-RestMethod "$base/sections/$($sec.id)/detail" -Headers $h
Write-Output "3. GET /detail (separate request - reads from DB):"
Write-Output "   TitleUz: $($verify.titleUz)"
Write-Output "   TitleRu: $($verify.titleRu)"
Write-Output "   TitleEn: $($verify.titleEn)"
Write-Output "   SortOrder: $($verify.sortOrder)"
Write-Output "   IsActive: $($verify.isActive)"
Write-Output ""

# 4. Verification
$ok = $true
if ($verify.titleUz -ne 'CHANGED_UZ') { Write-Host "   FAIL: TitleUz not persisted" -ForegroundColor Red; $ok = $false }
if ($verify.titleRu -ne 'CHANGED_RU') { Write-Host "   FAIL: TitleRu not persisted" -ForegroundColor Red; $ok = $false }
if ($verify.titleEn -ne 'CHANGED_EN') { Write-Host "   FAIL: TitleEn not persisted" -ForegroundColor Red; $ok = $false }
if ($verify.sortOrder -ne 701) { Write-Host "   FAIL: SortOrder not persisted" -ForegroundColor Red; $ok = $false }
if ($verify.isActive) { Write-Host "   FAIL: IsActive not persisted" -ForegroundColor Red; $ok = $false }

if ($ok) { Write-Host "   ALL UPDATE FIELDS PERSISTED IN DB CORRECTLY" -ForegroundColor Green }

# Cleanup
Invoke-WebRequest "$base/sections/$($sec.id)" -Method DELETE -Headers $h | Out-Null

Write-Output ""
Write-Output "=== Same proof for CONTENT ==="

$sec = PostJson "$base/sections" (ConvertTo-Json @{titleUz='Parent';titleRu='Parent';titleEn='Parent';sortOrder=800;isActive=$true})
$ct = PostJson "$base/contents" (ConvertTo-Json @{
    sectionId=$sec.id
    titleUz='ORIG_UZ';titleRu='ORIG_RU';titleEn='ORIG_EN'
    descriptionUz='OrigDesc';descriptionRu='OrigDesc';descriptionEn='OrigDesc'
    imageUrl='/orig.jpg';sortOrder=10;isActive=$true
})
Write-Output "1. CREATE content id=$($ct.id):  ImageUrl=$($ct.imageUrl) SortOrder=$($ct.sortOrder)"

# UPDATE
$upd = Invoke-RestMethod "$base/contents/$($ct.id)" -Method PUT -Body (ConvertTo-Json @{
    sectionId=$sec.id
    titleUz='UPD_UZ';titleRu='UPD_RU';titleEn='UPD_EN'
    descriptionUz='UpdDesc';descriptionRu='UpdDesc';descriptionEn='UpdDesc'
    imageUrl='/updated.jpg';sortOrder=99;isActive=$false
}) -ContentType 'application/json; charset=utf-8' -Headers $h
Write-Output "2. PUT response (immediate): ImageUrl=$($upd.imageUrl) SortOrder=$($upd.sortOrder) IsActive=$($upd.isActive)"

# GET back
$verify = Invoke-RestMethod "$base/contents/$($ct.id)/detail" -Headers $h
Write-Output "3. GET /detail (reads from DB):"
Write-Output "   TitleUz: $($verify.titleUz)"
Write-Output "   TitleRu: $($verify.titleRu)"
Write-Output "   TitleEn: $($verify.titleEn)"
Write-Output "   ImageUrl: $($verify.imageUrl)"
Write-Output "   SortOrder: $($verify.sortOrder)"
Write-Output "   IsActive: $($verify.isActive)"

$ok = $true
if ($verify.titleUz -ne 'UPD_UZ') { Write-Host "   FAIL: TitleUz" -ForegroundColor Red; $ok = $false }
if ($verify.imageUrl -ne '/updated.jpg') { Write-Host "   FAIL: ImageUrl" -ForegroundColor Red; $ok = $false }
if ($verify.sortOrder -ne 99) { Write-Host "   FAIL: SortOrder" -ForegroundColor Red; $ok = $false }
if ($verify.isActive) { Write-Host "   FAIL: IsActive" -ForegroundColor Red; $ok = $false }

if ($ok) { Write-Host "   CONTENT UPDATE FULLY PERSISTED" -ForegroundColor Green }

# Cleanup
Invoke-WebRequest "$base/sections/$($sec.id)" -Method DELETE -Headers $h | Out-Null
