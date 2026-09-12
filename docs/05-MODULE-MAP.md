# AgriCheck V3 — Module Map (V2 → V3)

Maps legacy Symfony features to V3 modules. Use V2 as **reference only** — V3 reimplements with cleaner API + React UI.

**Legend:** 🔴 Must (MVP+) | 🟡 Should | 🟢 Could (defer)

---

## Platform

| V2 Location | Feature | V3 Module | Phase | Priority |
|-------------|---------|-----------|-------|----------|
| `AuthController`, `WebAuthController` | Login, register, logout | Auth | P1 | 🔴 |
| `PasswordChangeController` | Force password change | Auth | P1 | 🔴 |
| `EmailVerificationController` | Email verify / resend | Auth | P1 | 🔴 |
| `UserController` | User CRUD | Users | P1 | 🔴 |
| `RoleController` | Role management | Users | P1 | 🔴 |
| `NotificationController` | In-app notifications | Notifications | P2 | 🟡 |
| `AdminSettingsController` | System settings | Settings | P4 | 🟡 |
| `AuditController` | Audit logs | Audit | P4 | 🟡 |

---

## Client Portal

| V2 Location | Feature | V3 Module | Phase | Priority |
|-------------|---------|-----------|-------|----------|
| `DashboardController` | Client dashboard | Client/Dashboard | P2 | 🔴 |
| `EntryController`, `WebEntryController` | My entries, submit, view | Entry | P2 | 🔴 |
| `AccreditationController` | Accreditation status + submissions | Accreditation | P2 | 🔴 |
| `CertificateViewController` | Certificate list/view | Certificate | P2 | 🔴 |
| `CertificateVerificationController` | Public verify | Certificate | P2 | 🔴 |
| `WarehouseBookingPageController` | Warehouse bookings UI | Warehouse | P2 | 🔴 |
| `ClientPaymentController` | Client pay links | Billing | P2 | 🔴 |
| `ClientPaymentHistoryController` | Payment history | Billing | P2 | 🟡 |
| `ApplicantComplianceController` | Compliance view | Entry/Compliance | P3 | 🟡 |
| `ApplicantPaymentController` | Applicant payments | Billing | P2 | 🔴 |

---

## Agency Portal

| V2 Location | Feature | V3 Module | Phase | Priority |
|-------------|---------|-----------|-------|----------|
| `AgencyDashboardController` | Agency home | Agency/Dashboard | P3 | 🔴 |
| `EvaluatorController` | Evaluator workflow | Evaluation | P3 | 🔴 |
| `EvaluationDashboardController` | Evaluation dashboard | Evaluation | P3 | 🔴 |
| `Agency\DocumentEvaluationController` | Document review | Evaluation | P3 | 🔴 |
| `Agency\EntryEvaluationController` | Entry evaluation | Evaluation | P3 | 🔴 |
| `ComplianceController` | Compliance checks | Evaluation | P3 | 🔴 |
| `Agency\BillingController` | Agency billing | Billing | P3 | 🔴 |
| `AccountantController` | Accountant views | Billing | P3 | 🟡 |
| `SecretaryController` | Secretary portal | Agency/Secretary | P3 | 🔴 |
| `AccreditationOfficerController` | Accreditation review | Accreditation | P3 | 🔴 |
| `SubmissionReviewController` | Submission review | Accreditation | P3 | 🔴 |
| `InspectorController` | Inspector portal | Inspection | P3 | 🔴 |
| `InspectionController` | Inspection management | Inspection | P3 | 🔴 |
| `Agency\InspectionPhotoController` | Photo evaluation | Inspection | P3 | 🟡 |

---

## Admin

| V2 Location | Feature | V3 Module | Phase | Priority |
|-------------|---------|-----------|-------|----------|
| `AdminController` | Admin dashboard | Admin | P4 | 🔴 |
| `CommodityController` | Commodity CRUD | Reference Data | P4 | 🔴 |
| `CommodityCategoryController` | Categories | Reference Data | P4 | 🔴 |
| `PaymentMethodConfigurationController` | Payment config | Billing/Admin | P4 | 🔴 |
| `ProcessingFeeConfigurationController` | Fee config | Billing/Admin | P4 | 🔴 |
| `ClientPaymentAdminController` | Client payment admin | Billing/Admin | P4 | 🟡 |
| `FormBuilderController` | Dynamic forms | Form Builder | P4 | 🟡 |
| `CertificateBuilderController` | Cert templates | Certificate | P4 | 🔴 |
| `CertificateManagementController` | Cert management | Certificate | P4 | 🔴 |
| `AdminCannedResponseController` | Canned responses | Admin | P4 | 🟢 |
| `TermsManagementController` | Terms of agreement | Admin | P4 | 🟡 |
| `WarehouseFacilityController` | Warehouse facilities | Warehouse/Admin | P4 | 🟡 |
| `EntryFileAuditController` | File audit | Audit | P4 | 🟢 |
| `PaymentReportController` | Payment reports | Reports | P4 | 🟡 |
| `AnalyticsController` | Analytics | Reports | P4 | 🟢 |

---

## MAV

