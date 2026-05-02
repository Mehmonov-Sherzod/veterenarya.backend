[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ProgressPreference = 'SilentlyContinue'
$base = "http://localhost:5099/api/v1"
$ok = 0
$fail = 0
$failedTests = @()

function Test {
    param([string]$cat, [string]$name, [scriptblock]$block)
    Write-Host -NoNewline ("  > {0,-58} " -f $name)
    try {
        & $block
        Write-Host "PASS" -ForegroundColor Green
        $script:ok++
    } catch {
        $msg = $_.Exception.Message
        if ($msg.Length -gt 70) { $msg = $msg.Substring(0, 70) + "..." }
        Write-Host ("FAIL " + $msg) -ForegroundColor Red
        $script:fail++
        $script:failedTests += "[$cat] $name : $msg"
    }
}

# Login once and reuse token
$loginBody = ConvertTo-Json @{ username='admin'; password='Admin@123' }
$auth = Invoke-RestMethod -Uri "$base/auth/login" -Method POST -Body $loginBody -ContentType 'application/json'
$tk = $auth.accessToken
$h = @{ Authorization = "Bearer $tk" }

Write-Output "================================================================"
Write-Output " PHASE 1 - DATABASE INTEGRITY (har bir qator hatolarsiz)"
Write-Output "================================================================"

$env:PGPASSWORD = "Sherzod3466"
$psql = "C:\Program Files\PostgreSQL\18\bin\psql.exe"

function Q($q) {
    & $psql -h localhost -p 5433 -U postgres -d veterinary_db -t -A -c $q 2>$null
}

Test "db" "Sections count = 4" { $n = (Q 'SELECT count(*) FROM sections;').Trim(); if ($n -ne '4') { throw "got $n" } }
Test "db" "Contents count = 15" { $n = (Q 'SELECT count(*) FROM contents;').Trim(); if ($n -ne '15') { throw "got $n" } }
Test "db" "Media count = 5" { $n = (Q 'SELECT count(*) FROM media_files;').Trim(); if ($n -ne '5') { throw "got $n" } }
Test "db" "All sections TitleUz non-empty" { $n = (Q 'SELECT count(*) FROM sections WHERE "TitleUz" IS NULL OR length("TitleUz")=0;').Trim(); if ($n -ne '0') { throw "$n empty" } }
Test "db" "All sections TitleRu non-empty" { $n = (Q 'SELECT count(*) FROM sections WHERE "TitleRu" IS NULL OR length("TitleRu")=0;').Trim(); if ($n -ne '0') { throw "$n empty" } }
Test "db" "All sections TitleEn non-empty" { $n = (Q 'SELECT count(*) FROM sections WHERE "TitleEn" IS NULL OR length("TitleEn")=0;').Trim(); if ($n -ne '0') { throw "$n empty" } }
Test "db" "Section slugs all unique" { $n = (Q 'SELECT count(*) - count(DISTINCT "Slug") FROM sections;').Trim(); if ($n -ne '0') { throw "duplicates" } }
Test "db" "All contents have valid SectionId FK" { $n = (Q 'SELECT count(*) FROM contents WHERE "SectionId" IS NULL OR "SectionId" NOT IN (SELECT "Id" FROM sections);').Trim(); if ($n -ne '0') { throw "$n orphans" } }
Test "db" "All contents TitleUz non-empty" { $n = (Q 'SELECT count(*) FROM contents WHERE "TitleUz" IS NULL OR length("TitleUz")=0;').Trim(); if ($n -ne '0') { throw "$n empty" } }
Test "db" "All contents TitleRu non-empty" { $n = (Q 'SELECT count(*) FROM contents WHERE "TitleRu" IS NULL OR length("TitleRu")=0;').Trim(); if ($n -ne '0') { throw "$n empty" } }
Test "db" "All contents TitleEn non-empty" { $n = (Q 'SELECT count(*) FROM contents WHERE "TitleEn" IS NULL OR length("TitleEn")=0;').Trim(); if ($n -ne '0') { throw "$n empty" } }
Test "db" "All contents DescriptionUz non-empty" { $n = (Q 'SELECT count(*) FROM contents WHERE "DescriptionUz" IS NULL OR length("DescriptionUz")=0;').Trim(); if ($n -ne '0') { throw "$n empty" } }
Test "db" "All contents DescriptionRu non-empty" { $n = (Q 'SELECT count(*) FROM contents WHERE "DescriptionRu" IS NULL OR length("DescriptionRu")=0;').Trim(); if ($n -ne '0') { throw "$n empty" } }
Test "db" "All contents DescriptionEn non-empty" { $n = (Q 'SELECT count(*) FROM contents WHERE "DescriptionEn" IS NULL OR length("DescriptionEn")=0;').Trim(); if ($n -ne '0') { throw "$n empty" } }
Test "db" "All contents ImageUrl non-empty" { $n = (Q 'SELECT count(*) FROM contents WHERE "ImageUrl" IS NULL OR length("ImageUrl")=0;').Trim(); if ($n -ne '0') { throw "$n empty" } }
Test "db" "All contents have CreatedAt" { $n = (Q 'SELECT count(*) FROM contents WHERE "CreatedAt" IS NULL;').Trim(); if ($n -ne '0') { throw "$n null" } }
Test "db" "All media have non-empty Url" { $n = (Q 'SELECT count(*) FROM media_files WHERE "Url" IS NULL OR length("Url")=0;').Trim(); if ($n -ne '0') { throw "$n empty" } }
Test "db" "All media have ContentType" { $n = (Q 'SELECT count(*) FROM media_files WHERE "ContentType" IS NULL OR length("ContentType")=0;').Trim(); if ($n -ne '0') { throw "$n empty" } }
Test "db" "All media files exist on disk" {
    $rows = & $psql -h localhost -p 5433 -U postgres -d veterinary_db -t -A -c 'SELECT "RelativePath" FROM media_files;' 2>$null
    $missing = 0
    foreach ($rp in $rows) {
        if ($rp.Trim() -eq '') { continue }
        $abs = "C:\Users\AIZENN\Desktop\veterenarya.backend\src\VeterinaryBackend.API\wwwroot\$($rp.Replace('/','\'))"
        if (-not (Test-Path $abs)) { $missing++ }
    }
    if ($missing -gt 0) { throw "$missing files missing" }
}
Test "db" "FK constraint contents-sections present" { $n = (Q "SELECT count(*) FROM information_schema.table_constraints WHERE constraint_name='FK_contents_sections_SectionId';").Trim(); if ($n -ne '1') { throw "missing" } }
Test "db" "Cascade delete configured" { $n = (Q "SELECT count(*) FROM information_schema.referential_constraints WHERE constraint_name='FK_contents_sections_SectionId' AND delete_rule='CASCADE';").Trim(); if ($n -ne '1') { throw "no cascade" } }
Test "db" "Indexes on sections (>=4)" { $n = (Q "SELECT count(*) FROM pg_indexes WHERE tablename='sections';").Trim(); if ([int]$n -lt 4) { throw "only $n" } }
Test "db" "Indexes on contents (>=4)" { $n = (Q "SELECT count(*) FROM pg_indexes WHERE tablename='contents';").Trim(); if ([int]$n -lt 4) { throw "only $n" } }

Write-Output ""
Write-Output "================================================================"
Write-Output " PHASE 2 - EVERY API METHOD (POST/GET/PUT/DELETE)"
Write-Output "================================================================"

$wrongLogin = ConvertTo-Json @{ username='admin'; password='wrong' }
$correctLogin = ConvertTo-Json @{ username='admin'; password='Admin@123' }
$wrongChange = ConvertTo-Json @{ currentPassword='wrong'; newPassword='NewPass123' }
$shortChange = ConvertTo-Json @{ currentPassword='Admin@123'; newPassword='abc' }
$wrongReset = ConvertTo-Json @{ recoveryKey='wrong'; newPassword='NewPass123' }

Test "auth" "POST /auth/login wrong -> 401" {
    try { Invoke-RestMethod "$base/auth/login" -Method POST -Body $wrongLogin -ContentType 'application/json' | Out-Null; throw "expected 401" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 401) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
Test "auth" "POST /auth/login correct -> 200 + JWT" {
    $a = Invoke-RestMethod "$base/auth/login" -Method POST -Body $correctLogin -ContentType 'application/json'
    if (-not $a.accessToken) { throw "no token" }
}
Test "auth" "POST /auth/change-password no auth -> 401" {
    try { Invoke-WebRequest "$base/auth/change-password" -Method POST -Body '{}' -ContentType 'application/json' | Out-Null; throw "expected 401" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 401) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
Test "auth" "POST /auth/change-password wrong current -> 401" {
    try { Invoke-WebRequest "$base/auth/change-password" -Method POST -Body $wrongChange -ContentType 'application/json' -Headers $h | Out-Null; throw "expected 401" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 401) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
Test "auth" "POST /auth/change-password too short -> 400" {
    try { Invoke-WebRequest "$base/auth/change-password" -Method POST -Body $shortChange -ContentType 'application/json' -Headers $h | Out-Null; throw "expected 400" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 400) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
Test "auth" "POST /auth/reset-password wrong key -> 401" {
    try { Invoke-RestMethod "$base/auth/reset-password" -Method POST -Body $wrongReset -ContentType 'application/json' | Out-Null; throw "expected 401" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 401) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
Test "config" "GET /config/public -> 200 + frontendUrl" {
    $c = Invoke-RestMethod "$base/config/public"; if (-not $c.frontendUrl) { throw "no url" }
}
Test "sec" "GET /sections (uz) -> 4 items" { $r = Invoke-RestMethod "$base/sections"; if ($r.Count -ne 4) { throw "count=$($r.Count)" } }
Test "sec" "GET /sections?lang=ru -> RU titles" { $r = Invoke-RestMethod "$base/sections?lang=ru"; if ($r[0].title -notmatch '^[А-Яа-я]') { throw "not RU" } }
Test "sec" "GET /sections?lang=en -> EN titles" { $r = Invoke-RestMethod "$base/sections?lang=en"; if ($r[0].title -notmatch '^[A-Z]') { throw "not EN" } }
Test "sec" "GET /sections?onlyActive=false" { $r = Invoke-RestMethod "$base/sections?onlyActive=false"; if ($r.Count -lt 4) { throw "$($r.Count)" } }
Test "sec" "GET /sections/with-contents -> 4 sec, 15 items" {
    $r = Invoke-RestMethod "$base/sections/with-contents"
    if ($r.Count -ne 4) { throw "sections=$($r.Count)" }
    $tot = ($r | ForEach-Object { $_.items.Count } | Measure-Object -Sum).Sum
    if ($tot -ne 15) { throw "items=$tot" }
}
Test "sec" "GET /sections/{id}/detail (admin)" {
    $list = Invoke-RestMethod "$base/sections"
    $d = Invoke-RestMethod "$base/sections/$($list[0].id)/detail" -Headers $h
    if (-not $d.titleUz -or -not $d.titleRu -or -not $d.titleEn) { throw "missing langs" }
}
Test "sec" "GET /sections/9999/detail -> 404" {
    try { Invoke-RestMethod "$base/sections/9999/detail" -Headers $h | Out-Null; throw "expected 404" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 404) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
Test "sec" "Section CRUD: POST + PUT + DELETE roundtrip" {
    $b = ConvertTo-Json @{titleUz='_TestSec';titleRu='_TestSec';titleEn='_TestSec';sortOrder=999;isActive=$true}
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($b)
    $req = [System.Net.HttpWebRequest]::Create("$base/sections")
    $req.Method = "POST"
    $req.ContentType = "application/json; charset=utf-8"
    $req.Headers.Add("Authorization", "Bearer $tk")
    $req.ContentLength = $bytes.Length
    $st = $req.GetRequestStream(); $st.Write($bytes, 0, $bytes.Length); $st.Close()
    $resp = $req.GetResponse(); $rd = New-Object System.IO.StreamReader($resp.GetResponseStream())
    $r = $rd.ReadToEnd() | ConvertFrom-Json
    $b2 = ConvertTo-Json @{titleUz='_Upd';titleRu='_Upd';titleEn='_Upd';sortOrder=998;isActive=$false}
    $upd = Invoke-RestMethod "$base/sections/$($r.id)" -Method PUT -Body $b2 -ContentType 'application/json; charset=utf-8' -Headers $h
    if ($upd.titleUz -ne '_Upd') { throw "not updated" }
    Invoke-WebRequest "$base/sections/$($r.id)" -Method DELETE -Headers $h | Out-Null
}

Test "ct" "GET /contents -> 15 items" {
    $r = Invoke-RestMethod "$base/contents`?pageSize=20"; if ($r.totalCount -ne 15) { throw "$($r.totalCount)" }
}
Test "ct" "GET /contents?lang=ru -> RU title" {
    $r = Invoke-RestMethod "$base/contents`?lang=ru`&pageSize=1"
    if ($r.items[0].title -notmatch '[А-Яа-я]') { throw "not RU" }
}
Test "ct" "GET /contents Accept-Language: en" {
    $r = Invoke-RestMethod "$base/contents`?pageSize=1" -Headers @{'Accept-Language'='en'}
    if ($r.items[0].title -match '[А-Яа-я]') { throw "got Cyrillic" }
}
Test "ct" "GET /contents?sectionId filter -> 4 items" {
    $list = Invoke-RestMethod "$base/sections"
    $r = Invoke-RestMethod "$base/contents`?sectionId=$($list[0].id)`&pageSize=20"
    if ($r.totalCount -ne 4) { throw "$($r.totalCount)" }
}
Test "ct" "GET /contents/{id}" {
    $list = Invoke-RestMethod "$base/contents`?pageSize=1"
    $r = Invoke-RestMethod "$base/contents/$($list.items[0].id)"
    if (-not $r.title) { throw "no title" }
}
Test "ct" "GET /contents/{id}/detail (3 langs)" {
    $list = Invoke-RestMethod "$base/contents`?pageSize=1"
    $d = Invoke-RestMethod "$base/contents/$($list.items[0].id)/detail" -Headers $h
    if (-not $d.titleUz -or -not $d.titleRu -or -not $d.titleEn) { throw "missing langs" }
}
Test "ct" "GET /contents/9999 -> 404" {
    try { Invoke-RestMethod "$base/contents/9999" | Out-Null; throw "expected 404" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 404) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
Test "ct" "POST validation: empty image -> 400" {
    $bad1 = ConvertTo-Json @{sectionId=1;titleUz='a';titleRu='a';titleEn='a';descriptionUz='a';descriptionRu='a';descriptionEn='a';imageUrl=''}
    try { Invoke-WebRequest "$base/contents" -Method POST -Body $bad1 -ContentType 'application/json' -Headers $h | Out-Null; throw "expected 400" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 400) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
Test "ct" "POST validation: empty TitleEn -> 400" {
    $bad2 = ConvertTo-Json @{sectionId=1;titleUz='a';titleRu='a';titleEn='';descriptionUz='a';descriptionRu='a';descriptionEn='a';imageUrl='/x.jpg'}
    try { Invoke-WebRequest "$base/contents" -Method POST -Body $bad2 -ContentType 'application/json' -Headers $h | Out-Null; throw "expected 400" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 400) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
Test "ct" "Content CRUD: POST + PUT + DELETE roundtrip" {
    $list = Invoke-RestMethod "$base/sections"
    $b = ConvertTo-Json @{sectionId=$list[0].id;titleUz='_TestCt';titleRu='_TestCt';titleEn='_TestCt';descriptionUz='d';descriptionRu='d';descriptionEn='d';imageUrl='/x.jpg';sortOrder=999;isActive=$true}
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($b)
    $req = [System.Net.HttpWebRequest]::Create("$base/contents")
    $req.Method = "POST"; $req.ContentType = "application/json; charset=utf-8"
    $req.Headers.Add("Authorization", "Bearer $tk"); $req.ContentLength = $bytes.Length
    $st = $req.GetRequestStream(); $st.Write($bytes, 0, $bytes.Length); $st.Close()
    $resp = $req.GetResponse(); $rd = New-Object System.IO.StreamReader($resp.GetResponseStream())
    $r = $rd.ReadToEnd() | ConvertFrom-Json
    $b2 = ConvertTo-Json @{sectionId=$list[0].id;titleUz='_Upd';titleRu='_Upd';titleEn='_Upd';descriptionUz='d2';descriptionRu='d2';descriptionEn='d2';imageUrl='/y.jpg';sortOrder=998;isActive=$false}
    $upd = Invoke-RestMethod "$base/contents/$($r.id)" -Method PUT -Body $b2 -ContentType 'application/json; charset=utf-8' -Headers $h
    if (-not $upd.updatedAt) { throw "no updatedAt" }
    Invoke-WebRequest "$base/contents/$($r.id)" -Method DELETE -Headers $h | Out-Null
}

Test "md" "GET /media -> 5 items" { $r = Invoke-RestMethod "$base/media" -Headers $h; if ($r.totalCount -ne 5) { throw "$($r.totalCount)" } }
Test "md" "GET /media`?pageSize=2 -> 2 items" {
    $r = Invoke-RestMethod "$base/media`?pageSize=2" -Headers $h
    if ($r.items.Count -ne 2) { throw "$($r.items.Count)" }
}
Test "md" "GET /media/{id}" {
    $list = Invoke-RestMethod "$base/media`?pageSize=1" -Headers $h
    $m = Invoke-RestMethod "$base/media/$($list.items[0].id)" -Headers $h
    if (-not $m.url) { throw "no url" }
}
Test "md" "GET /media/9999 -> 404" {
    try { Invoke-RestMethod "$base/media/9999" -Headers $h | Out-Null; throw "expected 404" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 404) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
Test "md" "POST upload empty -> 400" {
    $LF = "`r`n"; $boundary = [Guid]::NewGuid().ToString()
    $body = "--$boundary$LF" + 'Content-Disposition: form-data; name="file"; filename="x.txt"' + "$LF$LF$LF--$boundary--$LF"
    try { Invoke-WebRequest "$base/media/upload" -Method POST -ContentType "multipart/form-data; boundary=$boundary" -Body $body -Headers $h | Out-Null; throw "expected 400" }
    catch { if ($_.Exception.Response.StatusCode.value__ -ne 400) { throw "got $($_.Exception.Response.StatusCode.value__)" } }
}
Test "md" "POST upload + DELETE roundtrip" {
    $LF = "`r`n"; $boundary = [Guid]::NewGuid().ToString()
    $body = "--$boundary$LF" + 'Content-Disposition: form-data; name="file"; filename="audit.txt"' + "$LF" + 'Content-Type: text/plain' + "$LF$LF" + 'audit' + "$LF--$boundary--$LF"
    $r = Invoke-RestMethod "$base/media/upload" -Method POST -ContentType "multipart/form-data; boundary=$boundary" -Body $body -Headers $h
    Invoke-WebRequest "$base/media/$($r.id)" -Method DELETE -Headers $h | Out-Null
}
Test "md" "Public access uploaded image" {
    $list = Invoke-RestMethod "$base/media`?pageSize=1" -Headers $h
    $r = Invoke-WebRequest "http://localhost:5099$($list.items[0].url)"
    if ($r.StatusCode -ne 200) { throw "got $($r.StatusCode)" }
}
Test "edge" "page=0 normalizes to 1" { $r = Invoke-RestMethod "$base/contents`?page=0`&pageSize=5"; if ($r.page -ne 1) { throw "page=$($r.page)" } }
Test "edge" "pageSize=999 caps at 100" { $r = Invoke-RestMethod "$base/contents`?pageSize=999"; if ($r.pageSize -ne 100) { throw "pageSize=$($r.pageSize)" } }

Write-Output ""
Write-Output "================================================================"
Write-Output " PHASE 3 - ADMIN PANEL UI ELEMENTS (har bir tugma, modal, form)"
Write-Output "================================================================"

$html = (Invoke-WebRequest "http://localhost:5099/admin/").Content
$jsApi = (Invoke-WebRequest "http://localhost:5099/admin/js/api.js").Content
$jsAuth = (Invoke-WebRequest "http://localhost:5099/admin/js/auth.js").Content
$jsCt = (Invoke-WebRequest "http://localhost:5099/admin/js/contents.js").Content
$jsSec = (Invoke-WebRequest "http://localhost:5099/admin/js/sections.js").Content
$jsMd = (Invoke-WebRequest "http://localhost:5099/admin/js/media.js").Content

$uiIds = @('login-form','login-btn','forgot-password-btn','theme-btn','back-to-site-btn','user-menu','change-password-btn','logout-btn','sidebar-toggle','new-section-btn','new-content-btn','media-upload-input','media-dropzone','section-modal','section-form','section-save-btn','content-modal','content-form','content-save-btn','pick-from-media-btn','content-quick-upload','content-image-remove','lang-fill-status','media-picker-modal','confirm-modal','change-password-modal','reset-password-modal')
foreach ($id in $uiIds) {
    Test "ui" "Element #$id present" { if ($html -notmatch [regex]::Escape("id=`"$id`"")) { throw "missing" } }
}
Test "ui" "Sidebar tab data-tab='sections'" { if ($html -notmatch 'data-tab="sections"') { throw "missing" } }
Test "ui" "Sidebar tab data-tab='contents'" { if ($html -notmatch 'data-tab="contents"') { throw "missing" } }
Test "ui" "Sidebar tab data-tab='media'" { if ($html -notmatch 'data-tab="media"') { throw "missing" } }
Test "ui" "Section dropdown name='sectionId'" { if ($html -notmatch 'name="sectionId"') { throw "missing" } }
Test "ui" "Image picker BIG cards (image-picker-card)" { if ($html -notmatch 'image-picker-card') { throw "missing" } }
Test "ui" "3 lang tabs (Uz/Ru/En)" {
    if ($html -notmatch 'data-lang="Uz"' -or $html -notmatch 'data-lang="Ru"' -or $html -notmatch 'data-lang="En"') { throw "missing" }
}
Test "ui" "Lang tab dots (lang-tab-dot)" { if ($html -notmatch 'lang-tab-dot') { throw "missing" } }
Test "ui" "Help banners (>=3)" {
    $count = ([regex]::Matches($html, "Bu yerda nima qilasiz")).Count
    if ($count -lt 3) { throw "only $count" }
}

Write-Output ""
Write-Output "================================================================"
Write-Output " PHASE 4 - JS METHODS"
Write-Output "================================================================"

$apiMethods = @('login','changePassword','resetPassword','getPublicConfig','listSections','createSection','updateSection','deleteSection','getSectionDetail','listContents','createContent','updateContent','deleteContent','getContentDetail','listMedia','uploadMedia','deleteMedia')
foreach ($m in $apiMethods) {
    $methodName = $m
    Test "js" "api.$methodName method present" { if ($jsApi -notmatch $methodName) { throw "missing" } }
}
Test "js" "auth.bindChangePassword present" { if ($jsAuth -notmatch 'bindChangePassword') { throw "missing" } }
Test "js" "auth.bindResetPassword present" { if ($jsAuth -notmatch 'bindResetPassword') { throw "missing" } }
Test "js" "auth.bindBackToSite present" { if ($jsAuth -notmatch 'bindBackToSite') { throw "missing" } }
Test "js" "Sections.openModal present" { if ($jsSec -notmatch 'openModal') { throw "missing" } }
Test "js" "Sections.deleteRow present" { if ($jsSec -notmatch 'deleteRow') { throw "missing" } }
Test "js" "Contents.save multilang validation" { if ($jsCt -notmatch 'missing\.length') { throw "missing" } }
Test "js" "Contents.openMediaPicker" { if ($jsCt -notmatch 'openMediaPicker') { throw "missing" } }
Test "js" "Contents.quickUpload" { if ($jsCt -notmatch 'quickUpload') { throw "missing" } }
Test "js" "Contents.refreshSectionDropdown" { if ($jsCt -notmatch 'refreshSectionDropdown') { throw "missing" } }
Test "js" "Contents.updateLangFillStatus" { if ($jsCt -notmatch 'updateLangFillStatus') { throw "missing" } }
Test "js" "Media.handleFiles" { if ($jsMd -notmatch 'handleFiles') { throw "missing" } }
Test "js" "Media drag-drop bindings" { if ($jsMd -notmatch 'dragenter') { throw "missing" } }

Write-Output ""
Write-Output "================================================================"
Write-Output " PHASE 5 - FRONTEND UI"
Write-Output "================================================================"

$fhtml = (Invoke-WebRequest "http://localhost:8080/").Content
$fapi = (Invoke-WebRequest "http://localhost:8080/js/api.js").Content
$fapp = (Invoke-WebRequest "http://localhost:8080/js/app.js").Content

$frontIds = @('home','sections','contact','theme-btn','lang-menu','admin-link','mobile-menu','back-to-top')
foreach ($id in $frontIds) {
    Test "front" "Frontend element #$id" { if ($fhtml -notmatch [regex]::Escape("id=`"$id`"")) { throw "missing" } }
}
Test "front" "VetSafe brand" { if ($fhtml -notmatch 'VetSafe') { throw "missing" } }
Test "front" "Dark mode config (class)" { if ($fhtml -notmatch "darkMode") { throw "missing" } }
Test "front" "api.getSectionsWithContents" { if ($fapi -notmatch 'getSectionsWithContents') { throw "missing" } }
Test "front" "api.resolveMediaUrl" { if ($fapi -notmatch 'resolveMediaUrl') { throw "missing" } }
Test "front" "App.rebuildNav" { if ($fapp -notmatch 'rebuildNav') { throw "missing" } }
Test "front" "App.renderSectionGroup" { if ($fapp -notmatch 'renderSectionGroup') { throw "missing" } }
Test "front" "App.renderDescription" { if ($fapp -notmatch 'renderDescription') { throw "missing" } }
Test "front" "App.bindThemeToggle" { if ($fapp -notmatch 'bindThemeToggle') { throw "missing" } }
Test "front" "Live: backend gives 4 sections, 15 items" {
    $r = Invoke-RestMethod "$base/sections/with-contents`?lang=uz"
    if ($r.Count -ne 4) { throw "$($r.Count)" }
    $tot = ($r | ForEach-Object { $_.items.Count } | Measure-Object -Sum).Sum
    if ($tot -ne 15) { throw "items=$tot" }
}

Write-Output ""
Write-Output "================================================================"
Write-Output " PHASE 6 - CROSS-CUTTING (CORS, cache, security)"
Write-Output "================================================================"

Test "x" "CORS Allow-Origin present" {
    $r = Invoke-WebRequest "$base/sections" -Headers @{'Origin'='http://localhost:8080'}
    if (-not $r.Headers['Access-Control-Allow-Origin']) { throw "missing" }
}
Test "x" "Admin static no-cache" {
    $r = Invoke-WebRequest "http://localhost:5099/admin/js/api.js" -Method HEAD
    if (-not ($r.Headers['Cache-Control'] -like '*no-cache*')) { throw "wrong" }
}
Test "x" "Frontend HTML has no-cache meta" { if ($fhtml -notmatch 'http-equiv="Cache-Control"') { throw "missing" } }

$cfg = Get-Content "C:\Users\AIZENN\Desktop\veterenarya.backend\src\VeterinaryBackend.API\appsettings.json" | ConvertFrom-Json
Test "x" "BCrypt cost factor 11" { if ($cfg.Admin.PasswordHash.Substring(4, 2) -ne '11') { throw "wrong" } }
Test "x" "JWT secret >= 256 bits" {
    $len = [Convert]::FromBase64String($cfg.Jwt.SecretKey).Length * 8
    if ($len -lt 256) { throw "$len bits" }
}
Test "x" "Recovery key >= 128 bits" {
    $len = [Convert]::FromBase64String($cfg.Admin.RecoveryKey).Length * 8
    if ($len -lt 128) { throw "$len bits" }
}

Write-Output ""
Write-Output "================================================================"
Write-Output " PHASE 7 - PER-ROW DATA INTEGRITY (15 yozuv)"
Write-Output "================================================================"

$secs = Invoke-RestMethod "$base/sections/with-contents`?lang=uz"
$rowFail = 0; $imgFail = 0
foreach ($sec in $secs) {
    foreach ($it in $sec.items) {
        $detail = Invoke-RestMethod "$base/contents/$($it.id)/detail" -Headers $h
        $issues = @()
        if (-not $detail.titleUz) { $issues += 'no UZ title' }
        if (-not $detail.titleRu) { $issues += 'no RU title' }
        if (-not $detail.titleEn) { $issues += 'no EN title' }
        if (-not $detail.descriptionUz) { $issues += 'no UZ desc' }
        if (-not $detail.descriptionRu) { $issues += 'no RU desc' }
        if (-not $detail.descriptionEn) { $issues += 'no EN desc' }
        if (-not $detail.imageUrl) { $issues += 'no image' }
        if (-not $detail.sectionId) { $issues += 'no sectionId' }
        if ($issues.Count -gt 0) { $rowFail++ }

        $url = $it.imageUrl
        if ($url.StartsWith('/')) { $url = "http://localhost:5099$url" }
        try {
            $r = Invoke-WebRequest -Uri $url -Method HEAD -TimeoutSec 5 -ErrorAction Stop
            if ($r.StatusCode -ne 200) { $imgFail++ }
        } catch { $imgFail++ }
    }
}
Test "row" "All 15 contents have all required fields" { if ($rowFail -gt 0) { throw "$rowFail rows" } }
Test "row" "All 15 image URLs reachable (200 OK)" { if ($imgFail -gt 0) { throw "$imgFail unreachable" } }

Write-Output ""
Write-Output "================================================================"
Write-Output ""
$total = $ok + $fail
$pct = if ($total -gt 0) { [Math]::Round(($ok/$total)*100, 1) } else { 0 }
Write-Output (" TOTAL:  PASS {0}   FAIL {1}   ({2}%)" -f $ok, $fail, $pct)
if ($fail -gt 0) {
    Write-Output ""
    Write-Output " FAILED:"
    $failedTests | ForEach-Object { Write-Output ("   - $_") }
}
Write-Output ""
Write-Output "================================================================"
Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
