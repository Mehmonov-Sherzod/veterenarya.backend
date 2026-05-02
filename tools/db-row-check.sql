\echo '════════════════════════════════════════════════════════════════════════'
\echo ' SECTIONS — barcha qatorlar, barcha maydonlar'
\echo '════════════════════════════════════════════════════════════════════════'
\x on
SELECT
  "Id",
  "Slug",
  "TitleUz",
  "TitleRu",
  "TitleEn",
  "SortOrder",
  "IsActive",
  "CreatedAt",
  "UpdatedAt",
  length("TitleUz") AS uz_len,
  length("TitleRu") AS ru_len,
  length("TitleEn") AS en_len
FROM sections
ORDER BY "SortOrder";

\echo ''
\echo '════════════════════════════════════════════════════════════════════════'
\echo ' CONTENTS — barcha qatorlar, barcha maydonlar (uzunliklar bilan)'
\echo '════════════════════════════════════════════════════════════════════════'
SELECT
  "Id",
  "SectionId",
  "TitleUz",
  "TitleRu",
  "TitleEn",
  left("DescriptionUz", 80) || '...' AS desc_uz_preview,
  left("DescriptionRu", 80) || '...' AS desc_ru_preview,
  left("DescriptionEn", 80) || '...' AS desc_en_preview,
  "ImageUrl",
  "SortOrder",
  "IsActive",
  "CreatedAt",
  length("TitleUz") AS title_uz_len,
  length("TitleRu") AS title_ru_len,
  length("TitleEn") AS title_en_len,
  length("DescriptionUz") AS desc_uz_len,
  length("DescriptionRu") AS desc_ru_len,
  length("DescriptionEn") AS desc_en_len,
  length("ImageUrl") AS img_url_len
FROM contents
ORDER BY "SectionId", "SortOrder";

\echo ''
\echo '════════════════════════════════════════════════════════════════════════'
\echo ' MEDIA FILES — barcha qatorlar'
\echo '════════════════════════════════════════════════════════════════════════'
SELECT
  "Id",
  "OriginalFileName",
  "StoredFileName",
  "RelativePath",
  "Url",
  "ContentType",
  "Extension",
  "SizeBytes",
  "CreatedAt"
FROM media_files
ORDER BY "Id";
\x off

\echo ''
\echo '════════════════════════════════════════════════════════════════════════'
\echo ' INTEGRITY SUMMARY'
\echo '════════════════════════════════════════════════════════════════════════'
SELECT
  'sections'                              AS table_name,
  count(*)                                AS total_rows,
  count(*) FILTER (WHERE "TitleUz" IS NULL OR "TitleUz" = '')          AS empty_title_uz,
  count(*) FILTER (WHERE "TitleRu" IS NULL OR "TitleRu" = '')          AS empty_title_ru,
  count(*) FILTER (WHERE "TitleEn" IS NULL OR "TitleEn" = '')          AS empty_title_en,
  count(*) FILTER (WHERE "Slug" IS NULL OR "Slug" = '')                AS empty_slug,
  count(DISTINCT "Slug")                                                AS distinct_slugs
FROM sections
UNION ALL
SELECT
  'contents',
  count(*),
  count(*) FILTER (WHERE "TitleUz" IS NULL OR "TitleUz" = ''),
  count(*) FILTER (WHERE "TitleRu" IS NULL OR "TitleRu" = ''),
  count(*) FILTER (WHERE "TitleEn" IS NULL OR "TitleEn" = ''),
  count(*) FILTER (WHERE "ImageUrl" IS NULL OR "ImageUrl" = ''),
  count(DISTINCT "Id")
FROM contents
UNION ALL
SELECT
  'media_files',
  count(*),
  count(*) FILTER (WHERE "Url" IS NULL OR "Url" = ''),
  count(*) FILTER (WHERE "RelativePath" IS NULL OR "RelativePath" = ''),
  count(*) FILTER (WHERE "ContentType" IS NULL OR "ContentType" = ''),
  count(*) FILTER (WHERE "OriginalFileName" IS NULL OR "OriginalFileName" = ''),
  count(DISTINCT "Id")
FROM media_files;

\echo ''
\echo '════════════════════════════════════════════════════════════════════════'
\echo ' CONTENTS DESCRIPTION FIELDS — to'liq saqlanganmi (mass)?'
\echo '════════════════════════════════════════════════════════════════════════'
SELECT
  count(*)                                  AS total,
  count(*) FILTER (WHERE "DescriptionUz" IS NULL OR "DescriptionUz" = '') AS empty_uz,
  count(*) FILTER (WHERE "DescriptionRu" IS NULL OR "DescriptionRu" = '') AS empty_ru,
  count(*) FILTER (WHERE "DescriptionEn" IS NULL OR "DescriptionEn" = '') AS empty_en,
  min(length("DescriptionUz"))              AS min_uz_len,
  max(length("DescriptionUz"))              AS max_uz_len,
  avg(length("DescriptionUz"))::int         AS avg_uz_len,
  sum(length("DescriptionUz") + length("DescriptionRu") + length("DescriptionEn")) AS total_desc_bytes
FROM contents;

\echo ''
\echo '════════════════════════════════════════════════════════════════════════'
\echo ' SECTION → CONTENT DISTRIBUTION'
\echo '════════════════════════════════════════════════════════════════════════'
SELECT
  s."Id",
  s."Slug",
  s."TitleUz",
  count(c."Id") AS content_count
FROM sections s
LEFT JOIN contents c ON c."SectionId" = s."Id"
GROUP BY s."Id", s."Slug", s."TitleUz"
ORDER BY s."SortOrder";

\echo ''
\echo '════════════════════════════════════════════════════════════════════════'
\echo ' UTF-8 CYRILLIC PRESERVATION CHECK'
\echo '════════════════════════════════════════════════════════════════════════'
SELECT
  "Id",
  "TitleRu",
  "TitleRu" ~ '[А-Яа-яЁё]' AS has_cyrillic,
  octet_length("TitleRu") AS bytes,
  length("TitleRu")       AS chars
FROM sections
ORDER BY "Id"
LIMIT 4;
