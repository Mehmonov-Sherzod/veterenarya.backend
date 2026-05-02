#!/bin/bash
# Professional comprehensive test - all CRUD, all guards, all edge cases.
BASE="http://localhost:5099"
PASS=0
FAIL=0
FAILED=()

t() {
  local name="$1"; local expected="$2"; local actual="$3"
  if [ "$expected" = "$actual" ]; then
    PASS=$((PASS+1))
  else
    FAIL=$((FAIL+1))
    FAILED+=("$name | expected=$expected got=$actual")
  fi
}

LOGIN=$(curl -s -X POST $BASE/api/v1/auth/login -H "Content-Type: application/json" -d '{"username":"admin","password":"Admin@123"}')
TOKEN=$(echo "$LOGIN" | python -c "import sys,json;print(json.load(sys.stdin).get('accessToken',''))")
[ -n "$TOKEN" ] && PASS=$((PASS+1)) || { FAIL=$((FAIL+1)); FAILED+=("LOGIN failed"); }

H="Authorization: Bearer $TOKEN"
JC="Content-Type: application/json; charset=utf-8"

# ===== AUTH =====
CODE=$(curl -s -o /dev/null -w "%{http_code}" -X POST $BASE/api/v1/auth/login -H "Content-Type: application/json" -d '{"username":"admin","password":"WRONG"}')
t "auth wrong password" "401" "$CODE"

CODE=$(curl -s -o /dev/null -w "%{http_code}" $BASE/api/v1/config/public)
t "config public 200" "200" "$CODE"

# ===== AUTH GUARDS =====
CODE=$(curl -s -o /dev/null -w "%{http_code}" -X POST $BASE/api/v1/sections -H "$JC" -d '{}')
t "no-auth POST sections" "401" "$CODE"

CODE=$(curl -s -o /dev/null -w "%{http_code}" -X DELETE $BASE/api/v1/sections/8)
t "no-auth DELETE section" "401" "$CODE"

CODE=$(curl -s -o /dev/null -w "%{http_code}" -X POST $BASE/api/v1/contents -H "$JC" -d '{}')
t "no-auth POST content" "401" "$CODE"

CODE=$(curl -s -o /dev/null -w "%{http_code}" -X DELETE $BASE/api/v1/media/1)
t "no-auth DELETE media" "401" "$CODE"

CODE=$(curl -s -o /dev/null -w "%{http_code}" $BASE/api/v1/media)
t "no-auth GET media (admin only)" "401" "$CODE"

# ===== PUBLIC =====
CODE=$(curl -s -o /dev/null -w "%{http_code}" "$BASE/api/v1/sections/with-contents?lang=uz")
t "public GET with-contents UZ" "200" "$CODE"

CODE=$(curl -s -o /dev/null -w "%{http_code}" "$BASE/api/v1/sections/with-contents?lang=ru")
t "public GET with-contents RU" "200" "$CODE"

CODE=$(curl -s -o /dev/null -w "%{http_code}" "$BASE/api/v1/sections/with-contents?lang=en")
t "public GET with-contents EN" "200" "$CODE"

# Verify multilingual differentiation
UZ=$(curl -s "$BASE/api/v1/sections/with-contents?lang=uz" | python -c "import sys,json;d=json.load(sys.stdin);print(d[0]['title'] if d else '')")
RU=$(curl -s "$BASE/api/v1/sections/with-contents?lang=ru" | python -c "import sys,json;d=json.load(sys.stdin);print(d[0]['title'] if d else '')")
[ "$UZ" != "$RU" ] && PASS=$((PASS+1)) || { FAIL=$((FAIL+1)); FAILED+=("UZ vs RU same: $UZ"); }

