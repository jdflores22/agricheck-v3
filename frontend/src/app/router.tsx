import { createBrowserRouter, Navigate, useParams } from 'react-router-dom'

import { GuestRoute, ProtectedRoute, RoleProtectedRoute } from './ProtectedRoute'

import { PublicLayout } from '../layouts/PublicLayout'
import { AuthLayout } from '../layouts/AuthLayout'
import { AppPortalLayout } from '../layouts/AppPortalLayout'

import { HomePage } from '../features/home/pages/HomePage'

import { LoginPage } from '../features/auth/pages/LoginPage'

import { RegisterPage } from '../features/auth/pages/RegisterPage'

import { ForgotPasswordPage } from '../features/auth/pages/ForgotPasswordPage'

import { ResetPasswordPage } from '../features/auth/pages/ResetPasswordPage'
import { ResendVerificationPage } from '../features/auth/pages/ResendVerificationPage'

import { ProfilePage } from '../features/auth/pages/ProfilePage'

import { ClientDashboardPage } from '../features/client/pages/ClientDashboardPage'

import { EntryAgenciesPage } from '../features/client/pages/EntryAgenciesPage'
import { EntriesListPage } from '../features/client/pages/EntriesListPage'

import { EntryFormPage } from '../features/client/pages/EntryFormPage'

import { EntryViewPage } from '../features/client/pages/EntryViewPage'
import { EntryInspectionUploadPage } from '../features/client/pages/EntryInspectionUploadPage'

import { AccreditationListPage } from '../features/client/pages/AccreditationListPage'
import { AccreditationDetailPage } from '../features/client/pages/AccreditationDetailPage'

import { CertificatesListPage } from '../features/client/pages/CertificatesListPage'
import { CertificateDetailPage } from '../features/client/pages/CertificateDetailPage'

import { WarehouseBookingsPage } from '../features/client/pages/WarehouseBookingsPage'
import { WarehouseBookingDetailPage } from '../features/client/pages/WarehouseBookingDetailPage'

import { BillsListPage } from '../features/client/pages/BillsListPage'
import { BillDetailPage } from '../features/client/pages/BillDetailPage'
import { BillPaymentLinkPage } from '../features/client/pages/BillPaymentLinkPage'
import { BillPaymentReturnPage } from '../features/client/pages/BillPaymentReturnPage'
import { PaymentHistoryPage } from '../features/client/pages/PaymentHistoryPage'
import { ContainersListPage } from '../features/client/pages/ContainersListPage'
import { ContainerDetailPage } from '../features/client/pages/ContainerDetailPage'
import { ClientInspectionsListPage } from '../features/client/pages/ClientInspectionsListPage'
import { ClientInspectionDetailPage } from '../features/client/pages/ClientInspectionDetailPage'
import { EntryConfirmationPage } from '../features/client/pages/EntryConfirmationPage'
import { EntryCompliancePage } from '../features/client/pages/EntryCompliancePage'
import { AccreditationCompliancePage } from '../features/client/pages/AccreditationCompliancePage'
import { AccreditationConfirmationPage } from '../features/client/pages/AccreditationConfirmationPage'

import { NotificationsPage } from '../features/notifications/pages/NotificationsPage'

import { VerifyCertificatePage } from '../features/client/pages/VerifyCertificatePage'

import { AgencyDashboardPage } from '../features/agency/pages/AgencyDashboardPage'

import { EvaluatorQueuePage } from '../features/agency/pages/EvaluatorQueuePage'

import { EvaluatorAssignmentsPage } from '../features/agency/pages/EvaluatorAssignmentsPage'

import { EntryEvaluationPage } from '../features/agency/pages/EntryEvaluationPage'

import { InspectionsPage } from '../features/agency/pages/InspectionsPage'
import { ContainerInspectionReviewPage } from '../features/agency/pages/ContainerInspectionReviewPage'

import { BillingPage } from '../features/agency/pages/BillingPage'
import { CreateBillingPage } from '../features/agency/pages/CreateBillingPage'
import { AgencyPaymentConfigPage } from '../features/agency/pages/AgencyPaymentConfigPage'

import { AccreditationReviewPage } from '../features/agency/pages/AccreditationReviewPage'

import { SecretaryReportsPage } from '../features/agency/pages/SecretaryReportsPage'
import { AgencyBillingReportsPage } from '../features/agency/pages/AgencyBillingReportsPage'

