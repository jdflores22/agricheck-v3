import { execSync } from 'node:child_process'
import { randomUUID } from 'node:crypto'
import { writeFileSync } from 'node:fs'

const mysql = 'C:/xampp/mysql/bin/mysql.exe'
const query = "SELECT SchemaJson FROM form_template_versions WHERE TemplateId=3 AND VersionNumber=27 LIMIT 1"
const raw = execSync(
  `"${mysql}" -u agricheck_v3 -pagricheck_v3_dev agricheck_v3 -N -B -e "${query}"`,
  { encoding: 'utf8' },
).trim()

const fields = JSON.parse(raw.replace(/\\n/g, '\n'))
const newField = {
  id: randomUUID(),
  name: 'nature_of_business',
  label: 'Nature of Business',
  type: 'text',
  required: true,
  placeholder: 'e.g. Importation of agricultural products',
  helpText: 'Primary business activity or line of business',
  columnWidth: 12,
  displayOrder: 9,
}

const updated = fields
  .filter((field) => field.name !== 'nature_of_business')
  .map((field) => {
    if ((field.displayOrder ?? 0) >= 9) {
      return { ...field, displayOrder: (field.displayOrder ?? 0) + 1 }
    }
    return field
  })
  .sort((a, b) => (a.displayOrder ?? 0) - (b.displayOrder ?? 0))

const final = []
let inserted = false
for (const field of updated) {
  if (!inserted && (field.displayOrder ?? 0) > 8) {
    final.push(newField)
    inserted = true
  }
  final.push(field)
}
if (!inserted) {
  final.push(newField)
}

writeFileSync('c:/xampp/htdocs/agricheck-v3/tmp_form_schema_v28.json', JSON.stringify(final))
console.log(`Prepared ${final.length} fields`)
