[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ProgressPreference = 'SilentlyContinue'
$base = "http://localhost:5099/api/v1"
$env:PGPASSWORD = "Sherzod3466"
$psql = "C:\Program Files\PostgreSQL\18\bin\psql.exe"

function DbCount($table) {
    $q = "SELECT count(*) FROM $table;"
    (& $psql -h localhost -p 5433 -U postgres -d veterinary_db -t -A -c $q 2>$null).Trim()
}
function DbVal($q) {
    (& $psql -h localhost -p 5433 -U postgres -d veterinary_db -t -A -c $q 2>$null).Trim()
}
function PostJson($url, $body, $headers) {
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($body)
    $req = [System.Net.HttpWebRequest]::Create($url)
    $req.Method = "POST"
    $req.ContentType = "application/json; charset=utf-8"
    if ($headers) { foreach ($k in $headers.Keys) { $req.Headers.Add($k, $headers[$k]) } }
    $req.ContentLength = $bytes.Length
    $st = $req.GetRequestStream(); $st.Write($bytes, 0, $bytes.Length); $st.Close()
    $resp = $req.GetResponse()
    $rd = New-Object System.IO.StreamReader($resp.GetResponseStream())
    @{ status = [int]$resp.StatusCode; body = $rd.ReadToEnd() | ConvertFrom-Json }
}

# Login
$auth = Invoke-RestMethod "$base/auth/login" -Method POST -Body (ConvertTo-Json @{username='admin';password='Admin@123'}) -ContentType 'application/json'
$h = @{ Authorization = "Bearer $($auth.accessToken)" }

$ok = 0; $fail = 0
function Step {
    param([string]$label, [scriptblock]$block)
    Write-Host -NoNewline ("    {0,-50} " -f $label)
    try { & $block; Write-Host "OK" -ForegroundColor Green; $script:ok++ }
    catch { Write-Host ("FAIL " + $_.Exception.Message) -ForegroundColor Red; $script:fail++ }
}

Write-Output ""
Write-Output "================================================================"
Write-Output "  AUTH -- full cycle (login, change, reset, change-back)"
Write-Output "================================================================"

Step "POST /auth/login (admin/Admin@123) -> 200 + JWT" {
    $r = Invoke-RestMethod "$base/auth/login" -Method POST -Body (ConvertTo-Json @{username='admin';password='Admin@123'}) -ContentType 'application/json'
    if (-not $r.accessToken) { throw "no token" }
    if ($r.role -ne 'Admin') { throw "wrong role" }
}
Step "POST /auth/login wrong password -> 401" {
    try { Invoke-RestMethod "$base/auth/login" -Method POST -Body (ConvertTo-Json @{username='admin';password='WRONG'}) -ContentType 'application/json' | Out-Null; throw "expected 401" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 401) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
Step "POST /auth/change-password (Admin@123 -> CHG@456) -> 204" {
    $r = Invoke-WebRequest "$base/auth/change-password" -Method POST -Body (ConvertTo-Json @{currentPassword='Admin@123';newPassword='CHG@456'}) -ContentType 'application/json' -Headers $h
    if ($r.StatusCode -ne 204) { throw "got $($r.StatusCode)" }
    Start-Sleep -Milliseconds 700
}
Step "Login with NEW password (CHG@456) succeeds" {
    $r = Invoke-RestMethod "$base/auth/login" -Method POST -Body (ConvertTo-Json @{username='admin';password='CHG@456'}) -ContentType 'application/json'
    if (-not $r.accessToken) { throw "no token" }
}
Step "Login with OLD password fails (Admin@123 rejected)" {
    try { Invoke-RestMethod "$base/auth/login" -Method POST -Body (ConvertTo-Json @{username='admin';password='Admin@123'}) -ContentType 'application/json' | Out-Null; throw "old still works" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 401) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
Step "POST /auth/reset-password (recovery key -> RST@789) -> 204" {
    $r = Invoke-WebRequest "$base/auth/reset-password" -Method POST -Body (ConvertTo-Json @{recoveryKey='ArNp4YNVJljrfCbYW8fe/bw5bc8hkvjorlNrYDG/f8c=';newPassword='RST@789'}) -ContentType 'application/json'
    if ($r.StatusCode -ne 204) { throw "got $($r.StatusCode)" }
    Start-Sleep -Milliseconds 700
}
Step "Login with reset password (RST@789) succeeds" {
    $r = Invoke-RestMethod "$base/auth/login" -Method POST -Body (ConvertTo-Json @{username='admin';password='RST@789'}) -ContentType 'application/json'
    if (-not $r.accessToken) { throw "no token" }
    $script:tempToken = $r.accessToken
}
Step "Restore original password (Admin@123)" {
    $tH = @{ Authorization = "Bearer $tempToken" }
    Invoke-WebRequest "$base/auth/change-password" -Method POST -Body (ConvertTo-Json @{currentPassword='RST@789';newPassword='Admin@123'}) -ContentType 'application/json' -Headers $tH | Out-Null
    Start-Sleep -Milliseconds 700
    $verify = Invoke-RestMethod "$base/auth/login" -Method POST -Body (ConvertTo-Json @{username='admin';password='Admin@123'}) -ContentType 'application/json'
    if (-not $verify.accessToken) { throw "restore failed" }
    $script:authToken = $verify.accessToken
    $script:h = @{ Authorization = "Bearer $authToken" }
}

Write-Output ""
Write-Output "================================================================"
Write-Output "  SECTION -- full CRUD lifecycle"
Write-Output "================================================================"

$beforeCount = DbCount "sections"
Write-Output "    [DB] sections count BEFORE: $beforeCount"

$sectionId = $null
Step "POST /sections -> 201 + slug auto-generated" {
    $r = PostJson "$base/sections" (ConvertTo-Json @{titleUz='Test_Section_UZ';titleRu='Test_Section_RU';titleEn='Test_Section_EN';sortOrder=500;isActive=$true}) $h
    if ($r.status -ne 201) { throw "got $($r.status)" }
    if ($r.body.slug -ne 'test-section-en') { throw "slug=$($r.body.slug)" }
    $script:sectionId = $r.body.id
}
$afterCreate = DbCount "sections"
Write-Output "    [DB] AFTER CREATE: $afterCreate (expected $([int]$beforeCount + 1))"
if ([int]$afterCreate -eq [int]$beforeCount + 1) { Write-Host "    DB +1 row OK" -ForegroundColor Green; $ok++ } else { Write-Host "    DB MISMATCH" -ForegroundColor Red; $fail++ }

Step "GET /sections/{id}/detail returns all 3 langs + sortOrder + isActive" {
    $r = Invoke-RestMethod "$base/sections/$sectionId/detail" -Headers $h
    if ($r.titleUz -ne 'Test_Section_UZ') { throw "uz wrong" }
    if ($r.titleRu -ne 'Test_Section_RU') { throw "ru wrong" }
    if ($r.titleEn -ne 'Test_Section_EN') { throw "en wrong" }
    if ($r.sortOrder -ne 500) { throw "sort wrong" }
    if (-not $r.isActive) { throw "active wrong" }
}
Step "GET /sections (public list) -- new section appears" {
    $list = Invoke-RestMethod "$base/sections?onlyActive=false"
    $found = $list | Where-Object { $_.id -eq $sectionId }
    if (-not $found) { throw "not in list" }
}
Step "GET /sections?lang=ru -- RU title localized" {
    $list = Invoke-RestMethod "$base/sections?lang=ru&onlyActive=false"
    $found = $list | Where-Object { $_.id -eq $sectionId }
    if ($found.title -ne 'Test_Section_RU') { throw "got $($found.title)" }
}
Step "PUT /sections/{id} -> 200 + fields updated" {
    $r = Invoke-RestMethod "$base/sections/$sectionId" -Method PUT -Body (ConvertTo-Json @{titleUz='Updated_UZ';titleRu='Updated_RU';titleEn='Updated_EN';sortOrder=501;isActive=$false}) -ContentType 'application/json; charset=utf-8' -Headers $h
    if ($r.titleUz -ne 'Updated_UZ') { throw "not updated" }
    if (-not $r.updatedAt) { throw "no updatedAt" }
}
Step "Verify UPDATE persisted in DB" {
    $val = DbVal "SELECT `"TitleUz`" FROM sections WHERE `"Id`"=$sectionId;"
    if ($val -ne 'Updated_UZ') { throw "DB val wrong" }
}
Step "Filter onlyActive=true now hides this section (we set isActive=false)" {
    $list = Invoke-RestMethod "$base/sections?onlyActive=true"
    $found = $list | Where-Object { $_.id -eq $sectionId }
    if ($found) { throw "still appears in active list" }
}
Step "DELETE /sections/{id} -> 204" {
    $r = Invoke-WebRequest "$base/sections/$sectionId" -Method DELETE -Headers $h
    if ($r.StatusCode -ne 204) { throw "got $($r.StatusCode)" }
}
Step "Verify DELETE -- 404 on detail" {
    try { Invoke-RestMethod "$base/sections/$sectionId/detail" -Headers $h | Out-Null; throw "still exists" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 404) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
$afterDelete = DbCount "sections"
Write-Output "    [DB] AFTER DELETE: $afterDelete (expected $beforeCount)"
if ([int]$afterDelete -eq [int]$beforeCount) { Write-Host "    DB back to baseline OK" -ForegroundColor Green; $ok++ } else { Write-Host "    DB MISMATCH" -ForegroundColor Red; $fail++ }

Write-Output ""
Write-Output "================================================================"
Write-Output "  CONTENT -- full CRUD + cascade"
Write-Output "================================================================"

$beforeCount = DbCount "contents"
Write-Output "    [DB] contents count BEFORE: $beforeCount"

$sec = (PostJson "$base/sections" (ConvertTo-Json @{titleUz='ParentSec';titleRu='ParentSec';titleEn='ParentSec';sortOrder=600;isActive=$true}) $h).body
$parentId = $sec.id
$contentId = $null

Step "POST /contents -> 201 + sectionId, all 3 lang fields" {
    $r = PostJson "$base/contents" (ConvertTo-Json @{
        sectionId=$parentId;titleUz='Ct_UZ';titleRu='Ct_RU';titleEn='Ct_EN'
        descriptionUz='Desc UZ <strong>HTML</strong>';descriptionRu='Описание RU';descriptionEn='Desc EN'
        imageUrl='/uploads/test.jpg';sortOrder=10;isActive=$true
    }) $h
    if ($r.status -ne 201) { throw "got $($r.status)" }
    if ($r.body.sectionId -ne $parentId) { throw "wrong sectionId" }
    $script:contentId = $r.body.id
}
$afterCreate = DbCount "contents"
Write-Output "    [DB] AFTER CREATE: $afterCreate (expected $([int]$beforeCount + 1))"
if ([int]$afterCreate -eq [int]$beforeCount + 1) { Write-Host "    DB +1 row OK" -ForegroundColor Green; $ok++ } else { Write-Host "    DB MISMATCH" -ForegroundColor Red; $fail++ }

Step "GET /contents/{id}/detail -- all 3 langs preserved + HTML safe" {
    $r = Invoke-RestMethod "$base/contents/$contentId/detail" -Headers $h
    if ($r.titleUz -ne 'Ct_UZ') { throw "uz" }
    if ($r.titleRu -ne 'Ct_RU') { throw "ru" }
    if ($r.titleEn -ne 'Ct_EN') { throw "en" }
    if ($r.descriptionUz -notmatch 'HTML') { throw "HTML stripped" }
    if ($r.descriptionRu -notmatch 'Описание') { throw "RU desc lost" }
}
Step "GET /contents/{id}?lang=ru -- localized title + desc" {
    $r = Invoke-RestMethod "$base/contents/$contentId`?lang=ru"
    if ($r.title -ne 'Ct_RU') { throw "title=$($r.title)" }
    if ($r.description -notmatch 'Описание') { throw "no RU desc" }
}
Step "GET /contents?sectionId filter finds new content" {
    $r = Invoke-RestMethod "$base/contents?sectionId=$parentId"
    $found = $r.items | Where-Object { $_.id -eq $contentId }
    if (-not $found) { throw "filter missed" }
}
Step "PUT /contents/{id} -> 200 + updatedAt set" {
    $r = Invoke-RestMethod "$base/contents/$contentId" -Method PUT -Body (ConvertTo-Json @{
        sectionId=$parentId;titleUz='Ct_UZ_NEW';titleRu='Ct_RU_NEW';titleEn='Ct_EN_NEW'
        descriptionUz='New desc';descriptionRu='Новое описание';descriptionEn='New desc'
        imageUrl='/uploads/new.jpg';sortOrder=11;isActive=$false
    }) -ContentType 'application/json; charset=utf-8' -Headers $h
    if ($r.titleUz -ne 'Ct_UZ_NEW') { throw "not updated" }
    if (-not $r.updatedAt) { throw "no updatedAt" }
}
Step "Verify UPDATE persisted -- title, image, sortOrder" {
    $title = DbVal "SELECT `"TitleUz`" FROM contents WHERE `"Id`"=$contentId;"
    $img = DbVal "SELECT `"ImageUrl`" FROM contents WHERE `"Id`"=$contentId;"
    $sort = DbVal "SELECT `"SortOrder`" FROM contents WHERE `"Id`"=$contentId;"
    if ($title -ne 'Ct_UZ_NEW') { throw "title=$title" }
    if ($img -ne '/uploads/new.jpg') { throw "img=$img" }
    if ($sort -ne '11') { throw "sort=$sort" }
}
Step "DELETE /contents/{id} -> 204" {
    $r = Invoke-WebRequest "$base/contents/$contentId" -Method DELETE -Headers $h
    if ($r.StatusCode -ne 204) { throw "got $($r.StatusCode)" }
}
Step "Verify DELETE -- 404" {
    try { Invoke-RestMethod "$base/contents/$contentId" | Out-Null; throw "still" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 404) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}

Write-Output ""
Write-Output "    >>> CASCADE DELETE TEST"
$c1 = (PostJson "$base/contents" (ConvertTo-Json @{sectionId=$parentId;titleUz='C1';titleRu='C1';titleEn='C1';descriptionUz='d';descriptionRu='d';descriptionEn='d';imageUrl='/x.jpg';sortOrder=1;isActive=$true}) $h).body
$c2 = (PostJson "$base/contents" (ConvertTo-Json @{sectionId=$parentId;titleUz='C2';titleRu='C2';titleEn='C2';descriptionUz='d';descriptionRu='d';descriptionEn='d';imageUrl='/x.jpg';sortOrder=2;isActive=$true}) $h).body
$c1id = $c1.id; $c2id = $c2.id
Write-Output "    Created 2 child contents (id=$c1id, id=$c2id)"

Step "DELETE parent section -> cascades 2 child contents" {
    Invoke-WebRequest "$base/sections/$parentId" -Method DELETE -Headers $h | Out-Null
    try { Invoke-RestMethod "$base/contents/$c1id" | Out-Null; throw "child 1 not cascaded" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 404) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
    try { Invoke-RestMethod "$base/contents/$c2id" | Out-Null; throw "child 2 not cascaded" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 404) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
$afterCascade = DbCount "contents"
Write-Output "    [DB] AFTER cascade: $afterCascade (expected $beforeCount)"
if ([int]$afterCascade -eq [int]$beforeCount) { Write-Host "    DB cascade verified" -ForegroundColor Green; $ok++ } else { Write-Host "    DB MISMATCH" -ForegroundColor Red; $fail++ }

Write-Output ""
Write-Output "================================================================"
Write-Output "  MEDIA -- upload + delete (DB row + disk file)"
Write-Output "================================================================"

$beforeMedia = DbCount "media_files"
Write-Output "    [DB] media count BEFORE: $beforeMedia"

$mediaId = $null
$mediaPath = $null
Step "POST /media/upload -> 201 + url + file on disk" {
    $LF = "`r`n"; $boundary = [Guid]::NewGuid().ToString()
    $body = "--$boundary$LF" + 'Content-Disposition: form-data; name="file"; filename="test-upload.txt"' + "$LF" + 'Content-Type: text/plain' + "$LF$LF" + 'lifecycle test content' + "$LF--$boundary--$LF"
    $r = Invoke-RestMethod "$base/media/upload" -Method POST -ContentType "multipart/form-data; boundary=$boundary" -Body $body -Headers $h
    if (-not $r.url) { throw "no url" }
    $script:mediaId = $r.id
    $script:mediaPath = "C:\Users\AIZENN\Desktop\veterenarya.backend\src\VeterinaryBackend.API\wwwroot$($r.url.Replace('/','\'))"
    if (-not (Test-Path $mediaPath)) { throw "file not on disk" }
}
$afterUpload = DbCount "media_files"
Write-Output "    [DB] AFTER upload: $afterUpload (expected $([int]$beforeMedia + 1))"
if ([int]$afterUpload -eq [int]$beforeMedia + 1) { Write-Host "    DB +1 row OK" -ForegroundColor Green; $ok++ } else { Write-Host "    DB MISMATCH" -ForegroundColor Red; $fail++ }

Step "GET /media/{id} -- metadata correct" {
    $r = Invoke-RestMethod "$base/media/$mediaId" -Headers $h
    if ($r.originalFileName -ne 'test-upload.txt') { throw "wrong name" }
    if ($r.contentType -ne 'text/plain') { throw "wrong type" }
}
Step "Public URL fetch -- file content readable" {
    $r = Invoke-RestMethod "$base/media/$mediaId" -Headers $h
    $resp = Invoke-WebRequest "http://localhost:5099$($r.url)"
    if ($resp.StatusCode -ne 200) { throw "got $($resp.StatusCode)" }
    if ($resp.Content -notmatch 'lifecycle') { throw "content wrong" }
}
Step "DELETE /media/{id} -> 204 + DB row + disk file gone" {
    $r = Invoke-WebRequest "$base/media/$mediaId" -Method DELETE -Headers $h
    if ($r.StatusCode -ne 204) { throw "got $($r.StatusCode)" }
    if (Test-Path $mediaPath) { throw "file still on disk" }
}
Step "GET deleted /media/{id} -> 404" {
    try { Invoke-RestMethod "$base/media/$mediaId" -Headers $h | Out-Null; throw "still" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 404) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}

Write-Output ""
Write-Output "================================================================"
Write-Output "  VALIDATION -- bad data must be rejected"
Write-Output "================================================================"

Step "POST /sections empty title -> 400" {
    try { Invoke-WebRequest "$base/sections" -Method POST -Body '{"titleUz":"","titleRu":"","titleEn":""}' -ContentType 'application/json' -Headers $h | Out-Null; throw "expected 400" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 400) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
Step "POST /sections invalid slug -> 400" {
    try { Invoke-WebRequest "$base/sections" -Method POST -Body '{"slug":"BAD SLUG!","titleUz":"x","titleRu":"x","titleEn":"x"}' -ContentType 'application/json' -Headers $h | Out-Null; throw "expected 400" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 400) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
Step "POST /contents empty image -> 400" {
    try { Invoke-WebRequest "$base/contents" -Method POST -Body '{"sectionId":1,"titleUz":"a","titleRu":"a","titleEn":"a","descriptionUz":"a","descriptionRu":"a","descriptionEn":"a","imageUrl":""}' -ContentType 'application/json' -Headers $h | Out-Null; throw "expected 400" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 400) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
Step "POST /contents empty TitleEn -> 400" {
    try { Invoke-WebRequest "$base/contents" -Method POST -Body '{"sectionId":1,"titleUz":"a","titleRu":"a","titleEn":"","descriptionUz":"a","descriptionRu":"a","descriptionEn":"a","imageUrl":"/x.jpg"}' -ContentType 'application/json' -Headers $h | Out-Null; throw "expected 400" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 400) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
Step "PUT /sections/99999 -> 404" {
    try { Invoke-WebRequest "$base/sections/99999" -Method PUT -Body '{"titleUz":"x","titleRu":"x","titleEn":"x"}' -ContentType 'application/json' -Headers $h | Out-Null; throw "expected 404" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 404) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
Step "DELETE /sections/99999 -> 404" {
    try { Invoke-WebRequest "$base/sections/99999" -Method DELETE -Headers $h | Out-Null; throw "expected 404" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 404) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
Step "POST /contents without auth -> 401" {
    try { Invoke-WebRequest "$base/contents" -Method POST -Body '{}' -ContentType 'application/json' | Out-Null; throw "expected 401" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 401) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}

Write-Output ""
Write-Output "================================================================"
Write-Output "  FINAL STATE -- original 4 sec / 15 ct / 5 md must be intact"
Write-Output "================================================================"
$fS = DbCount "sections"; $fC = DbCount "contents"; $fM = DbCount "media_files"
Write-Output "    sections    : $fS  (expected: 4)"
Write-Output "    contents    : $fC  (expected: 15)"
Write-Output "    media_files : $fM  (expected: 5)"
if ($fS -eq '4' -and $fC -eq '15' -and $fM -eq '5') {
    Write-Host "    Original data INTACT after CRUD test" -ForegroundColor Green; $ok++
} else {
    Write-Host "    DATA MISMATCH" -ForegroundColor Red; $fail++
}

Write-Output ""
Write-Output "================================================================"
$total = $ok + $fail
$pct = if ($total -gt 0) { [Math]::Round(($ok/$total)*100, 1) } else { 0 }
Write-Output ("  RESULT:  PASS $ok   FAIL $fail   ({0}%)" -f $pct)
Write-Output "================================================================"
Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