# ===== SECTIONS CRUD =====
RESP=$(curl -s -X POST $BASE/api/v1/sections -H "$H" -H "$JC" -d '{"slug":"test-prof","titleUz":"Test prof","titleRu":"Тест проф","titleEn":"Test prof","sortOrder":99,"isActive":true}')
NEW_SECTION=$(echo "$RESP" | python -c "import sys,json;print(json.load(sys.stdin).get('id',''))")
[ -n "$NEW_SECTION" ] && PASS=$((PASS+1)) || { FAIL=$((FAIL+1)); FAILED+=("create section: $RESP"); }

CODE=$(curl -s -o /dev/null -w "%{http_code}" -X PUT $BASE/api/v1/sections/$NEW_SECTION -H "$H" -H "$JC" -d "{\"slug\":\"test-prof\",\"titleUz\":\"Updated\",\"titleRu\":\"Обновлено\",\"titleEn\":\"Updated\",\"sortOrder\":99,\"isActive\":true}")
t "PUT section update" "200" "$CODE"

# Validation: empty body
CODE=$(curl -s -o /dev/null -w "%{http_code}" -X POST $BASE/api/v1/sections -H "$H" -H "$JC" -d '{}')
t "POST section empty body 400" "400" "$CODE"

# Validation: bad slug
CODE=$(curl -s -o /dev/null -w "%{http_code}" -X POST $BASE/api/v1/sections -H "$H" -H "$JC" -d '{"slug":"BAD SLUG","titleUz":"X","titleRu":"X","titleEn":"X","sortOrder":0,"isActive":true}')
t "POST section bad slug regex" "400" "$CODE"

# Slug duplicate auto-resolves
RESP=$(curl -s -X POST $BASE/api/v1/sections -H "$H" -H "$JC" -d '{"slug":"test-prof","titleUz":"Dup","titleRu":"Дуп","titleEn":"Dup","sortOrder":100,"isActive":true}')
DUP_SECTION=$(echo "$RESP" | python -c "import sys,json;d=json.load(sys.stdin);print(d.get('slug',''))")
[ "$DUP_SECTION" = "test-prof-2" ] && PASS=$((PASS+1)) || { FAIL=$((FAIL+1)); FAILED+=("slug auto-rename: $DUP_SECTION"); }
DUP_ID=$(curl -s "$BASE/api/v1/sections" -H "$H" | python -c "import sys,json;[print(s['id']) for s in json.load(sys.stdin) if s['slug']=='test-prof-2']")

# ===== MEDIA CRUD =====
python -c "from PIL import Image;Image.new('RGB',(50,50),'blue').save('C:/Users/AIZENN/Desktop/_t.jpg',quality=70)"
RESP=$(curl -s -X POST $BASE/api/v1/media/upload -H "$H" -F "file=@C:/Users/AIZENN/Desktop/_t.jpg")
NEW_MEDIA=$(echo "$RESP" | python -c "import sys,json;print(json.load(sys.stdin).get('id',''))")
NEW_URL=$(echo "$RESP" | python -c "import sys,json;print(json.load(sys.stdin).get('url',''))")
[ -n "$NEW_MEDIA" ] && PASS=$((PASS+1)) || { FAIL=$((FAIL+1)); FAILED+=("media upload: $RESP"); }

CODE=$(curl -s -o /dev/null -w "%{http_code}" "$BASE$NEW_URL")
t "static file public access" "200" "$CODE"

# .exe blocked
echo "x" > "C:/Users/AIZENN/Desktop/_e.exe"
CODE=$(curl -s -o /dev/null -w "%{http_code}" -X POST $BASE/api/v1/media/upload -H "$H" -F "file=@C:/Users/AIZENN/Desktop/_e.exe")
t "media .exe blocked" "400" "$CODE"
rm "C:/Users/AIZENN/Desktop/_e.exe"

# Empty extension blocked
echo "x" > "C:/Users/AIZENN/Desktop/_n"
CODE=$(curl -s -o /dev/null -w "%{http_code}" -X POST $BASE/api/v1/media/upload -H "$H" -F "file=@C:/Users/AIZENN/Desktop/_n")
t "media no extension blocked" "400" "$CODE"
rm "C:/Users/AIZENN/Desktop/_n"