import { AccreditationReviewDetailPage } from '../features/agency/pages/AccreditationReviewDetailPage'
import { InspectorDashboardPage } from '../features/agency/pages/InspectorDashboardPage'
import { AccreditationOfficerDashboardPage } from '../features/accreditation-officer/pages/AccreditationOfficerDashboardPage'
import { AccreditationOfficerListPage } from '../features/accreditation-officer/pages/AccreditationOfficerListPage'
import { AccreditationOfficerDetailPage } from '../features/accreditation-officer/pages/AccreditationOfficerDetailPage'

import { AdminDashboardPage } from '../features/admin/pages/AdminDashboardPage'
import { AdminUsersPage } from '../features/admin/pages/AdminUsersPage'
import { AdminUserEditPage } from '../features/admin/pages/AdminUserEditPage'
import { AdminSettingsPage } from '../features/admin/pages/AdminSettingsPage'
import { AdminAgenciesPage } from '../features/admin/pages/AdminAgenciesPage'
import { AdminAgencyCreatePage } from '../features/admin/pages/AdminAgencyCreatePage'
import { AdminAgencyEditPage } from '../features/admin/pages/AdminAgencyEditPage'
import { AdminAgencyViewPage } from '../features/admin/pages/AdminAgencyViewPage'
import { AdminCommoditiesPage } from '../features/admin/pages/AdminCommoditiesPage'
import { AdminPaymentConfigPage } from '../features/admin/pages/AdminPaymentConfigPage'
import { AdminEntryPaymentsPage } from '../features/admin/pages/AdminEntryPaymentsPage'
import { AdminRevenuePage } from '../features/admin/pages/AdminRevenuePage'
import { AdminFormsPage } from '../features/admin/pages/AdminFormsPage'
import { AdminFormEditPage } from '../features/admin/pages/AdminFormEditPage'
import { AdminFormViewPage } from '../features/admin/pages/AdminFormViewPage'
import { AdminCertificateTemplatesPage } from '../features/admin/pages/AdminCertificateTemplatesPage'
import { AdminCertificateTemplateEditPage } from '../features/admin/pages/AdminCertificateTemplateEditPage'
import { AdminCertificateTemplateViewPage } from '../features/admin/pages/AdminCertificateTemplateViewPage'
import { AdminCertificatesPage } from '../features/admin/pages/AdminCertificatesPage'
import { AdminAuditLogsPage } from '../features/admin/pages/AdminAuditLogsPage'

import { MavDashboardPage } from '../features/mav/pages/MavDashboardPage'
import { MavApplicationsPage } from '../features/mav/pages/MavApplicationsPage'
import { MavLicensesPage, MavLicenseDetailPage } from '../features/mav/pages/MavLicensesPage'
import { MavAdminDashboardPage } from '../features/mav/pages/MavAdminDashboardPage'
import { MavAdminPeriodsPage } from '../features/mav/pages/MavAdminPeriodsPage'
import { MavAdminApplicationsPage } from '../features/mav/pages/MavAdminApplicationsPage'
import { MavAdminLicensesPage } from '../features/mav/pages/MavAdminLicensesPage'
import { MavCompliancePage } from '../features/mav/pages/MavCompliancePage'
import { MavReportsPage } from '../features/mav/pages/MavReportsPage'
import { MavYearTransitionPage } from '../features/mav/pages/MavYearTransitionPage'
import { MavApplicationDetailPage } from '../features/mav/pages/MavApplicationDetailPage'
import { MavHsLibraryPage } from '../features/mav/pages/MavHsLibraryPage'
import { MavAdminMicPage } from '../features/mav/pages/MavAdminMicPage'

import { WarehouseDashboardPage } from '../features/ops/pages/WarehouseDashboardPage'
import { WarehouseInventoryPage } from '../features/ops/pages/WarehouseInventoryPage'
import { WarehouseReceivePage } from '../features/ops/pages/WarehouseReceivePage'
import { WarehouseReleasesPage } from '../features/ops/pages/WarehouseReleasesPage'
import { DriverDashboardPage } from '../features/ops/pages/DriverDashboardPage'
import { DriverContainersPage } from '../features/ops/pages/DriverContainersPage'
import { DriverProfilePage } from '../features/ops/pages/DriverProfilePage'
import { OperatorContainersPage } from '../features/ops/pages/OperatorContainersPage'
import { OperatorDriversPage } from '../features/ops/pages/OperatorDriversPage'
import { OperatorVehiclesPage } from '../features/ops/pages/OperatorVehiclesPage'
import { OperatorInvitesPage } from '../features/ops/pages/OperatorInvitesPage'
import { DoctorContainersPage } from '../features/ops/pages/DoctorContainersPage'
import { TransportTagPage } from '../features/agency/pages/TransportTagPage'
import { TransportTagDetailPage } from '../features/agency/pages/TransportTagDetailPage'
import { DaDashboardPage } from '../features/da/pages/DaDashboardPage'
import { DaAgenciesPage } from '../features/da/pages/DaAgenciesPage'
import { DaAgencyOversightPage } from '../features/da/pages/DaAgencyOversightPage'
import { DaReportsPage } from '../features/da/pages/DaReportsPage'
import { DaMavNationalReportPage } from '../features/da/pages/DaMavNationalReportPage'
import { DaCommodityStockPage } from '../features/da/pages/DaCommodityStockPage'
import { DaImportPipelinePage } from '../features/da/pages/DaImportPipelinePage'
import { DaGeoStockPage } from '../features/da/pages/DaGeoStockPage'
import { DaWarehousesPage } from '../features/da/pages/DaWarehousesPage'
import { DaWarehouseDetailPage } from '../features/da/pages/DaWarehouseDetailPage'

