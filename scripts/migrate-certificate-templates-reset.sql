SET FOREIGN_KEY_CHECKS = 0;
UPDATE certificates SET TemplateVersionId = NULL WHERE TemplateVersionId IS NOT NULL;
DELETE FROM certificate_process_assignments;
DELETE FROM certificate_elements;
DELETE FROM certificate_template_versions;
DELETE FROM certificate_templates;
SET FOREIGN_KEY_CHECKS = 1;