# ===== CONTENTS CRUD =====
PAYLOAD=$(cat <<JSON
{"sectionId":$NEW_SECTION,"titleUz":"Test content","titleRu":"Тест контент","titleEn":"Test content","descriptionUz":"Tavsif","descriptionRu":"Описание","descriptionEn":"Description","imageUrl":"$NEW_URL","sortOrder":1,"isActive":true}
JSON
)
RESP=$(curl -s -X POST $BASE/api/v1/contents -H "$H" -H "$JC" -d "$PAYLOAD")
NEW_CONTENT=$(echo "$RESP" | python -c "import sys,json;print(json.load(sys.stdin).get('id',''))")
[ -n "$NEW_CONTENT" ] && PASS=$((PASS+1)) || { FAIL=$((FAIL+1)); FAILED+=("create content: $RESP"); }

# Required: sectionId
CODE=$(curl -s -o /dev/null -w "%{http_code}" -X POST $BASE/api/v1/contents -H "$H" -H "$JC" -d '{"titleUz":"X","titleRu":"X","titleEn":"X","descriptionUz":"X","descriptionRu":"X","descriptionEn":"X","imageUrl":"/uploads/x.jpg","sortOrder":0,"isActive":true}')
t "POST content no sectionId 400" "400" "$CODE"

# Bad sectionId
CODE=$(curl -s -o /dev/null -w "%{http_code}" -X POST $BASE/api/v1/contents -H "$H" -H "$JC" -d '{"sectionId":99999,"titleUz":"X","titleRu":"X","titleEn":"X","descriptionUz":"X","descriptionRu":"X","descriptionEn":"X","imageUrl":"/uploads/x.jpg","sortOrder":0,"isActive":true}')
t "POST content bad sectionId 404" "404" "$CODE"

# UPDATE keeps sectionId
UPD=$(cat <<JSON
{"sectionId":$NEW_SECTION,"titleUz":"Updated","titleRu":"Обновлено","titleEn":"Updated","descriptionUz":"upd","descriptionRu":"upd","descriptionEn":"upd","imageUrl":"$NEW_URL","sortOrder":2,"isActive":true}
JSON
)
RESP=$(curl -s -X PUT "$BASE/api/v1/contents/$NEW_CONTENT" -H "$H" -H "$JC" -d "$UPD")
KEPT_SECTION=$(echo "$RESP" | python -c "import sys,json;print(json.load(sys.stdin).get('sectionId',''))")
t "PUT content keeps sectionId" "$NEW_SECTION" "$KEPT_SECTION"

# UPDATE without sectionId now blocked
CODE=$(curl -s -o /dev/null -w "%{http_code}" -X PUT "$BASE/api/v1/contents/$NEW_CONTENT" -H "$H" -H "$JC" -d '{"titleUz":"X","titleRu":"X","titleEn":"X","descriptionUz":"X","descriptionRu":"X","descriptionEn":"X","imageUrl":"/uploads/x.jpg","sortOrder":0,"isActive":true}')
t "PUT content no sectionId 400" "400" "$CODE"

# Pagination
TOTAL=$(curl -s "$BASE/api/v1/contents?page=1&pageSize=3" -H "$H" | python -c "import sys,json;print(json.load(sys.stdin).get('totalCount',-1))")
[ "$TOTAL" -gt 0 ] && PASS=$((PASS+1)) || { FAIL=$((FAIL+1)); FAILED+=("contents pagination total=$TOTAL"); }

# Filter by sectionId
SEC_COUNT=$(curl -s "$BASE/api/v1/contents?sectionId=$NEW_SECTION" -H "$H" | python -c "import sys,json;print(json.load(sys.stdin).get('totalCount',-1))")
t "filter contents by sectionId" "1" "$SEC_COUNT"

