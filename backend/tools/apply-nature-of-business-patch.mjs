import { execSync } from 'node:child_process'
import { readFileSync } from 'node:fs'

const mysql = 'C:/xampp/mysql/bin/mysql.exe'
const now = new Date().toISOString().replace('T', ' ').replace('Z', '')

function runSql(sql) {
  execSync(`"${mysql}" -u agricheck_v3 -pagricheck_v3_dev agricheck_v3 -e "${sql.replace(/"/g, '\\"')}"`, {
    stdio: 'inherit',
  })
}

const schemaJson = readFileSync('c:/xampp/htdocs/agricheck-v3/tmp_form_schema_v28.json', 'utf8')
const escapedSchema = schemaJson.replace(/\\/g, '\\\\').replace(/'/g, "''")

runSql(`
INSERT INTO form_template_versions (TemplateId, VersionNumber, SchemaJson, IsPublished, CreatedAt, UpdatedAt)
SELECT 3, 28, '${escapedSchema}', 1, '${now}', '${now}'
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM form_template_versions WHERE TemplateId=3 AND VersionNumber=28);
`)

runSql(`
UPDATE accreditation_submissions
SET FormDataJson = JSON_SET(CAST(FormDataJson AS JSON), '$.nature_of_business', 'Importation of agricultural products'),
    UpdatedAt = '${now}'
WHERE Uuid = 'c6e4de3b-edbb-4f97-9bcb-c66b090c6799'
  AND JSON_EXTRACT(CAST(FormDataJson AS JSON), '$.nature_of_business') IS NULL;
`)

const config =
  '{"content":"{{ company.nature_of_business }}","x":110,"y":199,"width":67,"height":10,"fontFamily":"helvetica","fontSize":11,"fontWeight":"normal","fontStyle":"normal","textAlign":"left","textColor":"#000000","rotation":0,"backgroundColor":"transparent","borderWidth":0,"borderColor":"#000000","borderRadius":0,"imagePath":null,"zIndex":33,"displayOrder":34,"qrLogoPath":null,"qrLogoSize":20,"qrForegroundColor":"#000000","qrBackgroundColor":"#FFFFFF","qrStyle":"square"}'

runSql(`
INSERT INTO certificate_elements (VersionId, ElementType, Label, ConfigJson, SortOrder, CreatedAt, UpdatedAt)
SELECT 31, 'TEXT', '{{ company.nature_of_business }}', '${config}', 34, '${now}', '${now}'
FROM DUAL
WHERE NOT EXISTS (
  SELECT 1 FROM certificate_elements WHERE VersionId=31 AND Label='{{ company.nature_of_business }}'
);
`)

console.log('Database patch complete.')