| V2 Location | Feature | V3 Module | Phase | Priority |
|-------------|---------|-----------|-------|----------|
| `MavApplicationController` | MAV applications | MAV | P5 | 🔴 |
| `MavLicenseController` | Licenses | MAV | P5 | 🔴 |
| `MavMicController` | MIC management | MAV | P5 | 🔴 |
| `MavDashboardController` | Importer dashboard | MAV | P5 | 🔴 |
| `MavApplicationAdminController` | Admin applications | MAV/Admin | P5 | 🔴 |
| `MavLicenseAdminController` | Admin licenses | MAV/Admin | P5 | 🔴 |
| `MavMicAdminController` | Admin MIC | MAV/Admin | P5 | 🔴 |
| `MavReportController` | Reports | MAV/Reports | P5 | 🔴 |
| `MavComplianceController` | Compliance | MAV | P5 | 🔴 |
| `MavAuditController` | MAV audit | MAV/Audit | P5 | 🟡 |
| `ApplicationPeriodController` | Application periods | MAV/Admin | P5 | 🔴 |
| `YearTransitionController` | Year rollover | MAV/Admin | P5 | 🟡 |
| `MavNotificationPreferenceController` | Notification prefs | MAV | P5 | 🟢 |

---

## Warehouse & Logistics

| V2 Location | Feature | V3 Module | Phase | Priority |
|-------------|---------|-----------|-------|----------|
| `WarehouseBookingController` | Booking API | Warehouse | P2 | 🔴 |
| `WebWarehouseController` | Warehouse staff UI | Warehouse/Ops | P6 | 🟡 |
| `WarehouseController` | Inventory, releases | Warehouse/Ops | P6 | 🟡 |
| `ContainerController` | Container tracking | Container | P6 | 🟡 |
| `WebContainerController` | Container UI | Container | P6 | 🟡 |
| `DriverController` | Driver portal | Driver | P6 | 🟡 |
| `OperatorController` | Operator portal | Driver | P6 | 🟢 |

---

## Mobile API

| V2 Location | Feature | V3 Module | Phase | Priority |
|-------------|---------|-----------|-------|----------|
| `MobileAuthController` | Mobile login | Mobile/Auth | P6 | 🔴 |
| `MobileEntryController` | Mobile entries | Mobile/Entry | P6 | 🟡 |
| `MobileContainerController` | Mobile containers | Mobile/Container | P6 | 🟡 |
| `MobileBookingController` | Mobile bookings | Mobile/Warehouse | P6 | 🟢 |
| `MobileDeliveryController` | Delivery tracking | Mobile/Delivery | P6 | 🟢 |
| `MobileSyncController` | Offline sync | Mobile/Sync | P6 | 🟡 |
| `MobileProfileController` | Mobile profile | Mobile/Profile | P6 | 🟡 |
| `MobileHealthController` | Health check | Mobile | P6 | 🔴 |

---

## API (existing JSON endpoints)

| V2 Location | Feature | V3 Equivalent | Phase |
|-------------|---------|---------------|-------|
| `Api\AccreditationSubmissionController` | Accreditation API | `/api/v1/accreditation` | P2 |
| `Api\FormTemplateController` | Form templates | `/api/v1/forms` | P4 |
| `Api\PaymentWebhookController` | Webhooks | `/api/v1/webhooks` | P2 |
| `Api\WarehouseBookingApiController` | Bookings API | `/api/v1/warehouse/bookings` | P2 |
| `Api\CommodityApiController` | Commodities | `/api/v1/commodities` | P2 |
| `Api\MavDetectionController` | MAV detection | `/api/v1/mav` | P5 |
| `Api\CertificateElementController` | Cert elements | `/api/v1/certificates/templates` | P4 |

---

## V2 Templates → V3 React Pages

| V2 Twig Template | V3 React Route (planned) |
|------------------|--------------------------|
| `templates/client/*` + `base_client.html.twig` | `/client/*` |
| `templates/entry/*` | `/client/entries/*` |
| `templates/accreditation/*` | `/client/accreditation/*` |
| `templates/certificate/*` | `/client/certificates/*`, `/verify/:code` |
| `templates/warehouse/booking/*` | `/client/warehouse/bookings` |
| `templates/inspector/*` | `/inspector/*` |
| `templates/agency/*` | `/agency/*` |
| `templates/admin/*` | `/admin/*` |
| `templates/mav/*` | `/mav/*` |
| `templates/driver/*` | `/driver/*` |
| `templates/auth/*` | `/login`, `/register`, `/reset-password` |

UI reference: V2 `public/css/agricheck-global.css` and recently redesigned client/inspector templates.

---

## Out of Scope (V3 initial)

| Feature | Reason |
|---------|--------|
| Direct V2 DB connection | Fresh DB decision |
| Symfony Twig in V3 | Full React SPA |
| PHP runtime in V3 | .NET only |
| Face recognition (TBD) | Evaluate in P6 — may defer |
| TestQRController | Dev-only; replace with QRCoder unit tests |

---

## Module Dependency Order

```
Auth → Reference Data → Entry → Accreditation
                      ↘ Warehouse (bookings)
Entry → Evaluation → Inspection
Entry → Billing → Certificates
Reference Data → MAV
Warehouse → Warehouse Ops → Driver/Mobile
```

Build in this order to avoid blocking dependencies.