# ===== CASCADE DELETE =====
CODE=$(curl -s -o /dev/null -w "%{http_code}" -X DELETE "$BASE/api/v1/sections/$NEW_SECTION" -H "$H")
t "DELETE section cascade" "204" "$CODE"

CODE=$(curl -s -o /dev/null -w "%{http_code}" "$BASE/api/v1/contents/$NEW_CONTENT/detail" -H "$H")
t "content gone after section cascade" "404" "$CODE"

# Cleanup duplicate section
[ -n "$DUP_ID" ] && curl -s -o /dev/null -X DELETE "$BASE/api/v1/sections/$DUP_ID" -H "$H"

# Cleanup media
CODE=$(curl -s -o /dev/null -w "%{http_code}" -X DELETE "$BASE/api/v1/media/$NEW_MEDIA" -H "$H")
t "DELETE media" "204" "$CODE"
rm -f "C:/Users/AIZENN/Desktop/_t.jpg"

# ===== EDGE CASES =====
CODE=$(curl -s -o /dev/null -w "%{http_code}" "$BASE/api/v1/sections/99999/detail" -H "$H")
t "GET non-existent section 404" "404" "$CODE"

CODE=$(curl -s -o /dev/null -w "%{http_code}" -X DELETE "$BASE/api/v1/sections/99999" -H "$H")
t "DELETE non-existent section 404" "404" "$CODE"

CODE=$(curl -s -o /dev/null -w "%{http_code}" "$BASE/api/v1/contents/99999/detail" -H "$H")
t "GET non-existent content 404" "404" "$CODE"

CODE=$(curl -s -o /dev/null -w "%{http_code}" "$BASE/api/v1/media/99999" -H "$H")
t "GET non-existent media 404" "404" "$CODE"

# ===== ADMIN STATIC =====
CODE=$(curl -s -o /dev/null -w "%{http_code}" $BASE/admin/)
t "admin SPA HTML" "200" "$CODE"

for f in api.js app.js auth.js sections.js contents.js media.js ui.js; do
  CODE=$(curl -s -o /dev/null -w "%{http_code}" "$BASE/admin/js/$f")
  t "admin static $f" "200" "$CODE"
done

CACHE=$(curl -s -I "$BASE/admin/" | grep -i "cache-control" | head -1)
echo "$CACHE" | grep -q "no-store" && PASS=$((PASS+1)) || { FAIL=$((FAIL+1)); FAILED+=("admin no-cache header"); }

# ===== FRONTEND =====
CODE=$(curl -s -o /dev/null -w "%{http_code}" "http://localhost:8080/index.html")
t "frontend HTML" "200" "$CODE"

# Frontend JS files
for f in api.js app.js config.js i18n.js; do
  CODE=$(curl -s -o /dev/null -w "%{http_code}" "http://localhost:8080/js/$f")
  t "frontend static $f" "200" "$CODE"
done

# ===== DB INTEGRITY =====
SECTIONS_DB=$(curl -s "$BASE/api/v1/sections" -H "$H" | python -c "import sys,json;print(len(json.load(sys.stdin)))")
[ "$SECTIONS_DB" -ge 5 ] && PASS=$((PASS+1)) || { FAIL=$((FAIL+1)); FAILED+=("sections in DB: $SECTIONS_DB"); }

ORPHAN=$(curl -s "$BASE/api/v1/contents?pageSize=200" -H "$H" | python -c "import sys,json;d=json.load(sys.stdin);print(sum(1 for c in d['items'] if not c.get('sectionId')))")
t "orphan contents count" "0" "$ORPHAN"

# ===== REPORT =====
TOTAL=$((PASS+FAIL))
echo ""
echo "============================================="
echo "  PASS=$PASS  FAIL=$FAIL  TOTAL=$TOTAL"
echo "============================================="
if [ $FAIL -gt 0 ]; then
  echo "FAILURES:"
  for f in "${FAILED[@]}"; do echo "  - $f"; done
fi