function InspectorContainerInspectionRedirect() {
  const { containerUuid = '' } = useParams()
  return <Navigate to={`/inspector/inspections/containers/${containerUuid}`} replace />
}

export const router = createBrowserRouter([

  {

    path: '/',

    element: <PublicLayout />,

    children: [
      { index: true, element: <HomePage /> },
      { path: 'verify', element: <VerifyCertificatePage /> },
    ],

  },

  { path: '/client/payment/link/:token', element: <BillPaymentLinkPage /> },
  { path: '/client/payment/return/:billUuid', element: <BillPaymentReturnPage /> },

  {

    element: <GuestRoute />,

    children: [

      {

        element: <AuthLayout />,

        children: [

          { path: '/login', element: <LoginPage /> },

          { path: '/register', element: <RegisterPage /> },

          { path: '/forgot-password', element: <ForgotPasswordPage /> },

          { path: '/reset-password', element: <ResetPasswordPage /> },

          { path: '/resend-verification', element: <ResendVerificationPage /> },

        ],

      },

    ],

  },

  {

    element: <ProtectedRoute />,

    children: [

      {

        element: <AppPortalLayout />,

        children: [

          { path: '/profile', element: <ProfilePage /> },
          { path: '/notifications', element: <NotificationsPage /> },

          {

            element: <RoleProtectedRoute roles={['ROLE_IMPORTER', 'ROLE_EXPORTER', 'ROLE_BROKER']} />,

            children: [

              {

            path: '/client',

            children: [

              { index: true, element: <ClientDashboardPage /> },

              { path: 'entries/agencies', element: <EntryAgenciesPage /> },
              { path: 'entries', element: <EntriesListPage /> },
              { path: 'entries/new', element: <EntryFormPage /> },
              { path: 'entries/:uuid/edit', element: <EntryFormPage /> },
              { path: 'entries/:uuid', element: <EntryViewPage /> },
              { path: 'entries/:uuid/confirmation', element: <EntryConfirmationPage /> },
              { path: 'entries/:uuid/compliance', element: <EntryCompliancePage /> },
              { path: 'entries/:uuid/inspection', element: <EntryInspectionUploadPage /> },

              { path: 'accreditation', element: <AccreditationListPage /> },
              { path: 'accreditation/status', element: <Navigate to="/client/accreditation" replace /> },
              { path: 'accreditation/:uuid/compliance', element: <AccreditationCompliancePage /> },
              { path: 'accreditation/:uuid/confirmation', element: <AccreditationConfirmationPage /> },
              { path: 'accreditation/:uuid', element: <AccreditationDetailPage /> },

              { path: 'certificates', element: <CertificatesListPage /> },
              { path: 'certificates/:uuid', element: <CertificateDetailPage /> },

              { path: 'containers', element: <ContainersListPage /> },
              { path: 'containers/:uuid', element: <ContainerDetailPage /> },

              { path: 'inspections', element: <ClientInspectionsListPage /> },
              { path: 'inspections/:uuid', element: <ClientInspectionDetailPage /> },

              { path: 'warehouse/bookings', element: <WarehouseBookingsPage /> },
              { path: 'warehouse/bookings/:uuid', element: <WarehouseBookingDetailPage /> },

              { path: 'bills', element: <BillsListPage /> },
              { path: 'bills/:uuid', element: <BillDetailPage /> },
              { path: 'payment-history', element: <PaymentHistoryPage /> },

            ],

              },

            ],

          },

          {

            path: '/agency',

            children: [

              { index: true, element: <AgencyDashboardPage /> },

              { path: 'evaluator/queue', element: <EvaluatorQueuePage /> },

              { path: 'evaluator/assignments', element: <EvaluatorAssignmentsPage /> },

              { path: 'evaluator/entries/:uuid', element: <EntryEvaluationPage /> },

              {
                element: <RoleProtectedRoute roles={['ROLE_INSPECTOR', 'ROLE_ADMIN']} redirectTo="/agency" />,
                children: [
                  { path: 'inspections', element: <Navigate to="/inspector/inspections" replace /> },
                  { path: 'inspections/containers/:containerUuid', element: <InspectorContainerInspectionRedirect /> },
                ],
              },

              {
                element: <RoleProtectedRoute roles={['ROLE_BILLING_AGENT', 'ROLE_ACCOUNTANT', 'ROLE_ADMIN']} />,
                children: [
                  { path: 'billing/create', element: <CreateBillingPage /> },
                  { path: 'billing/create/:entryUuid', element: <CreateBillingPage /> },
                  { path: 'billing/:billingUuid', element: <CreateBillingPage /> },
                  { path: 'billing', element: <BillingPage /> },
                  { path: 'billing-reports', element: <AgencyBillingReportsPage /> },
                ],
              },

              { path: 'payment-config', element: <AgencyPaymentConfigPage /> },

              { path: 'transport-tags', element: <TransportTagPage /> },
              { path: 'transport-tags/:containerUuid', element: <TransportTagDetailPage /> },

              { path: 'accreditation', element: <AccreditationReviewPage /> },

              { path: 'accreditation/:uuid', element: <AccreditationReviewDetailPage /> },

              { path: 'reports', element: <SecretaryReportsPage /> },

            ],

          },

          {

            element: <RoleProtectedRoute roles={['ROLE_DA_SECRETARY', 'ROLE_DA_UNDERSECRETARY', 'ROLE_ADMIN']} />,

            children: [

              {

                path: '/da',

                children: [

                  { index: true, element: <DaDashboardPage /> },

                  { path: 'agencies', element: <DaAgenciesPage /> },

                  { path: 'agencies/:id', element: <DaAgencyOversightPage /> },

                  { path: 'warehouses', element: <DaWarehousesPage /> },

                  { path: 'warehouses/:id', element: <DaWarehouseDetailPage /> },

                  { path: 'reports', element: <DaReportsPage /> },
                  { path: 'reports/mav', element: <DaMavNationalReportPage /> },
                  { path: 'reports/import-pipeline', element: <DaImportPipelinePage /> },
                  { path: 'reports/commodities', element: <DaCommodityStockPage /> },
                  { path: 'reports/stock', element: <DaGeoStockPage /> },

                ],

              },

            ],

          },

          {

            element: <RoleProtectedRoute roles={['ROLE_ACCREDITATION_OFFICER', 'ROLE_ADMIN']} />,

            children: [

              {

                path: '/accreditation-officer',

                children: [

                  { index: true, element: <Navigate to="/accreditation-officer/dashboard" replace /> },

                  { path: 'dashboard', element: <AccreditationOfficerDashboardPage /> },

                  { path: 'accreditations', element: <AccreditationOfficerListPage /> },

                  { path: 'accreditations/:uuid', element: <AccreditationOfficerDetailPage /> },

                ],

              },

            ],

          },

          {

            element: <RoleProtectedRoute roles={['ROLE_INSPECTOR', 'ROLE_ADMIN']} redirectTo="/agency" />,

            children: [

              {

                path: '/inspector',

                children: [

                  { index: true, element: <InspectorDashboardPage /> },

                  { path: 'inspections', element: <InspectionsPage /> },
                  { path: 'inspections/containers/:containerUuid', element: <ContainerInspectionReviewPage /> },

                ],

              },

            ],

          },

          {

            path: '/admin',

            children: [

              { index: true, element: <AdminDashboardPage /> },

              { path: 'users', element: <AdminUsersPage /> },

              { path: 'users/:uuid/edit', element: <AdminUserEditPage /> },

              { path: 'agencies', element: <AdminAgenciesPage /> },
              { path: 'agencies/create', element: <AdminAgencyCreatePage /> },
              { path: 'agencies/:id/view', element: <AdminAgencyViewPage /> },
              { path: 'agencies/:id/edit', element: <AdminAgencyEditPage /> },

              { path: 'commodities', element: <AdminCommoditiesPage /> },

              { path: 'payment-config', element: <AdminPaymentConfigPage /> },
              { path: 'entry-payments', element: <AdminEntryPaymentsPage /> },
              { path: 'revenue', element: <AdminRevenuePage /> },

              { path: 'forms', element: <AdminFormsPage /> },
              { path: 'forms/:uuid/view', element: <AdminFormViewPage /> },
              { path: 'forms/:uuid/edit', element: <AdminFormEditPage /> },

              { path: 'certificate-templates', element: <AdminCertificateTemplatesPage /> },
              { path: 'certificate-templates/:uuid', element: <AdminCertificateTemplateViewPage /> },
              { path: 'certificate-templates/:uuid/edit', element: <AdminCertificateTemplateEditPage /> },

              { path: 'certificates', element: <AdminCertificatesPage /> },

              { path: 'audit-logs', element: <AdminAuditLogsPage /> },

              { path: 'settings', element: <AdminSettingsPage /> },

            ],

          },

          {

            path: '/mav',

            children: [

              {

                element: <RoleProtectedRoute roles={['ROLE_MAV_IMPORTER', 'ROLE_IMPORTER', 'ROLE_EXPORTER', 'ROLE_BROKER', 'ROLE_MAV_ADMIN', 'ROLE_MAV_EVALUATOR', 'ROLE_MAV_SECRETARY']} />,

                children: [

                  { index: true, element: <MavDashboardPage /> },

                  { path: 'applications', element: <MavApplicationsPage /> },

                  { path: 'applications/:uuid', element: <MavApplicationDetailPage /> },

                  { path: 'licenses', element: <MavLicensesPage /> },

                  { path: 'licenses/:uuid', element: <MavLicenseDetailPage /> },

                ],

              },

              {

                element: <RoleProtectedRoute roles={['ROLE_MAV_ADMIN', 'ROLE_MAV_EVALUATOR', 'ROLE_MAV_SECRETARY']} />,

                children: [

                  { path: 'admin', element: <MavAdminDashboardPage /> },

                  { path: 'admin/periods', element: <MavAdminPeriodsPage /> },

                  { path: 'admin/applications', element: <MavAdminApplicationsPage /> },

                  { path: 'admin/licenses', element: <MavAdminLicensesPage /> },

                  { path: 'admin/mic', element: <MavAdminMicPage /> },

                  { path: 'admin/hs-library', element: <MavHsLibraryPage /> },

                  { path: 'admin/compliance', element: <MavCompliancePage /> },

                  { path: 'admin/reports', element: <MavReportsPage /> },

                  { path: 'admin/year-transition', element: <MavYearTransitionPage /> },

                ],

              },

            ],

          },

          {

            element: <RoleProtectedRoute roles={['ROLE_WAREHOUSE_STAFF', 'ROLE_ADMIN']} />,

            children: [

              {

            path: '/warehouse',

            children: [
              { index: true, element: <WarehouseDashboardPage /> },
              { path: 'inventory', element: <WarehouseInventoryPage /> },
              { path: 'receive', element: <WarehouseReceivePage /> },
              { path: 'releases', element: <WarehouseReleasesPage /> },
            ],

              },

            ],

          },

          {

            element: <RoleProtectedRoute roles={['ROLE_DRIVER', 'ROLE_OPERATOR', 'ROLE_ADMIN']} />,

            children: [

              {

            path: '/driver',

            children: [
              { index: true, element: <DriverDashboardPage /> },
              { path: 'containers', element: <DriverContainersPage /> },
              { path: 'profile', element: <DriverProfilePage /> },
            ],

              },

            ],

          },

          {

            element: <RoleProtectedRoute roles={['ROLE_OPERATOR', 'ROLE_ADMIN']} />,

            children: [

              {

            path: '/operator',

            children: [
              { index: true, element: <Navigate to="/operator/containers" replace /> },
              { path: 'containers', element: <OperatorContainersPage /> },
              { path: 'drivers', element: <OperatorDriversPage /> },
              { path: 'vehicles', element: <OperatorVehiclesPage /> },
              { path: 'invites', element: <OperatorInvitesPage /> },
            ],

              },

            ],

          },

          {

            element: <RoleProtectedRoute roles={['ROLE_DOCTOR', 'ROLE_INSPECTOR', 'ROLE_ADMIN']} />,

            children: [

              {

            path: '/doctor',

            children: [
              { index: true, element: <Navigate to="/doctor/containers" replace /> },
              { path: 'containers', element: <DoctorContainersPage /> },
            ],

              },

            ],

          },

        ],

      },

    ],

  },

  { path: '*', element: <Navigate to="/" replace /> },

])


