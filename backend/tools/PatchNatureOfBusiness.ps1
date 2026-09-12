$mysql = "C:\xampp\mysql\bin\mysql.exe"
$args = @("-u", "agricheck_v3", "-pagricheck_v3_dev", "agricheck_v3", "-N", "-B")
$now = (Get-Date).ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ffffff")

# 1) Add nature_of_business to accreditation form (new version 28)
$schemaJson = & $mysql @args -e "SELECT SchemaJson FROM form_template_versions WHERE TemplateId=3 AND VersionNumber=27 LIMIT 1;"
$fields = $schemaJson | ConvertFrom-Json
$newField = [ordered]@{
    id = [guid]::NewGuid().ToString()
    name = "nature_of_business"
    label = "Nature of Business"
    type = "text"
    required = $true
    placeholder = "e.g. Importation of agricultural products"
    helpText = "Primary business activity or line of business"
    columnWidth = 12
    displayOrder = 9
}
foreach ($field in $fields) {
    if (($field.displayOrder -ge 9) -and ($field.name -ne "nature_of_business")) {
        $field.displayOrder = [int]$field.displayOrder + 1
    }
}
$updatedFields = @()
$inserted = $false
foreach ($field in ($fields | Sort-Object { [int]$field.displayOrder })) {
    if (-not $inserted -and [int]$field.displayOrder -gt 8) {
        $updatedFields += [pscustomobject]$newField
        $inserted = $true
    }
    if ($field.name -ne "nature_of_business") {
        $updatedFields += $field
    }
}
if (-not $inserted) {
    $updatedFields += [pscustomobject]$newField
}
$newSchemaJson = ($updatedFields | ConvertTo-Json -Depth 10 -Compress)
$escapedSchema = $newSchemaJson.Replace("\", "\\").Replace("'", "''")
$insertFormSql = @"
INSERT INTO form_template_versions (TemplateId, VersionNumber, SchemaJson, IsPublished, CreatedAt, UpdatedAt)
VALUES (3, 28, '$escapedSchema', 1, '$now', '$now');
"@
& $mysql @("-u", "agricheck_v3", "-pagricheck_v3_dev", "agricheck_v3") -e $insertFormSql
Write-Host "Form template version 28 created."

# 2) Backfill existing approved submission test data
$backfillSql = @"
UPDATE accreditation_submissions
SET FormDataJson = JSON_SET(CAST(FormDataJson AS JSON), '$.nature_of_business', 'Importation of agricultural products'),
    UpdatedAt = '$now'
WHERE Uuid = 'c6e4de3b-edbb-4f97-9bcb-c66b090c6799'
  AND JSON_EXTRACT(CAST(FormDataJson AS JSON), '$.nature_of_business') IS NULL;
"@
& $mysql @("-u", "agricheck_v3", "-pagricheck_v3_dev", "agricheck_v3") -e $backfillSql
Write-Host "Submission form data backfilled."

# 3) Add certificate template variable element if missing
$exists = & $mysql @args -e "SELECT COUNT(*) FROM certificate_elements WHERE VersionId=31 AND Label='{{ company.nature_of_business }}';"
if ([int]$exists -eq 0) {
    $config = '{"content":"{{ company.nature_of_business }}","x":110,"y":199,"width":67,"height":10,"fontFamily":"helvetica","fontSize":11,"fontWeight":"normal","fontStyle":"normal","textAlign":"left","textColor":"#000000","rotation":0,"backgroundColor":"transparent","borderWidth":0,"borderColor":"#000000","borderRadius":0,"imagePath":null,"zIndex":33,"displayOrder":34,"qrLogoPath":null,"qrLogoSize":20,"qrForegroundColor":"#000000","qrBackgroundColor":"#FFFFFF","qrStyle":"square"}'
    $insertCertSql = @"
INSERT INTO certificate_elements (VersionId, ElementType, Label, ConfigJson, SortOrder, CreatedAt, UpdatedAt)
VALUES (31, 'TEXT', '{{ company.nature_of_business }}', '$config', 34, '$now', '$now');
"@
    & $mysql @("-u", "agricheck_v3", "-pagricheck_v3_dev", "agricheck_v3") -e $insertCertSql
    Write-Host "Certificate element added."
} else {
    Write-Host "Certificate element already present."
}

Write-Host "Done."
