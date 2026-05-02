[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ProgressPreference = 'SilentlyContinue'
$base = "http://localhost:5099/api/v1"
$adm = "http://localhost:5099/admin"
$front = "http://localhost:8080"
$ok = 0; $fail = 0; $failed = @()

function T {
    param([string]$cat, [string]$name, [scriptblock]$block)
    Write-Host -NoNewline ("  > {0,-65} " -f $name)
    try { & $block; Write-Host "PASS" -ForegroundColor Green; $script:ok++ }
    catch {
        $msg = $_.Exception.Message
        if ($msg.Length -gt 60) { $msg = $msg.Substring(0, 60) + "..." }
        Write-Host ("FAIL " + $msg) -ForegroundColor Red
        $script:fail++
        $script:failed += "[$cat] $name : $msg"
    }
}

# Login
$auth = Invoke-RestMethod "$base/auth/login" -Method POST -Body (ConvertTo-Json @{username='admin';password='Admin@123'}) -ContentType 'application/json'
$tk = $auth.accessToken
$h = @{ Authorization = "Bearer $tk" }

Write-Output ""
Write-Output "================================================================"
Write-Output " 1. SERVER HEALTH"
Write-Output "================================================================"

T "h" "Backend admin (HTTP 200)" {
    $r = Invoke-WebRequest "$adm/" -Method GET; if ($r.StatusCode -ne 200) { throw "got $($r.StatusCode)" }
}
T "h" "Frontend index (HTTP 200)" {
    $r = Invoke-WebRequest "$front/" -Method GET; if ($r.StatusCode -ne 200) { throw "got $($r.StatusCode)" }
}
T "h" "Swagger reachable" {
    $r = Invoke-WebRequest "http://localhost:5099/swagger" -MaximumRedirection 5
    if ($r.StatusCode -ne 200) { throw "got $($r.StatusCode)" }
}
T "h" "Root / redirects to /admin/" {
    try { Invoke-WebRequest "http://localhost:5099/" -MaximumRedirection 0 | Out-Null }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 302) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
T "h" "/admin (no slash) redirects to /admin/" {
    try { Invoke-WebRequest "http://localhost:5099/admin" -MaximumRedirection 0 | Out-Null }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 302) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}

Write-Output ""
Write-Output "================================================================"
Write-Output " 2. AUTH ENDPOINTS"
Write-Output "================================================================"

T "auth" "POST /auth/login wrong password -> 401" {
    try { Invoke-RestMethod "$base/auth/login" -Method POST -Body (ConvertTo-Json @{username='admin';password='wrong'}) -ContentType 'application/json' | Out-Null; throw "expected 401" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 401) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
T "auth" "POST /auth/login empty body -> 401" {
    try { Invoke-RestMethod "$base/auth/login" -Method POST -Body '{}' -ContentType 'application/json' | Out-Null; throw "expected 401" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 401) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
T "auth" "POST /auth/login correct -> 200 + JWT (role=Admin)" {
    $r = Invoke-RestMethod "$base/auth/login" -Method POST -Body (ConvertTo-Json @{username='admin';password='Admin@123'}) -ContentType 'application/json'
    if (-not $r.accessToken) { throw "no token" }
    if ($r.role -ne 'Admin') { throw "role=$($r.role)" }
}
T "auth" "POST /auth/change-password no auth -> 401" {
    try { Invoke-WebRequest "$base/auth/change-password" -Method POST -Body '{}' -ContentType 'application/json' | Out-Null; throw "expected 401" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 401) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
T "auth" "POST /auth/change-password wrong current -> 401" {
    try { Invoke-WebRequest "$base/auth/change-password" -Method POST -Body (ConvertTo-Json @{currentPassword='wrong';newPassword='NewPass1'}) -ContentType 'application/json' -Headers $h | Out-Null; throw "expected 401" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 401) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
T "auth" "POST /auth/change-password too short -> 400" {
    try { Invoke-WebRequest "$base/auth/change-password" -Method POST -Body (ConvertTo-Json @{currentPassword='Admin@123';newPassword='abc'}) -ContentType 'application/json' -Headers $h | Out-Null; throw "expected 400" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 400) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
T "auth" "POST /auth/reset-password wrong key -> 401" {
    try { Invoke-RestMethod "$base/auth/reset-password" -Method POST -Body (ConvertTo-Json @{recoveryKey='wrong';newPassword='NewPass1'}) -ContentType 'application/json' | Out-Null; throw "expected 401" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 401) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
T "auth" "POST /auth/reset-password too short -> 400" {
    try { Invoke-RestMethod "$base/auth/reset-password" -Method POST -Body (ConvertTo-Json @{recoveryKey='x';newPassword='abc'}) -ContentType 'application/json' | Out-Null; throw "expected 400" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 400) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
T "auth" "POST /auth/reset-password full cycle (key->new->old fails)" {
    Invoke-WebRequest "$base/auth/reset-password" -Method POST -Body (ConvertTo-Json @{recoveryKey='ArNp4YNVJljrfCbYW8fe/bw5bc8hkvjorlNrYDG/f8c=';newPassword='AuditTmp@99'}) -ContentType 'application/json' | Out-Null
    Start-Sleep -Milliseconds 700
    $newAuth = Invoke-RestMethod "$base/auth/login" -Method POST -Body (ConvertTo-Json @{username='admin';password='AuditTmp@99'}) -ContentType 'application/json'
    if (-not $newAuth.accessToken) { throw "new login failed" }
    Invoke-WebRequest "$base/auth/change-password" -Method POST -Body (ConvertTo-Json @{currentPassword='AuditTmp@99';newPassword='Admin@123'}) -ContentType 'application/json' -Headers @{Authorization="Bearer $($newAuth.accessToken)"} | Out-Null
    Start-Sleep -Milliseconds 700
    $orig = Invoke-RestMethod "$base/auth/login" -Method POST -Body (ConvertTo-Json @{username='admin';password='Admin@123'}) -ContentType 'application/json'
    if (-not $orig.accessToken) { throw "restore failed" }
    $script:tk = $orig.accessToken; $script:h = @{Authorization="Bearer $tk"}
}

Write-Output ""
Write-Output "================================================================"
Write-Output " 3. CONFIG ENDPOINT"
Write-Output "================================================================"

T "cfg" "GET /config/public -> frontendUrl" {
    $c = Invoke-RestMethod "$base/config/public"
    if (-not $c.frontendUrl) { throw "missing url" }
}

Write-Output ""
Write-Output "================================================================"
Write-Output " 4. AUTHORIZATION GUARDS (public vs admin)"
Write-Output "================================================================"

$guards = @(
    @{ url='/sections';                    public=$true },
    @{ url='/sections/with-contents';      public=$true },
    @{ url='/contents';                    public=$true },
    @{ url='/contents/1';                  public=$true }
)
$adminEndpoints = @(
    @{ url='/sections/1/detail'; method='GET' },
    @{ url='/contents/1/detail'; method='GET' },
    @{ url='/sections';          method='POST' },
    @{ url='/contents';          method='POST' },
    @{ url='/media';             method='GET' },
    @{ url='/media/upload';      method='POST' }
)

foreach ($g in $guards) {
    $url = $g.url
    T "guard" "GET $url public access -> 200/404" {
        try {
            $r = Invoke-WebRequest "$base$url"
            if ($r.StatusCode -ne 200) { throw "got $($r.StatusCode)" }
        } catch {
            $code = $_.Exception.Response.StatusCode.value__
            if ($code -ne 200 -and $code -ne 404) { throw "got $code" }
        }
    }
}
foreach ($e in $adminEndpoints) {
    $url = $e.url; $method = $e.method
    T "guard" "$method $url no auth -> 401" {
        try {
            if ($method -eq 'GET') { Invoke-WebRequest "$base$url" | Out-Null }
            else { Invoke-WebRequest "$base$url" -Method $method -Body '{}' -ContentType 'application/json' | Out-Null }
            throw "expected 401"
        }
        catch { if ($_.Exception.Response.StatusCode.value__ -ne 401) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
    }
}
T "guard" "Garbage Bearer token -> 401" {
    try { Invoke-WebRequest "$base/contents/1/detail" -Headers @{Authorization="Bearer xxx"} | Out-Null; throw "expected 401" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 401) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}

Write-Output ""
Write-Output "================================================================"
Write-Output " 5. SECTIONS - full CRUD lifecycle"
Write-Output "================================================================"

$secId = $null
T "sec" "POST /sections (auto slug from cyrillic title)" {
    $b = ConvertTo-Json @{titleUz='AuditUZ';titleRu='AuditRU';titleEn='Audit Section EN';sortOrder=900;isActive=$true}
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($b)
    $req = [System.Net.HttpWebRequest]::Create("$base/sections")
    $req.Method = "POST"; $req.ContentType = "application/json; charset=utf-8"
    $req.Headers.Add("Authorization", "Bearer $tk"); $req.ContentLength = $bytes.Length
    $st = $req.GetRequestStream(); $st.Write($bytes, 0, $bytes.Length); $st.Close()
    $resp = $req.GetResponse(); $rd = New-Object System.IO.StreamReader($resp.GetResponseStream())
    $r = $rd.ReadToEnd() | ConvertFrom-Json
    if (-not $r.id) { throw "no id" }
    if ($r.slug -ne 'audit-section-en') { throw "slug=$($r.slug)" }
    $script:secId = $r.id
}
T "sec" "POST /sections empty title -> 400" {
    try { Invoke-WebRequest "$base/sections" -Method POST -Body '{"titleUz":"","titleRu":"","titleEn":""}' -ContentType 'application/json' -Headers $h | Out-Null; throw "expected 400" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 400) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
T "sec" "POST /sections invalid slug 'BAD!' -> 400" {
    try { Invoke-WebRequest "$base/sections" -Method POST -Body '{"slug":"BAD!","titleUz":"x","titleRu":"x","titleEn":"x"}' -ContentType 'application/json' -Headers $h | Out-Null; throw "expected 400" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 400) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
T "sec" "GET /sections (uz) - created section appears" {
    $list = Invoke-RestMethod "$base/sections?onlyActive=false"
    if (-not ($list | Where-Object { $_.id -eq $secId })) { throw "not in list" }
}
T "sec" "GET /sections?lang=ru -> RU title returned" {
    $list = Invoke-RestMethod "$base/sections?lang=ru&onlyActive=false"
    $found = $list | Where-Object { $_.id -eq $secId }
    if ($found.title -ne 'AuditRU') { throw "got $($found.title)" }
}
T "sec" "GET /sections?lang=en -> EN title" {
    $list = Invoke-RestMethod "$base/sections?lang=en&onlyActive=false"
    $found = $list | Where-Object { $_.id -eq $secId }
    if ($found.title -ne 'Audit Section EN') { throw "got $($found.title)" }
}
T "sec" "Accept-Language: ru header honored" {
    $list = Invoke-RestMethod "$base/sections?onlyActive=false" -Headers @{'Accept-Language'='ru'}
    $found = $list | Where-Object { $_.id -eq $secId }
    if ($found.title -ne 'AuditRU') { throw "got $($found.title)" }
}
T "sec" "GET /sections/{id}/detail returns all 3 langs" {
    $d = Invoke-RestMethod "$base/sections/$secId/detail" -Headers $h
    if ($d.titleUz -notmatch 'Audit') { throw "uz wrong" }
    if ($d.titleRu -notmatch 'Audit') { throw "ru wrong" }
    if ($d.titleEn -ne 'Audit Section EN') { throw "en wrong" }
}
T "sec" "GET /sections/9999/detail -> 404" {
    try { Invoke-RestMethod "$base/sections/9999/detail" -Headers $h | Out-Null; throw "expected 404" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 404) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
T "sec" "PUT /sections/{id} -> updates persist" {
    $b = ConvertTo-Json @{titleUz='UPD_UZ';titleRu='UPD_RU';titleEn='UPD_EN';sortOrder=901;isActive=$false}
    $upd = Invoke-RestMethod "$base/sections/$secId" -Method PUT -Body $b -ContentType 'application/json; charset=utf-8' -Headers $h
    if ($upd.titleUz -ne 'UPD_UZ') { throw "not updated" }
    if (-not $upd.updatedAt) { throw "no updatedAt" }
    # Verify via GET
    $verify = Invoke-RestMethod "$base/sections/$secId/detail" -Headers $h
    if ($verify.titleUz -ne 'UPD_UZ') { throw "DB not persisted" }
    if ($verify.isActive) { throw "isActive not persisted" }
}
T "sec" "PUT /sections/9999 -> 404" {
    try { Invoke-WebRequest "$base/sections/9999" -Method PUT -Body '{"titleUz":"x","titleRu":"x","titleEn":"x"}' -ContentType 'application/json' -Headers $h | Out-Null; throw "expected 404" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 404) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
T "sec" "DELETE /sections/{id} -> 204 then 404 on next GET" {
    $r = Invoke-WebRequest "$base/sections/$secId" -Method DELETE -Headers $h
    if ($r.StatusCode -ne 204) { throw "got $($r.StatusCode)" }
    try { Invoke-RestMethod "$base/sections/$secId/detail" -Headers $h | Out-Null; throw "still exists" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 404) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
T "sec" "DELETE /sections/9999 -> 404" {
    try { Invoke-WebRequest "$base/sections/9999" -Method DELETE -Headers $h | Out-Null; throw "expected 404" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 404) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}

Write-Output ""
Write-Output "================================================================"
Write-Output " 6. CONTENTS - full CRUD + cascade"
Write-Output "================================================================"

# Make a host section
$hostBody = ConvertTo-Json @{titleUz='HostSec';titleRu='HostSec';titleEn='HostSec';sortOrder=910;isActive=$true}
$hostBytes = [System.Text.Encoding]::UTF8.GetBytes($hostBody)
$hreq = [System.Net.HttpWebRequest]::Create("$base/sections")
$hreq.Method = "POST"; $hreq.ContentType = "application/json; charset=utf-8"
$hreq.Headers.Add("Authorization", "Bearer $tk"); $hreq.ContentLength = $hostBytes.Length
$hst = $hreq.GetRequestStream(); $hst.Write($hostBytes, 0, $hostBytes.Length); $hst.Close()
$hresp = $hreq.GetResponse(); $hrd = New-Object System.IO.StreamReader($hresp.GetResponseStream())
$hostSec = $hrd.ReadToEnd() | ConvertFrom-Json
$hostSecId = $hostSec.id

$ctId = $null
T "ct" "POST /contents (with sectionId, all 3 langs)" {
    $b = ConvertTo-Json @{
        sectionId=$hostSecId
        titleUz='Ct_UZ_AUDIT';titleRu='AuditCT_RU';titleEn='Ct_EN_AUDIT'
        descriptionUz='<p>Desc UZ <strong>HTML</strong></p>';descriptionRu='OpisRU';descriptionEn='Desc EN'
        imageUrl='/uploads/audit.jpg';sortOrder=10;isActive=$true
    }
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($b)
    $req = [System.Net.HttpWebRequest]::Create("$base/contents")
    $req.Method = "POST"; $req.ContentType = "application/json; charset=utf-8"
    $req.Headers.Add("Authorization", "Bearer $tk"); $req.ContentLength = $bytes.Length
    $st = $req.GetRequestStream(); $st.Write($bytes, 0, $bytes.Length); $st.Close()
    $resp = $req.GetResponse(); $rd = New-Object System.IO.StreamReader($resp.GetResponseStream())
    $r = $rd.ReadToEnd() | ConvertFrom-Json
    if ($r.sectionId -ne $hostSecId) { throw "wrong sectionId" }
    $script:ctId = $r.id
}
T "ct" "POST /contents empty image -> 400" {
    try { Invoke-WebRequest "$base/contents" -Method POST -Body "{`"sectionId`":$hostSecId,`"titleUz`":`"a`",`"titleRu`":`"a`",`"titleEn`":`"a`",`"descriptionUz`":`"a`",`"descriptionRu`":`"a`",`"descriptionEn`":`"a`",`"imageUrl`":`"`"}" -ContentType 'application/json' -Headers $h | Out-Null; throw "expected 400" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 400) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
T "ct" "POST /contents empty TitleEn -> 400" {
    try { Invoke-WebRequest "$base/contents" -Method POST -Body "{`"sectionId`":$hostSecId,`"titleUz`":`"a`",`"titleRu`":`"a`",`"titleEn`":`"`",`"descriptionUz`":`"a`",`"descriptionRu`":`"a`",`"descriptionEn`":`"a`",`"imageUrl`":`"/x.jpg`"}" -ContentType 'application/json' -Headers $h | Out-Null; throw "expected 400" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 400) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
T "ct" "POST /contents empty DescriptionRu -> 400" {
    try { Invoke-WebRequest "$base/contents" -Method POST -Body "{`"sectionId`":$hostSecId,`"titleUz`":`"a`",`"titleRu`":`"a`",`"titleEn`":`"a`",`"descriptionUz`":`"a`",`"descriptionRu`":`"`",`"descriptionEn`":`"a`",`"imageUrl`":`"/x.jpg`"}" -ContentType 'application/json' -Headers $h | Out-Null; throw "expected 400" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 400) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
T "ct" "GET /contents -> contains new content" {
    $r = Invoke-RestMethod "$base/contents?pageSize=100"
    $found = $r.items | Where-Object { $_.id -eq $ctId }
    if (-not $found) { throw "not in list" }
}
T "ct" "GET /contents?sectionId={id} filter" {
    $r = Invoke-RestMethod "$base/contents?sectionId=$hostSecId&pageSize=10"
    if ($r.totalCount -ne 1) { throw "count=$($r.totalCount)" }
    if ($r.items[0].id -ne $ctId) { throw "wrong content" }
}
T "ct" "GET /contents/{id}?lang=ru -> RU localization (with sectionTitle)" {
    $r = Invoke-RestMethod "$base/contents/$ctId`?lang=ru"
    if ($r.title -ne 'AuditCT_RU') { throw "title=$($r.title)" }
    if ($r.description -ne 'OpisRU') { throw "no RU desc" }
    if (-not $r.sectionTitle) { throw "no sectionTitle" }
}
T "ct" "GET /contents/{id} Accept-Language: en" {
    $r = Invoke-RestMethod "$base/contents/$ctId" -Headers @{'Accept-Language'='en'}
    if ($r.title -ne 'Ct_EN_AUDIT') { throw "got $($r.title)" }
}
T "ct" "GET /contents/{id}/detail returns all 3 langs + HTML preserved" {
    $d = Invoke-RestMethod "$base/contents/$ctId/detail" -Headers $h
    if (-not $d.titleUz -or -not $d.titleRu -or -not $d.titleEn) { throw "missing langs" }
    if ($d.descriptionUz -notmatch 'HTML') { throw "HTML stripped" }
    if ($d.descriptionRu -notmatch 'OpisRU') { throw "RU lost" }
}
T "ct" "GET /contents/9999 -> 404" {
    try { Invoke-RestMethod "$base/contents/9999" | Out-Null; throw "expected 404" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 404) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
T "ct" "PUT /contents/{id} -> persisted" {
    $b = ConvertTo-Json @{sectionId=$hostSecId;titleUz='UPD_CT';titleRu='UPD_CT';titleEn='UPD_CT';descriptionUz='upd';descriptionRu='upd';descriptionEn='upd';imageUrl='/upd.jpg';sortOrder=11;isActive=$false}
    $upd = Invoke-RestMethod "$base/contents/$ctId" -Method PUT -Body $b -ContentType 'application/json; charset=utf-8' -Headers $h
    if ($upd.titleUz -ne 'UPD_CT') { throw "not updated" }
    if (-not $upd.updatedAt) { throw "no updatedAt" }
    $verify = Invoke-RestMethod "$base/contents/$ctId/detail" -Headers $h
    if ($verify.titleUz -ne 'UPD_CT') { throw "DB not persisted" }
    if ($verify.imageUrl -ne '/upd.jpg') { throw "image not persisted" }
}
T "ct" "DELETE /contents/{id} -> 204 then 404" {
    $r = Invoke-WebRequest "$base/contents/$ctId" -Method DELETE -Headers $h
    if ($r.StatusCode -ne 204) { throw "got $($r.StatusCode)" }
    try { Invoke-RestMethod "$base/contents/$ctId" | Out-Null; throw "still" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 404) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}

# Cascade test
T "ct" "Cascade delete: section delete removes all children" {
    $b1 = ConvertTo-Json @{sectionId=$hostSecId;titleUz='C1';titleRu='C1';titleEn='C1';descriptionUz='d';descriptionRu='d';descriptionEn='d';imageUrl='/x.jpg';sortOrder=1;isActive=$true}
    $b2 = ConvertTo-Json @{sectionId=$hostSecId;titleUz='C2';titleRu='C2';titleEn='C2';descriptionUz='d';descriptionRu='d';descriptionEn='d';imageUrl='/x.jpg';sortOrder=2;isActive=$true}
    $c1 = Invoke-RestMethod "$base/contents" -Method POST -Body $b1 -ContentType 'application/json; charset=utf-8' -Headers $h
    $c2 = Invoke-RestMethod "$base/contents" -Method POST -Body $b2 -ContentType 'application/json; charset=utf-8' -Headers $h
    Invoke-WebRequest "$base/sections/$hostSecId" -Method DELETE -Headers $h | Out-Null
    try { Invoke-RestMethod "$base/contents/$($c1.id)" | Out-Null; throw "child 1 not cascaded" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 404) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
    try { Invoke-RestMethod "$base/contents/$($c2.id)" | Out-Null; throw "child 2 not cascaded" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 404) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}

Write-Output ""
Write-Output "================================================================"
Write-Output " 7. MEDIA - upload/get/delete + disk verification"
Write-Output "================================================================"

T "md" "GET /media -> 5 items (DB state)" {
    $r = Invoke-RestMethod "$base/media" -Headers $h
    if ($r.totalCount -ne 5) { throw "count=$($r.totalCount)" }
}
T "md" "GET /media?pageSize=2 -> 2 items, totalPages>=3" {
    $r = Invoke-RestMethod "$base/media?pageSize=2" -Headers $h
    if ($r.items.Count -ne 2) { throw "items=$($r.items.Count)" }
    if ($r.totalPages -lt 3) { throw "totalPages=$($r.totalPages)" }
}
T "md" "GET /media/{id} -> metadata" {
    $list = Invoke-RestMethod "$base/media?pageSize=1" -Headers $h
    $m = Invoke-RestMethod "$base/media/$($list.items[0].id)" -Headers $h
    if (-not $m.url) { throw "no url" }
    if (-not $m.contentType) { throw "no contentType" }
}
T "md" "GET /media/9999 -> 404" {
    try { Invoke-RestMethod "$base/media/9999" -Headers $h | Out-Null; throw "expected 404" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 404) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
T "md" "POST /media/upload empty file -> 400" {
    $LF = "`r`n"; $boundary = [Guid]::NewGuid().ToString()
    $body = "--$boundary$LF" + 'Content-Disposition: form-data; name="file"; filename="empty.txt"' + "$LF$LF$LF--$boundary--$LF"
    try { Invoke-WebRequest "$base/media/upload" -Method POST -ContentType "multipart/form-data; boundary=$boundary" -Body $body -Headers $h | Out-Null; throw "expected 400" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 400) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
T "md" "POST upload + DELETE roundtrip (DB + disk)" {
    $LF = "`r`n"; $boundary = [Guid]::NewGuid().ToString()
    $body = "--$boundary$LF" + 'Content-Disposition: form-data; name="file"; filename="audit-roundtrip.txt"' + "$LF" + 'Content-Type: text/plain' + "$LF$LF" + 'roundtrip test' + "$LF--$boundary--$LF"
    $r = Invoke-RestMethod "$base/media/upload" -Method POST -ContentType "multipart/form-data; boundary=$boundary" -Body $body -Headers $h
    if (-not $r.url) { throw "no url" }
    $diskPath = "C:\Users\AIZENN\Desktop\veterenarya.backend\src\VeterinaryBackend.API\wwwroot$($r.url.Replace('/','\'))"
    if (-not (Test-Path $diskPath)) { throw "file not on disk" }
    Invoke-WebRequest "$base/media/$($r.id)" -Method DELETE -Headers $h | Out-Null
    if (Test-Path $diskPath) { throw "file still on disk after delete" }
    try { Invoke-RestMethod "$base/media/$($r.id)" -Headers $h | Out-Null; throw "still in DB" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 404) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
T "md" "Public URL access (no auth required) -> 200 + content" {
    $list = Invoke-RestMethod "$base/media?pageSize=1" -Headers $h
    $r = Invoke-WebRequest "http://localhost:5099$($list.items[0].url)"
    if ($r.StatusCode -ne 200) { throw "got $($r.StatusCode)" }
}

Write-Output ""
Write-Output "================================================================"
Write-Output " 8. PAGINATION + EDGE CASES"
Write-Output "================================================================"

T "edge" "page=0 normalizes to 1" { $r = Invoke-RestMethod "$base/contents?page=0&pageSize=5"; if ($r.page -ne 1) { throw "page=$($r.page)" } }
T "edge" "page=-5 normalizes to 1" { $r = Invoke-RestMethod "$base/contents?page=-5"; if ($r.page -ne 1) { throw "page=$($r.page)" } }
T "edge" "pageSize=999 caps at 100" { $r = Invoke-RestMethod "$base/contents?pageSize=999"; if ($r.pageSize -ne 100) { throw "ps=$($r.pageSize)" } }
T "edge" "pageSize=-1 -> default" { $r = Invoke-RestMethod "$base/contents?pageSize=-1"; if ($r.pageSize -le 0) { throw "ps=$($r.pageSize)" } }
T "edge" "Hugely-paged sectionId=0 -> empty list, valid response" {
    $r = Invoke-RestMethod "$base/contents?sectionId=0"
    if ($r.totalCount -ne 0) { throw "count=$($r.totalCount)" }
}

Write-Output ""
Write-Output "================================================================"
Write-Output " 9. DATA INTEGRITY (15 contents, 4 sections, 5 media)"
Write-Output "================================================================"

$secs = Invoke-RestMethod "$base/sections"
T "data" "Sections count = 4" { if ($secs.Count -ne 4) { throw "got $($secs.Count)" } }
$pub = Invoke-RestMethod "$base/sections/with-contents"
T "data" "/sections/with-contents = 4 sections" { if ($pub.Count -ne 4) { throw "got $($pub.Count)" } }
T "data" "Total content blocks = 15" {
    $tot = ($pub | ForEach-Object { $_.items.Count } | Measure-Object -Sum).Sum
    if ($tot -ne 15) { throw "got $tot" }
}
T "data" "Media count = 5" { $r = Invoke-RestMethod "$base/media" -Headers $h; if ($r.totalCount -ne 5) { throw "got $($r.totalCount)" } }
$rowFail = 0; $imgInternalOk = 0; $imgInternalTotal = 0
foreach ($s in $pub) {
    foreach ($i in $s.items) {
        $d = Invoke-RestMethod "$base/contents/$($i.id)/detail" -Headers $h
        if (-not $d.titleUz -or -not $d.titleRu -or -not $d.titleEn -or `
            -not $d.descriptionUz -or -not $d.descriptionRu -or -not $d.descriptionEn -or `
            -not $d.imageUrl -or -not $d.sectionId) { $rowFail++ }
        if ($i.imageUrl.StartsWith('/uploads')) {
            $imgInternalTotal++
            try {
                $ir = Invoke-WebRequest "http://localhost:5099$($i.imageUrl)" -Method HEAD -TimeoutSec 5
                if ($ir.StatusCode -eq 200) { $imgInternalOk++ }
            } catch {}
        }
    }
}
T "data" "All 15 contents have all required fields" { if ($rowFail -gt 0) { throw "$rowFail rows incomplete" } }
T "data" "All internal upload URLs reachable (5/5)" { if ($imgInternalOk -ne $imgInternalTotal) { throw "only $imgInternalOk/$imgInternalTotal" } }

Write-Output ""
Write-Output "================================================================"
Write-Output " 10. ADMIN PANEL UI - every static element"
Write-Output "================================================================"

$html = (Invoke-WebRequest "$adm/").Content

$adminIds = @(
    'login-screen','login-form','login-btn','forgot-password-btn',
    'theme-btn','back-to-site-btn','user-menu-btn','user-menu','change-password-btn','logout-btn',
    'sidebar','sidebar-toggle','mobile-menu',
    'tab-sections','tab-contents','tab-media',
    'new-section-btn','new-content-btn','media-upload-input','media-dropzone',
    'sections-tbody','contents-tbody','media-grid',
    'section-modal','section-form','section-save-btn',
    'content-modal','content-form','content-save-btn','content-form-error',
    'content-image-empty','content-image-preview','content-image-remove',
    'pick-from-media-btn','content-quick-upload','content-jump-to-sections',
    'lang-fill-status',
    'media-picker-modal','media-picker-grid',
    'confirm-modal','confirm-title','confirm-message','confirm-ok','confirm-cancel',
    'change-password-modal','change-password-form','cp-save-btn','cp-error',
    'reset-password-modal','reset-password-form','rp-save-btn','rp-error',
    'toast-container','user-name','user-initial'
)
foreach ($id in $adminIds) {
    T "ui" "Element #$id present" { if ($html -notmatch [regex]::Escape("id=`"$id`"")) { throw "missing" } }
}
T "ui" "3 sidebar tabs (data-tab)" {
    if ($html -notmatch 'data-tab="sections"') { throw "sections tab" }
    if ($html -notmatch 'data-tab="contents"') { throw "contents tab" }
    if ($html -notmatch 'data-tab="media"') { throw "media tab" }
}
T "ui" "3 lang tabs in content modal" {
    if ($html -notmatch 'data-lang="Uz"' -or $html -notmatch 'data-lang="Ru"' -or $html -notmatch 'data-lang="En"') { throw "lang tabs" }
}
T "ui" "Image picker BIG cards (image-picker-card)" { if ($html -notmatch 'image-picker-card') { throw "missing" } }
T "ui" "Stats overview cards (12 stat-cards)" {
    $count = ([regex]::Matches($html, 'stat-card-')).Count
    if ($count -lt 12) { throw "only $count stat-cards" }
}
T "ui" "Help banners (3 tabs each)" {
    $count = ([regex]::Matches($html, "Bu yerda nima qilasiz")).Count
    if ($count -lt 3) { throw "only $count banners" }
}
T "ui" "Numbered tab badges (1, 2, 3)" {
    if ($html -notmatch 'tab-num-badge') { throw "no tab-num-badge class" }
}

Write-Output ""
Write-Output "================================================================"
Write-Output " 11. ADMIN JS METHODS (every API call + handler bound)"
Write-Output "================================================================"

$jsApi = (Invoke-WebRequest "$adm/js/api.js").Content
$jsAuth = (Invoke-WebRequest "$adm/js/auth.js").Content
$jsCt = (Invoke-WebRequest "$adm/js/contents.js").Content
$jsSec = (Invoke-WebRequest "$adm/js/sections.js").Content
$jsMd = (Invoke-WebRequest "$adm/js/media.js").Content
$jsApp = (Invoke-WebRequest "$adm/js/app.js").Content
$jsUi = (Invoke-WebRequest "$adm/js/ui.js").Content

$apiMethods = @('login','changePassword','resetPassword','getPublicConfig','listSections','createSection','updateSection','deleteSection','getSectionDetail','listContents','createContent','updateContent','deleteContent','getContentDetail','listMedia','uploadMedia','deleteMedia')
foreach ($m in $apiMethods) {
    T "js" "api.$m method present" { if ($jsApi -notmatch $m) { throw "missing" } }
}
$authBindings = @('bindChangePassword','bindResetPassword','bindBackToSite','closeAllOverlays')
foreach ($b in $authBindings) {
    T "js" "auth.$b" { if ($jsAuth -notmatch $b) { throw "missing" } }
}
$secMethods = @('init','load','renderRows','updateStats','openModal','closeModal','save','deleteRow')
foreach ($m in $secMethods) {
    T "js" "Sections.$m" { if ($jsSec -notmatch $m) { throw "missing" } }
}
$ctMethods = @('init','load','renderRows','updateStats','openModal','closeModal','save','deleteRow','openMediaPicker','quickUpload','refreshSectionDropdown','updateLangFillStatus','updatePreview','setImage','switchLang')
foreach ($m in $ctMethods) {
    T "js" "Contents.$m" { if ($jsCt -notmatch $m) { throw "missing" } }
}
$mdMethods = @('init','load','renderGrid','updateStats','handleFiles','deleteOne')
foreach ($m in $mdMethods) {
    T "js" "Media.$m" { if ($jsMd -notmatch $m) { throw "missing" } }
}
T "js" "App.bindThemeToggle" { if ($jsApp -notmatch 'bindThemeToggle') { throw "missing" } }
T "js" "App.switchTab" { if ($jsApp -notmatch 'switchTab') { throw "missing" } }
T "js" "Global ESC handler" { if ($jsApp -notmatch 'Escape') { throw "missing" } }
T "js" "UI.toast" { if ($jsUi -notmatch 'toast') { throw "missing" } }
T "js" "UI.confirm" { if ($jsUi -notmatch 'confirm') { throw "missing" } }
T "js" "UI.formatBytes" { if ($jsUi -notmatch 'formatBytes') { throw "missing" } }

Write-Output ""
Write-Output "================================================================"
Write-Output " 12. ADMIN CSS - visual classes present"
Write-Output "================================================================"

$css = (Invoke-WebRequest "$adm/css/style.css").Content
$cssClasses = @('nav-link','nav-link.active','lang-tab','lang-tab.active','toast','file-icon-image','media-tile','media-tile:hover','dropzone-active','image-picker-card','quickstart-step','quickstart-num','lang-tab-dot','admin-bg','tab-num-badge','stat-card','stat-card-indigo','stat-card-emerald','stat-card-amber','stat-card-rose','row-avatar','fancy-row')
foreach ($c in $cssClasses) {
    T "css" "Class .$c defined" { if ($css -notmatch [regex]::Escape($c)) { throw "missing" } }
}

Write-Output ""
Write-Output "================================================================"
Write-Output " 13. FRONTEND UI"
Write-Output "================================================================"

$fhtml = (Invoke-WebRequest "$front/").Content
$frontIds = @('home','sections','contact','navbar','primary-nav','mobile-menu','mobile-nav-list','mobile-menu-btn','theme-btn','lang-btn','lang-menu','lang-current','admin-link','admin-link-mobile','admin-link-footer','sections-list','footer-year','back-to-top','nav-contact','mobile-nav-contact')
foreach ($id in $frontIds) {
    T "front" "Frontend element #$id" { if ($fhtml -notmatch [regex]::Escape("id=`"$id`"")) { throw "missing" } }
}
T "front" "VetSafe brand" { if ($fhtml -notmatch 'VetSafe') { throw "missing" } }
T "front" "Dark mode config (class)" { if ($fhtml -notmatch 'darkMode') { throw "missing" } }
T "front" "Cache-control meta tag" { if ($fhtml -notmatch 'http-equiv="Cache-Control"') { throw "missing" } }
T "front" "Hero CTA buttons (primary + secondary)" {
    if ($fhtml -notmatch 'data-i18n="hero.cta_primary"' -or $fhtml -notmatch 'data-i18n="hero.cta_secondary"') { throw "missing CTAs" }
}

$fapi = (Invoke-WebRequest "$front/js/api.js").Content
$fapp = (Invoke-WebRequest "$front/js/app.js").Content
$fi18n = (Invoke-WebRequest "$front/js/i18n.js").Content

T "front" "api.getContents()" { if ($fapi -notmatch 'getContents') { throw "missing" } }
T "front" "api.getSectionsWithContents()" { if ($fapi -notmatch 'getSectionsWithContents') { throw "missing" } }
T "front" "api.resolveMediaUrl()" { if ($fapi -notmatch 'resolveMediaUrl') { throw "missing" } }
T "front" "App.init()" { if ($fapp -notmatch 'init\s*\(') { throw "missing" } }
T "front" "App.applyTranslations()" { if ($fapp -notmatch 'applyTranslations') { throw "missing" } }
T "front" "App.setLanguage()" { if ($fapp -notmatch 'setLanguage') { throw "missing" } }
T "front" "App.bindThemeToggle()" { if ($fapp -notmatch 'bindThemeToggle') { throw "missing" } }
T "front" "App.bindMobileMenu()" { if ($fapp -notmatch 'bindMobileMenu') { throw "missing" } }
T "front" "App.bindBackToTop()" { if ($fapp -notmatch 'bindBackToTop') { throw "missing" } }
T "front" "App.bindScrollAnimations()" { if ($fapp -notmatch 'bindScrollAnimations') { throw "missing" } }
T "front" "App.rebuildNav()" { if ($fapp -notmatch 'rebuildNav') { throw "missing" } }
T "front" "App.renderSectionGroup()" { if ($fapp -notmatch 'renderSectionGroup') { throw "missing" } }
T "front" "App.renderContentBlock()" { if ($fapp -notmatch 'renderContentBlock') { throw "missing" } }
T "front" "App.renderDescription() (HTML/JSON/text)" { if ($fapp -notmatch 'renderDescription') { throw "missing" } }
T "front" "i18n: 3 languages defined" {
    if ($fi18n -notmatch 'uz:\s*\{' -or $fi18n -notmatch 'ru:\s*\{' -or $fi18n -notmatch 'en:\s*\{') { throw "missing langs" }
}
T "front" "Live: backend gives 4 sections, 15 items in UZ" {
    $r = Invoke-RestMethod "$base/sections/with-contents?lang=uz"
    if ($r.Count -ne 4) { throw "$($r.Count)" }
    $tot = ($r | ForEach-Object { $_.items.Count } | Measure-Object -Sum).Sum
    if ($tot -ne 15) { throw "items=$tot" }
}

Write-Output ""
Write-Output "================================================================"
Write-Output " 14. STATIC ASSETS (admin + frontend)"
Write-Output "================================================================"

$adminAssets = @('/admin/css/style.css','/admin/js/api.js','/admin/js/ui.js','/admin/js/auth.js','/admin/js/sections.js','/admin/js/contents.js','/admin/js/media.js','/admin/js/app.js')
foreach ($a in $adminAssets) {
    T "static" "GET http://localhost:5099$a -> 200" {
        $r = Invoke-WebRequest "http://localhost:5099$a"
        if ($r.StatusCode -ne 200) { throw "got $($r.StatusCode)" }
        if ($r.Content.Length -lt 100) { throw "tiny: $($r.Content.Length)" }
    }
}
$frontAssets = @('/css/style.css','/js/config.js','/js/i18n.js','/js/api.js','/js/app.js')
foreach ($a in $frontAssets) {
    T "static" "GET http://localhost:8080$a -> 200" {
        $r = Invoke-WebRequest "http://localhost:8080$a"
        if ($r.StatusCode -ne 200) { throw "got $($r.StatusCode)" }
    }
}

Write-Output ""
Write-Output "================================================================"
Write-Output " 15. CROSS-CUTTING (CORS, cache, security)"
Write-Output "================================================================"

T "x" "CORS Allow-Origin header" {
    $r = Invoke-WebRequest "$base/sections" -Headers @{'Origin'='http://localhost:8080'}
    if (-not $r.Headers['Access-Control-Allow-Origin']) { throw "missing" }
}
T "x" "Admin static no-cache" {
    $r = Invoke-WebRequest "$adm/js/api.js" -Method HEAD
    if (-not ($r.Headers['Cache-Control'] -like '*no-cache*')) { throw "wrong" }
}
T "x" "Public uploads cacheable (no no-store)" {
    $list = Invoke-RestMethod "$base/media?pageSize=1" -Headers $h
    $r = Invoke-WebRequest "http://localhost:5099$($list.items[0].url)" -Method HEAD
    if ($r.Headers['Cache-Control'] -like '*no-store*') { throw "uploads should not be no-store" }
}
$cfg = Get-Content "C:\Users\AIZENN\Desktop\veterenarya.backend\src\VeterinaryBackend.API\appsettings.json" | ConvertFrom-Json
T "x" "BCrypt cost factor 11" { if ($cfg.Admin.PasswordHash.Substring(4, 2) -ne '11') { throw "wrong" } }
T "x" "JWT secret >= 256 bits" {
    $len = [Convert]::FromBase64String($cfg.Jwt.SecretKey).Length * 8
    if ($len -lt 256) { throw "$len bits" }
}
T "x" "Recovery key >= 128 bits" {
    $len = [Convert]::FromBase64String($cfg.Admin.RecoveryKey).Length * 8
    if ($len -lt 128) { throw "$len bits" }
}
T "x" "JWT correctly signed (verify protected endpoint)" {
    $r = Invoke-WebRequest "$base/media" -Headers $h
    if ($r.StatusCode -ne 200) { throw "got $($r.StatusCode)" }
}

Write-Output ""
Write-Output "================================================================"
$total = $ok + $fail
$pct = if ($total -gt 0) { [Math]::Round(($ok/$total)*100, 1) } else { 0 }
Write-Output ("  RESULT:  PASS $ok   FAIL $fail   ({0}%)" -f $pct)
if ($fail -gt 0) {
    Write-Output ""
    Write-Output "  FAILED:"
    $failed | ForEach-Object { Write-Output ("   - $_") }
}
Write-Output "================================================================"
