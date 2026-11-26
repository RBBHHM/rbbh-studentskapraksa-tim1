using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RBBH.ConnectedParties.Api.Migrations
{
    /// <inheritdoc />
    public partial class StandardizeRegistrySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            StageLegacyData(migrationBuilder);

            migrationBuilder.DropForeignKey(
                name: "FK_FamilyMembers_FamilyMembers_ParentFamilyMemberId",
                table: "FamilyMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_FamilyMembers_RelatedPersons_RelatedPersonId",
                table: "FamilyMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_LegalEntityVerificationOrigins_LegalEntities_LegalEntityId",
                table: "LegalEntityVerificationOrigins");

            migrationBuilder.DropForeignKey(
                name: "FK_Limits_LegalEntities_LegalEntityId",
                table: "Limits");

            migrationBuilder.DropForeignKey(
                name: "FK_NotificationDeliveries_Notifications_NotificationId",
                table: "NotificationDeliveries");

            migrationBuilder.DropForeignKey(
                name: "FK_RelatedPersons_RelatedPersons_RelatedToPersonId",
                table: "RelatedPersons");

            migrationBuilder.DropForeignKey(
                name: "FK_RequestedRole_RoleRequests_RequestId",
                table: "RequestedRole");

            migrationBuilder.DropForeignKey(
                name: "FK_RequestedRole_Roles_RoleId",
                table: "RequestedRole");

            migrationBuilder.DropForeignKey(
                name: "FK_RolePermissions_Permissions_PermissionId",
                table: "RolePermissions");

            migrationBuilder.DropForeignKey(
                name: "FK_RolePermissions_Roles_RoleId",
                table: "RolePermissions");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_AppUsers_UserId",
                table: "UserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Roles_RoleId",
                table: "UserRoles");

            migrationBuilder.DropTable(
                name: "CapitalRevisions");

            migrationBuilder.DropTable(
                name: "ClientLimits");

            migrationBuilder.DropTable(
                name: "CodeListLocalization");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Roles",
                table: "Roles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Roles_Name_Trimmed",
                table: "Roles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Reports",
                table: "Reports");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Permissions",
                table: "Permissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Notifications",
                table: "Notifications");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Limits",
                table: "Limits");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Capitals",
                table: "Capitals");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserRoles",
                table: "UserRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UnlockRequests",
                table: "UnlockRequests");

            migrationBuilder.DropIndex(
                name: "IX_UnlockRequests_ParentRequestId",
                table: "UnlockRequests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RoleRequests",
                table: "RoleRequests");

            migrationBuilder.DropIndex(
                name: "IX_RoleRequests_RequesterSubject",
                table: "RoleRequests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RolePermissions",
                table: "RolePermissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RequestedRole",
                table: "RequestedRole");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RelatedPersons",
                table: "RelatedPersons");

            migrationBuilder.DropIndex(
                name: "IX_RelatedPersons_FBAId",
                table: "RelatedPersons");

            migrationBuilder.DropIndex(
                name: "IX_RelatedPersons_JMBG",
                table: "RelatedPersons");

            migrationBuilder.DropIndex(
                name: "IX_RelatedPersons_PassportNumber",
                table: "RelatedPersons");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RelatedPersons_FBAId_Trimmed",
                table: "RelatedPersons");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RelatedPersons_PassportNumber_Trimmed",
                table: "RelatedPersons");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PeriodLocks",
                table: "PeriodLocks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_NotificationDeliveries",
                table: "NotificationDeliveries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LegalEntityVerificationOrigins",
                table: "LegalEntityVerificationOrigins");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LegalEntities",
                table: "LegalEntities");

            migrationBuilder.DropIndex(
                name: "IX_LegalEntities_FbaId",
                table: "LegalEntities");

            migrationBuilder.DropIndex(
                name: "IX_LegalEntities_TaxNumber",
                table: "LegalEntities");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FamilyMembers",
                table: "FamilyMembers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CodeListValues",
                table: "CodeListValues");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CodeListValues_Category_Trimmed",
                table: "CodeListValues");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CodeListValues_ValueKey_Trimmed",
                table: "CodeListValues");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CodeListDefinitions",
                table: "CodeListDefinitions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CodeListDefinitions_Name_Trimmed",
                table: "CodeListDefinitions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AuditLogs",
                table: "AuditLogs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AppUsers",
                table: "AppUsers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AppUsers_Email_Trimmed",
                table: "AppUsers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AppUsers_Username_Trimmed",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "RecordId",
                table: "AuditLogs");

            migrationBuilder.DropSequence(
                name: "ClientLimitsIdSequence");

            migrationBuilder.RenameTable(
                name: "Roles",
                newName: "ROLES");

            migrationBuilder.RenameTable(
                name: "Reports",
                newName: "REPORTS");

            migrationBuilder.RenameTable(
                name: "Permissions",
                newName: "PERMISSIONS");

            migrationBuilder.RenameTable(
                name: "Notifications",
                newName: "NOTIFICATIONS");

            migrationBuilder.RenameTable(
                name: "Limits",
                newName: "LIMITS");

            migrationBuilder.RenameTable(
                name: "Capitals",
                newName: "CAPITALS");

            migrationBuilder.RenameTable(
                name: "UserRoles",
                newName: "USER_ROLES");

            migrationBuilder.RenameTable(
                name: "UnlockRequests",
                newName: "UNLOCK_REQUESTS");

            migrationBuilder.RenameTable(
                name: "RoleRequests",
                newName: "ROLE_REQUESTS");

            migrationBuilder.RenameTable(
                name: "RolePermissions",
                newName: "ROLE_PERMISSIONS");

            migrationBuilder.RenameTable(
                name: "RequestedRole",
                newName: "REQUESTED_ROLE");

            migrationBuilder.RenameTable(
                name: "RelatedPersons",
                newName: "RELATED_PERSONS");

            migrationBuilder.RenameTable(
                name: "PeriodLocks",
                newName: "PERIOD_LOCKS");

            migrationBuilder.RenameTable(
                name: "NotificationDeliveries",
                newName: "NOTIFICATION_DELIVERIES");

            migrationBuilder.RenameTable(
                name: "LegalEntityVerificationOrigins",
                newName: "LEGAL_ENTITY_VERIFICATION_ORIGINS");

            migrationBuilder.RenameTable(
                name: "LegalEntities",
                newName: "LEGAL_ENTITIES");

            migrationBuilder.RenameTable(
                name: "FamilyMembers",
                newName: "FAMILY_MEMBERS");

            migrationBuilder.RenameTable(
                name: "CodeListValues",
                newName: "CODE_LIST_VALUES");

            migrationBuilder.RenameTable(
                name: "CodeListDefinitions",
                newName: "CODELIST_DEFINITIONS");

            migrationBuilder.RenameTable(
                name: "AuditLogs",
                newName: "AUDIT_LOGS");

            migrationBuilder.RenameTable(
                name: "AppUsers",
                newName: "APP_USERS");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "ROLES",
                newName: "NAME");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "ROLES",
                newName: "DESCRIPTION");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "ROLES",
                newName: "ID");

            migrationBuilder.RenameColumn(
                name: "ModifiedBy",
                table: "ROLES",
                newName: "MODIFIED_BY");

            migrationBuilder.RenameColumn(
                name: "ModifiedAt",
                table: "ROLES",
                newName: "MODIFIED_AT");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "ROLES",
                newName: "IS_ACTIVE");

            migrationBuilder.RenameColumn(
                name: "DisplayName",
                table: "ROLES",
                newName: "DISPLAY_NAME");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "ROLES",
                newName: "CREATED_BY");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "ROLES",
                newName: "CREATED_AT");

            migrationBuilder.RenameIndex(
                name: "IX_Roles_Name",
                table: "ROLES",
                newName: "IX_ROLES_NAME");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "REPORTS",
                newName: "ID");

            migrationBuilder.RenameColumn(
                name: "TotalExposure",
                table: "REPORTS",
                newName: "TOTAL_EXPOSURE");

            migrationBuilder.RenameColumn(
                name: "TotalClients",
                table: "REPORTS",
                newName: "TOTAL_CLIENTS");

            migrationBuilder.RenameColumn(
                name: "ReportType",
                table: "REPORTS",
                newName: "REPORT_TYPE");

            migrationBuilder.RenameColumn(
                name: "ReportDate",
                table: "REPORTS",
                newName: "REPORT_DATE");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "REPORTS",
                newName: "IS_ACTIVE");

            migrationBuilder.RenameColumn(
                name: "DataSource",
                table: "REPORTS",
                newName: "DATA_SOURCE");

            migrationBuilder.RenameColumn(
                name: "DataSnapshot",
                table: "REPORTS",
                newName: "DATA_SNAPSHOT");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "REPORTS",
                newName: "CREATED_BY");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "REPORTS",
                newName: "CREATED_AT");

            migrationBuilder.RenameColumn(
                name: "ClientsWithBreachedLimit",
                table: "REPORTS",
                newName: "CLIENTS_WITH_BREACHED_LIMIT");

            migrationBuilder.RenameIndex(
                name: "IX_Reports_ReportType_ReportDate",
                table: "REPORTS",
                newName: "IX_REPORTS_REPORT_TYPE_REPORT_DATE");

            migrationBuilder.RenameIndex(
                name: "IX_Reports_ReportType",
                table: "REPORTS",
                newName: "IX_REPORTS_REPORT_TYPE");

            migrationBuilder.RenameIndex(
                name: "IX_Reports_ReportDate",
                table: "REPORTS",
                newName: "IX_REPORTS_REPORT_DATE");

            migrationBuilder.RenameColumn(
                name: "Module",
                table: "PERMISSIONS",
                newName: "MODULE");

            migrationBuilder.RenameColumn(
                name: "Code",
                table: "PERMISSIONS",
                newName: "CODE");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "PERMISSIONS",
                newName: "ID");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "PERMISSIONS",
                newName: "IS_ACTIVE");

            migrationBuilder.RenameIndex(
                name: "IX_Permissions_Code",
                table: "PERMISSIONS",
                newName: "IX_PERMISSIONS_CODE");

            migrationBuilder.RenameColumn(
                name: "Route",
                table: "NOTIFICATIONS",
                newName: "ROUTE");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "NOTIFICATIONS",
                newName: "ID");

            migrationBuilder.RenameColumn(
                name: "TitleKey",
                table: "NOTIFICATIONS",
                newName: "TITLE_KEY");

            migrationBuilder.RenameColumn(
                name: "RecipientSubject",
                table: "NOTIFICATIONS",
                newName: "RECIPIENT_SUBJECT");

            migrationBuilder.RenameColumn(
                name: "RecipientEmail",
                table: "NOTIFICATIONS",
                newName: "RECIPIENT_EMAIL");

            migrationBuilder.RenameColumn(
                name: "ReadAt",
                table: "NOTIFICATIONS",
                newName: "READ_AT");

            migrationBuilder.RenameColumn(
                name: "ParametersJson",
                table: "NOTIFICATIONS",
                newName: "PARAMETERS_JSON");

            migrationBuilder.RenameColumn(
                name: "EventKey",
                table: "NOTIFICATIONS",
                newName: "EVENT_KEY");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "NOTIFICATIONS",
                newName: "CREATED_AT");

            migrationBuilder.RenameColumn(
                name: "BodyKey",
                table: "NOTIFICATIONS",
                newName: "BODY_KEY");

            migrationBuilder.RenameIndex(
                name: "IX_Notifications_RecipientSubject_EventKey",
                table: "NOTIFICATIONS",
                newName: "IX_NOTIFICATIONS_RECIPIENT_SUBJECT_EVENT_KEY");

            migrationBuilder.RenameIndex(
                name: "IX_Notifications_RecipientSubject_CreatedAt",
                table: "NOTIFICATIONS",
                newName: "IX_NOTIFICATIONS_RECIPIENT_SUBJECT_CREATED_AT");

            migrationBuilder.RenameColumn(
                name: "Comment",
                table: "LIMITS",
                newName: "COMMENT");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "LIMITS",
                newName: "ID");

            migrationBuilder.RenameColumn(
                name: "ModifiedBy",
                table: "LIMITS",
                newName: "MODIFIED_BY");

            migrationBuilder.RenameColumn(
                name: "ModifiedAt",
                table: "LIMITS",
                newName: "MODIFIED_AT");

            migrationBuilder.RenameColumn(
                name: "MaximumExpectedUtilization",
                table: "LIMITS",
                newName: "MAXIMUM_EXPECTED_UTILIZATION");

            migrationBuilder.RenameColumn(
                name: "LimitType",
                table: "LIMITS",
                newName: "LIMIT_TYPE");

            migrationBuilder.RenameColumn(
                name: "LegalEntityId",
                table: "LIMITS",
                newName: "LEGAL_ENTITY_ID");

            migrationBuilder.RenameColumn(
                name: "ExpectedUtilizationDate",
                table: "LIMITS",
                newName: "EXPECTED_UTILIZATION_DATE");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "LIMITS",
                newName: "CREATED_BY");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "LIMITS",
                newName: "CREATED_AT");

            migrationBuilder.RenameColumn(
                name: "ApprovedLimit",
                table: "LIMITS",
                newName: "APPROVED_LIMIT");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "CAPITALS",
                newName: "ID");

            migrationBuilder.RenameColumn(
                name: "SupplementaryCapital",
                table: "CAPITALS",
                newName: "SUPPLEMENTARY_CAPITAL");

            migrationBuilder.RenameColumn(
                name: "RegulatoryCapital",
                table: "CAPITALS",
                newName: "REGULATORY_CAPITAL");

            migrationBuilder.RenameColumn(
                name: "ModifiedBy",
                table: "CAPITALS",
                newName: "MODIFIED_BY");

            migrationBuilder.RenameColumn(
                name: "ModifiedAt",
                table: "CAPITALS",
                newName: "MODIFIED_AT");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "CAPITALS",
                newName: "IS_ACTIVE");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "CAPITALS",
                newName: "CREATED_BY");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "CAPITALS",
                newName: "CREATED_AT");

            migrationBuilder.RenameColumn(
                name: "CoreCapital",
                table: "CAPITALS",
                newName: "CORE_CAPITAL");

            migrationBuilder.RenameColumn(
                name: "CapitalDate",
                table: "CAPITALS",
                newName: "VALID_FROM");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "USER_ROLES",
                newName: "ID");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "USER_ROLES",
                newName: "USER_ID");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                table: "USER_ROLES",
                newName: "ROLE_ID");

            migrationBuilder.RenameColumn(
                name: "ModifiedBy",
                table: "USER_ROLES",
                newName: "MODIFIED_BY");

            migrationBuilder.RenameColumn(
                name: "ModifiedAt",
                table: "USER_ROLES",
                newName: "MODIFIED_AT");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "USER_ROLES",
                newName: "IS_ACTIVE");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "USER_ROLES",
                newName: "CREATED_BY");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "USER_ROLES",
                newName: "CREATED_AT");

            migrationBuilder.RenameIndex(
                name: "IX_UserRoles_UserId_RoleId",
                table: "USER_ROLES",
                newName: "IX_USER_ROLES_USER_ID_ROLE_ID");

            migrationBuilder.RenameIndex(
                name: "IX_UserRoles_RoleId",
                table: "USER_ROLES",
                newName: "IX_USER_ROLES_ROLE_ID");

            migrationBuilder.RenameColumn(
                name: "Year",
                table: "UNLOCK_REQUESTS",
                newName: "YEAR");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "UNLOCK_REQUESTS",
                newName: "STATUS");

            migrationBuilder.RenameColumn(
                name: "Reason",
                table: "UNLOCK_REQUESTS",
                newName: "REASON");

            migrationBuilder.RenameColumn(
                name: "Month",
                table: "UNLOCK_REQUESTS",
                newName: "MONTH");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "UNLOCK_REQUESTS",
                newName: "ID");

            migrationBuilder.RenameColumn(
                name: "RequestedBySubject",
                table: "UNLOCK_REQUESTS",
                newName: "REQUESTED_BY_SUBJECT");

            migrationBuilder.RenameColumn(
                name: "RequestedByEmail",
                table: "UNLOCK_REQUESTS",
                newName: "REQUESTED_BY_EMAIL");

            migrationBuilder.RenameColumn(
                name: "RequestedBy",
                table: "UNLOCK_REQUESTS",
                newName: "REQUESTED_BY");

            migrationBuilder.RenameColumn(
                name: "ProcessedBy",
                table: "UNLOCK_REQUESTS",
                newName: "PROCESSED_BY");

            migrationBuilder.RenameColumn(
                name: "ProcessedAt",
                table: "UNLOCK_REQUESTS",
                newName: "PROCESSED_AT");

            migrationBuilder.RenameColumn(
                name: "ParentRequestId",
                table: "UNLOCK_REQUESTS",
                newName: "PARENT_REQUEST_ID");

            migrationBuilder.RenameColumn(
                name: "ModifiedBy",
                table: "UNLOCK_REQUESTS",
                newName: "MODIFIED_BY");

            migrationBuilder.RenameColumn(
                name: "ModifiedAt",
                table: "UNLOCK_REQUESTS",
                newName: "MODIFIED_AT");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "UNLOCK_REQUESTS",
                newName: "IS_ACTIVE");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "UNLOCK_REQUESTS",
                newName: "CREATED_BY");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "UNLOCK_REQUESTS",
                newName: "CREATED_AT");

            migrationBuilder.RenameColumn(
                name: "AdminNote",
                table: "UNLOCK_REQUESTS",
                newName: "ADMIN_NOTE");

            migrationBuilder.RenameIndex(
                name: "IX_UnlockRequests_Year_Month",
                table: "UNLOCK_REQUESTS",
                newName: "IX_UNLOCK_REQUESTS_YEAR_MONTH");

            migrationBuilder.RenameIndex(
                name: "IX_UnlockRequests_Status",
                table: "UNLOCK_REQUESTS",
                newName: "IX_UNLOCK_REQUESTS_STATUS");

            migrationBuilder.RenameIndex(
                name: "IX_UnlockRequests_RequestedBySubject",
                table: "UNLOCK_REQUESTS",
                newName: "IX_UNLOCK_REQUESTS_REQUESTED_BY_SUBJECT");

            migrationBuilder.RenameColumn(
                name: "Version",
                table: "ROLE_REQUESTS",
                newName: "VERSION");

            migrationBuilder.RenameColumn(
                name: "Username",
                table: "ROLE_REQUESTS",
                newName: "USERNAME");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "ROLE_REQUESTS",
                newName: "STATUS");

            migrationBuilder.RenameColumn(
                name: "Purpose",
                table: "ROLE_REQUESTS",
                newName: "PURPOSE");

            migrationBuilder.RenameColumn(
                name: "Note",
                table: "ROLE_REQUESTS",
                newName: "NOTE");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "ROLE_REQUESTS",
                newName: "EMAIL");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "ROLE_REQUESTS",
                newName: "ID");

            migrationBuilder.RenameColumn(
                name: "RequesterSubject",
                table: "ROLE_REQUESTS",
                newName: "REQUESTER_SUBJECT");

            migrationBuilder.RenameColumn(
                name: "ProcessedBy",
                table: "ROLE_REQUESTS",
                newName: "PROCESSED_BY");

            migrationBuilder.RenameColumn(
                name: "ProcessedAt",
                table: "ROLE_REQUESTS",
                newName: "PROCESSED_AT");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "ROLE_REQUESTS",
                newName: "LAST_NAME");

            migrationBuilder.RenameColumn(
                name: "FirstName",
                table: "ROLE_REQUESTS",
                newName: "FIRST_NAME");

            migrationBuilder.RenameColumn(
                name: "DecisionNote",
                table: "ROLE_REQUESTS",
                newName: "DECISION_NOTE");

            migrationBuilder.RenameColumn(
                name: "DecisionId",
                table: "ROLE_REQUESTS",
                newName: "DECISION_ID");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "ROLE_REQUESTS",
                newName: "CREATED_AT");

            migrationBuilder.RenameColumn(
                name: "ClientRequestId",
                table: "ROLE_REQUESTS",
                newName: "CLIENT_REQUEST_ID");

            migrationBuilder.RenameIndex(
                name: "IX_RoleRequests_Status_CreatedAt",
                table: "ROLE_REQUESTS",
                newName: "IX_ROLE_REQUESTS_STATUS_CREATED_AT");

            migrationBuilder.RenameIndex(
                name: "IX_RoleRequests_RequesterSubject_ClientRequestId",
                table: "ROLE_REQUESTS",
                newName: "IX_ROLE_REQUESTS_REQUESTER_SUBJECT_CLIENT_REQUEST_ID");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "ROLE_PERMISSIONS",
                newName: "ID");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                table: "ROLE_PERMISSIONS",
                newName: "ROLE_ID");

            migrationBuilder.RenameColumn(
                name: "PermissionId",
                table: "ROLE_PERMISSIONS",
                newName: "PERMISSION_ID");

            migrationBuilder.RenameIndex(
                name: "IX_RolePermissions_RoleId_PermissionId",
                table: "ROLE_PERMISSIONS",
                newName: "IX_ROLE_PERMISSIONS_ROLE_ID_PERMISSION_ID");

            migrationBuilder.RenameIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "ROLE_PERMISSIONS",
                newName: "IX_ROLE_PERMISSIONS_PERMISSION_ID");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "REQUESTED_ROLE",
                newName: "ID");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                table: "REQUESTED_ROLE",
                newName: "ROLE_ID");

            migrationBuilder.RenameColumn(
                name: "RequestId",
                table: "REQUESTED_ROLE",
                newName: "REQUEST_ID");

            migrationBuilder.RenameIndex(
                name: "IX_RequestedRole_RoleId",
                table: "REQUESTED_ROLE",
                newName: "IX_REQUESTED_ROLE_ROLE_ID");

            migrationBuilder.RenameIndex(
                name: "IX_RequestedRole_RequestId_RoleId",
                table: "REQUESTED_ROLE",
                newName: "IX_REQUESTED_ROLE_REQUEST_ID_ROLE_ID");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "RELATED_PERSONS",
                newName: "STATUS");

            migrationBuilder.RenameColumn(
                name: "Residency",
                table: "RELATED_PERSONS",
                newName: "RESIDENCY");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "RELATED_PERSONS",
                newName: "ID");

            migrationBuilder.RenameColumn(
                name: "VerifiedBy",
                table: "RELATED_PERSONS",
                newName: "VERIFIED_BY");

            migrationBuilder.RenameColumn(
                name: "VerifiedAt",
                table: "RELATED_PERSONS",
                newName: "VERIFIED_AT");

            migrationBuilder.RenameColumn(
                name: "VerificationOriginFingerprint",
                table: "RELATED_PERSONS",
                newName: "VERIFICATION_ORIGIN_FINGERPRINT");

            migrationBuilder.RenameColumn(
                name: "SpecialRelationshipWithBank",
                table: "RELATED_PERSONS",
                newName: "SPECIAL_RELATIONSHIP_WITH_BANK");

            migrationBuilder.RenameColumn(
                name: "SpecialRelationBasis",
                table: "RELATED_PERSONS",
                newName: "SPECIAL_RELATION_BASIS");

            migrationBuilder.RenameColumn(
                name: "SpecialContract",
                table: "RELATED_PERSONS",
                newName: "SPECIAL_CONTRACT");

            migrationBuilder.RenameColumn(
                name: "RelationBasis",
                table: "RELATED_PERSONS",
                newName: "RELATION_BASIS");

            migrationBuilder.RenameColumn(
                name: "RelatedToPersonId",
                table: "RELATED_PERSONS",
                newName: "RELATED_TO_PERSON_ID");

            migrationBuilder.RenameColumn(
                name: "PassportNumber",
                table: "RELATED_PERSONS",
                newName: "PASSPORT_NUMBER");

            migrationBuilder.RenameColumn(
                name: "ModifiedBy",
                table: "RELATED_PERSONS",
                newName: "MODIFIED_BY");

            migrationBuilder.RenameColumn(
                name: "ModifiedAt",
                table: "RELATED_PERSONS",
                newName: "MODIFIED_AT");

            migrationBuilder.RenameColumn(
                name: "MalusClawback",
                table: "RELATED_PERSONS",
                newName: "MALUS_CLAWBACK");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "RELATED_PERSONS",
                newName: "LAST_NAME");

            migrationBuilder.RenameColumn(
                name: "IsIdentifiedStaff",
                table: "RELATED_PERSONS",
                newName: "IS_IDENTIFIED_STAFF");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "RELATED_PERSONS",
                newName: "IS_ACTIVE");

            migrationBuilder.RenameColumn(
                name: "FirstName",
                table: "RELATED_PERSONS",
                newName: "FIRST_NAME");

            migrationBuilder.RenameColumn(
                name: "FBAId",
                table: "RELATED_PERSONS",
                newName: "FBA_ID");

            migrationBuilder.RenameColumn(
                name: "DeclarationNoFamilyMembers",
                table: "RELATED_PERSONS",
                newName: "DECLARATION_NO_FAMILY_MEMBERS");

            migrationBuilder.RenameColumn(
                name: "DateTo",
                table: "RELATED_PERSONS",
                newName: "DATE_TO");

            migrationBuilder.RenameColumn(
                name: "DateFrom",
                table: "RELATED_PERSONS",
                newName: "DATE_FROM");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "RELATED_PERSONS",
                newName: "CREATED_BY");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "RELATED_PERSONS",
                newName: "CREATED_AT");

            migrationBuilder.RenameColumn(
                name: "ConnectedWithBank",
                table: "RELATED_PERSONS",
                newName: "CONNECTED_WITH_BANK");

            migrationBuilder.RenameIndex(
                name: "IX_RelatedPersons_Status",
                table: "RELATED_PERSONS",
                newName: "IX_RELATED_PERSONS_STATUS");

            migrationBuilder.RenameIndex(
                name: "IX_RelatedPersons_RelatedToPersonId",
                table: "RELATED_PERSONS",
                newName: "IX_RELATED_PERSONS_RELATED_TO_PERSON_ID");

            migrationBuilder.RenameColumn(
                name: "Year",
                table: "PERIOD_LOCKS",
                newName: "YEAR");

            migrationBuilder.RenameColumn(
                name: "Month",
                table: "PERIOD_LOCKS",
                newName: "MONTH");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "PERIOD_LOCKS",
                newName: "ID");

            migrationBuilder.RenameColumn(
                name: "UnlockedBy",
                table: "PERIOD_LOCKS",
                newName: "UNLOCKED_BY");

            migrationBuilder.RenameColumn(
                name: "UnlockedAt",
                table: "PERIOD_LOCKS",
                newName: "UNLOCKED_AT");

            migrationBuilder.RenameColumn(
                name: "ModifiedBy",
                table: "PERIOD_LOCKS",
                newName: "MODIFIED_BY");

            migrationBuilder.RenameColumn(
                name: "ModifiedAt",
                table: "PERIOD_LOCKS",
                newName: "MODIFIED_AT");

            migrationBuilder.RenameColumn(
                name: "LockedBy",
                table: "PERIOD_LOCKS",
                newName: "LOCKED_BY");

            migrationBuilder.RenameColumn(
                name: "LockedAt",
                table: "PERIOD_LOCKS",
                newName: "LOCKED_AT");

            migrationBuilder.RenameColumn(
                name: "IsLocked",
                table: "PERIOD_LOCKS",
                newName: "IS_LOCKED");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "PERIOD_LOCKS",
                newName: "IS_ACTIVE");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "PERIOD_LOCKS",
                newName: "CREATED_BY");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "PERIOD_LOCKS",
                newName: "CREATED_AT");

            migrationBuilder.RenameIndex(
                name: "IX_PeriodLocks_Year_Month",
                table: "PERIOD_LOCKS",
                newName: "IX_PERIOD_LOCKS_YEAR_MONTH");

            migrationBuilder.RenameColumn(
                name: "Attempts",
                table: "NOTIFICATION_DELIVERIES",
                newName: "ATTEMPTS");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "NOTIFICATION_DELIVERIES",
                newName: "ID");

            migrationBuilder.RenameColumn(
                name: "NotificationId",
                table: "NOTIFICATION_DELIVERIES",
                newName: "NOTIFICATION_ID");

            migrationBuilder.RenameColumn(
                name: "NextAttemptAt",
                table: "NOTIFICATION_DELIVERIES",
                newName: "NEXT_ATTEMPT_AT");

            migrationBuilder.RenameColumn(
                name: "LeaseUntil",
                table: "NOTIFICATION_DELIVERIES",
                newName: "LEASE_UNTIL");

            migrationBuilder.RenameColumn(
                name: "LeaseId",
                table: "NOTIFICATION_DELIVERIES",
                newName: "LEASE_ID");

            migrationBuilder.RenameColumn(
                name: "FailureCode",
                table: "NOTIFICATION_DELIVERIES",
                newName: "FAILURE_CODE");

            migrationBuilder.RenameColumn(
                name: "DeliveredAt",
                table: "NOTIFICATION_DELIVERIES",
                newName: "DELIVERED_AT");

            migrationBuilder.RenameIndex(
                name: "IX_NotificationDeliveries_NotificationId",
                table: "NOTIFICATION_DELIVERIES",
                newName: "IX_NOTIFICATION_DELIVERIES_NOTIFICATION_ID");

            migrationBuilder.RenameIndex(
                name: "IX_NotificationDeliveries_DeliveredAt_NextAttemptAt",
                table: "NOTIFICATION_DELIVERIES",
                newName: "IX_NOTIFICATION_DELIVERIES_DELIVERED_AT_NEXT_ATTEMPT_AT");

            migrationBuilder.RenameColumn(
                name: "Fingerprint",
                table: "LEGAL_ENTITY_VERIFICATION_ORIGINS",
                newName: "FINGERPRINT");

            migrationBuilder.RenameColumn(
                name: "LegalEntityId",
                table: "LEGAL_ENTITY_VERIFICATION_ORIGINS",
                newName: "LEGAL_ENTITY_ID");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "LEGAL_ENTITIES",
                newName: "STATUS");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "LEGAL_ENTITIES",
                newName: "NAME");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "LEGAL_ENTITIES",
                newName: "ID");

            migrationBuilder.RenameColumn(
                name: "VerifiedBy",
                table: "LEGAL_ENTITIES",
                newName: "VERIFIED_BY");

            migrationBuilder.RenameColumn(
                name: "VerifiedAt",
                table: "LEGAL_ENTITIES",
                newName: "VERIFIED_AT");

            migrationBuilder.RenameColumn(
                name: "TaxNumber",
                table: "LEGAL_ENTITIES",
                newName: "TAX_NUMBER");

            migrationBuilder.RenameColumn(
                name: "RegistrationNumber",
                table: "LEGAL_ENTITIES",
                newName: "REGISTRATION_NUMBER");

            migrationBuilder.RenameColumn(
                name: "ModifiedBy",
                table: "LEGAL_ENTITIES",
                newName: "MODIFIED_BY");

            migrationBuilder.RenameColumn(
                name: "ModifiedAt",
                table: "LEGAL_ENTITIES",
                newName: "MODIFIED_AT");

            migrationBuilder.RenameColumn(
                name: "IsResident",
                table: "LEGAL_ENTITIES",
                newName: "IS_RESIDENT");

            migrationBuilder.RenameColumn(
                name: "IsLargeExposure",
                table: "LEGAL_ENTITIES",
                newName: "IS_LARGE_EXPOSURE");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "LEGAL_ENTITIES",
                newName: "IS_ACTIVE");

            migrationBuilder.RenameColumn(
                name: "GccNumber",
                table: "LEGAL_ENTITIES",
                newName: "GCC_NUMBER");

            migrationBuilder.RenameColumn(
                name: "GccName",
                table: "LEGAL_ENTITIES",
                newName: "GCC_NAME");

            migrationBuilder.RenameColumn(
                name: "FbaId",
                table: "LEGAL_ENTITIES",
                newName: "FBA_ID");

            migrationBuilder.RenameColumn(
                name: "DateTo",
                table: "LEGAL_ENTITIES",
                newName: "DATE_TO");

            migrationBuilder.RenameColumn(
                name: "DateFrom",
                table: "LEGAL_ENTITIES",
                newName: "DATE_FROM");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "LEGAL_ENTITIES",
                newName: "CREATED_BY");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "LEGAL_ENTITIES",
                newName: "CREATED_AT");

            migrationBuilder.RenameColumn(
                name: "ConnectedWithBank",
                table: "LEGAL_ENTITIES",
                newName: "CONNECTED_WITH_BANK");

            migrationBuilder.RenameColumn(
                name: "BasisOfConnection",
                table: "LEGAL_ENTITIES",
                newName: "BASIS_OF_CONNECTION");

            migrationBuilder.RenameColumn(
                name: "BankCustomerNumber",
                table: "LEGAL_ENTITIES",
                newName: "BANK_CUSTOMER_NUMBER");

            migrationBuilder.RenameIndex(
                name: "IX_LegalEntities_Status",
                table: "LEGAL_ENTITIES",
                newName: "IX_LEGAL_ENTITIES_STATUS");

            migrationBuilder.RenameIndex(
                name: "IX_LegalEntities_IsResident",
                table: "LEGAL_ENTITIES",
                newName: "IX_LEGAL_ENTITIES_IS_RESIDENT");

            migrationBuilder.RenameColumn(
                name: "Residency",
                table: "FAMILY_MEMBERS",
                newName: "RESIDENCY");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "FAMILY_MEMBERS",
                newName: "ID");

            migrationBuilder.RenameColumn(
                name: "RelatedPersonId",
                table: "FAMILY_MEMBERS",
                newName: "RELATED_PERSON_ID");

            migrationBuilder.RenameColumn(
                name: "PassportNumber",
                table: "FAMILY_MEMBERS",
                newName: "PASSPORT_NUMBER");

            migrationBuilder.RenameColumn(
                name: "ParentFamilyMemberId",
                table: "FAMILY_MEMBERS",
                newName: "PARENT_FAMILY_MEMBER_ID");

            migrationBuilder.RenameColumn(
                name: "ModifiedBy",
                table: "FAMILY_MEMBERS",
                newName: "MODIFIED_BY");

            migrationBuilder.RenameColumn(
                name: "ModifiedAt",
                table: "FAMILY_MEMBERS",
                newName: "MODIFIED_AT");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "FAMILY_MEMBERS",
                newName: "LAST_NAME");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "FAMILY_MEMBERS",
                newName: "IS_ACTIVE");

            migrationBuilder.RenameColumn(
                name: "FirstName",
                table: "FAMILY_MEMBERS",
                newName: "FIRST_NAME");

            migrationBuilder.RenameColumn(
                name: "FBAId",
                table: "FAMILY_MEMBERS",
                newName: "FBA_ID");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "FAMILY_MEMBERS",
                newName: "CREATED_BY");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "FAMILY_MEMBERS",
                newName: "CREATED_AT");

            migrationBuilder.RenameIndex(
                name: "IX_FamilyMembers_RelatedPersonId",
                table: "FAMILY_MEMBERS",
                newName: "IX_FAMILY_MEMBERS_RELATED_PERSON_ID");

            migrationBuilder.RenameIndex(
                name: "IX_FamilyMembers_ParentFamilyMemberId",
                table: "FAMILY_MEMBERS",
                newName: "IX_FAMILY_MEMBERS_PARENT_FAMILY_MEMBER_ID");

            migrationBuilder.RenameIndex(
                name: "IX_FamilyMembers_JMBG",
                table: "FAMILY_MEMBERS",
                newName: "IX_FAMILY_MEMBERS_JMBG");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "CODE_LIST_VALUES",
                newName: "NAME");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "CODE_LIST_VALUES",
                newName: "DESCRIPTION");

            migrationBuilder.RenameColumn(
                name: "Category",
                table: "CODE_LIST_VALUES",
                newName: "CATEGORY");

            migrationBuilder.RenameColumn(
                name: "ValueKey",
                table: "CODE_LIST_VALUES",
                newName: "VALUE_KEY");

            migrationBuilder.RenameColumn(
                name: "ModifiedBy",
                table: "CODE_LIST_VALUES",
                newName: "MODIFIED_BY");

            migrationBuilder.RenameColumn(
                name: "ModifiedAt",
                table: "CODE_LIST_VALUES",
                newName: "MODIFIED_AT");

            migrationBuilder.RenameColumn(
                name: "IsSystemRequired",
                table: "CODE_LIST_VALUES",
                newName: "IS_SYSTEM_REQUIRED");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "CODE_LIST_VALUES",
                newName: "IS_ACTIVE");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "CODE_LIST_VALUES",
                newName: "CREATED_BY");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "CODE_LIST_VALUES",
                newName: "CREATED_AT");

            migrationBuilder.RenameIndex(
                name: "IX_CodeListValues_Category_ValueKey",
                table: "CODE_LIST_VALUES",
                newName: "IX_CODE_LIST_VALUES_CATEGORY_VALUE_KEY");

            migrationBuilder.RenameIndex(
                name: "IX_CodeListValues_Category",
                table: "CODE_LIST_VALUES",
                newName: "IX_CODE_LIST_VALUES_CATEGORY");

            migrationBuilder.RenameColumn(
                name: "Version",
                table: "CODELIST_DEFINITIONS",
                newName: "VERSION");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "CODELIST_DEFINITIONS",
                newName: "NAME");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "CODELIST_DEFINITIONS",
                newName: "DESCRIPTION");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "CODELIST_DEFINITIONS",
                newName: "ID");

            migrationBuilder.RenameColumn(
                name: "ModifiedBy",
                table: "CODELIST_DEFINITIONS",
                newName: "MODIFIED_BY");

            migrationBuilder.RenameColumn(
                name: "ModifiedAt",
                table: "CODELIST_DEFINITIONS",
                newName: "MODIFIED_AT");

            migrationBuilder.RenameColumn(
                name: "IsSystemRequired",
                table: "CODELIST_DEFINITIONS",
                newName: "IS_SYSTEM_REQUIRED");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "CODELIST_DEFINITIONS",
                newName: "IS_ACTIVE");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "CODELIST_DEFINITIONS",
                newName: "CREATED_BY");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "CODELIST_DEFINITIONS",
                newName: "CREATED_AT");

            migrationBuilder.RenameIndex(
                name: "IX_CodeListDefinitions_Name",
                table: "CODELIST_DEFINITIONS",
                newName: "IX_CODELIST_DEFINITIONS_NAME");

            migrationBuilder.RenameColumn(
                name: "Timestamp",
                table: "AUDIT_LOGS",
                newName: "TIMESTAMP");

            migrationBuilder.RenameColumn(
                name: "Action",
                table: "AUDIT_LOGS",
                newName: "ACTION");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "AUDIT_LOGS",
                newName: "ID");

            migrationBuilder.RenameColumn(
                name: "Username",
                table: "AUDIT_LOGS",
                newName: "USER");

            migrationBuilder.RenameColumn(
                name: "TableName",
                table: "AUDIT_LOGS",
                newName: "TABLE_NAME");

            migrationBuilder.RenameColumn(
                name: "OldValues",
                table: "AUDIT_LOGS",
                newName: "OLD_VALUES");

            migrationBuilder.RenameColumn(
                name: "NewValues",
                table: "AUDIT_LOGS",
                newName: "NEW_VALUES");

            migrationBuilder.RenameColumn(
                name: "Username",
                table: "APP_USERS",
                newName: "USERNAME");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "APP_USERS",
                newName: "EMAIL");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "APP_USERS",
                newName: "ID");

            migrationBuilder.RenameColumn(
                name: "ModifiedBy",
                table: "APP_USERS",
                newName: "MODIFIED_BY");

            migrationBuilder.RenameColumn(
                name: "ModifiedAt",
                table: "APP_USERS",
                newName: "MODIFIED_AT");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "APP_USERS",
                newName: "LAST_NAME");

            migrationBuilder.RenameColumn(
                name: "KeycloakSubjectConfirmed",
                table: "APP_USERS",
                newName: "LOGIN_CONFIRMED");

            migrationBuilder.RenameColumn(
                name: "KeycloakId",
                table: "APP_USERS",
                newName: "KEYCLOAK_ID");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "APP_USERS",
                newName: "IS_ACTIVE");

            migrationBuilder.RenameColumn(
                name: "FirstName",
                table: "APP_USERS",
                newName: "FIRST_NAME");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "APP_USERS",
                newName: "CREATED_BY");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "APP_USERS",
                newName: "CREATED_AT");

            migrationBuilder.AddColumn<long>(
                name: "CAPITAL_ID",
                table: "LIMITS",
                type: "bigint",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 3);

            migrationBuilder.AddColumn<DateTime>(
                name: "VALID_TILL",
                table: "CAPITALS",
                type: "date",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified))
                .Annotation("Relational:ColumnOrder", 2);

            RebuildOrderedTables(migrationBuilder);
            ConsolidateCapitalAndLimits(migrationBuilder);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ROLES",
                table: "ROLES",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_REPORTS",
                table: "REPORTS",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PERMISSIONS",
                table: "PERMISSIONS",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_NOTIFICATIONS",
                table: "NOTIFICATIONS",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LIMITS",
                table: "LIMITS",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CAPITALS",
                table: "CAPITALS",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_USER_ROLES",
                table: "USER_ROLES",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UNLOCK_REQUESTS",
                table: "UNLOCK_REQUESTS",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ROLE_REQUESTS",
                table: "ROLE_REQUESTS",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ROLE_PERMISSIONS",
                table: "ROLE_PERMISSIONS",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_REQUESTED_ROLE",
                table: "REQUESTED_ROLE",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RELATED_PERSONS",
                table: "RELATED_PERSONS",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PERIOD_LOCKS",
                table: "PERIOD_LOCKS",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_NOTIFICATION_DELIVERIES",
                table: "NOTIFICATION_DELIVERIES",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LEGAL_ENTITY_VERIFICATION_ORIGINS",
                table: "LEGAL_ENTITY_VERIFICATION_ORIGINS",
                column: "LEGAL_ENTITY_ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LEGAL_ENTITIES",
                table: "LEGAL_ENTITIES",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FAMILY_MEMBERS",
                table: "FAMILY_MEMBERS",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CODE_LIST_VALUES",
                table: "CODE_LIST_VALUES",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CODELIST_DEFINITIONS",
                table: "CODELIST_DEFINITIONS",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AUDIT_LOGS",
                table: "AUDIT_LOGS",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_APP_USERS",
                table: "APP_USERS",
                column: "ID");

            migrationBuilder.CreateTable(
                name: "CODE_LIST_LOCALIZATION",
                columns: table => new
                {
                    CODE_LIST_ID = table.Column<int>(type: "int", nullable: false),
                    CATEGORY = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false, collation: "Latin1_General_100_CI_AS"),
                    LANGUAGE_CODE = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    RESOURCE_KEY = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, collation: "Latin1_General_100_CI_AS"),
                    RESOURCE_LABEL = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RESOURCE_HELPER = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RESOURCE_PLACEHOLDER = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CODE_LIST_LOCALIZATION", x => x.CODE_LIST_ID);
                    table.ForeignKey(
                        name: "FK_CODE_LIST_LOCALIZATION_CODE_LIST_VALUES_CODE_LIST_ID",
                        column: x => x.CODE_LIST_ID,
                        principalTable: "CODE_LIST_VALUES",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            RestoreLocalizedValues(migrationBuilder);
            SeedCapitalLabels(migrationBuilder);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Roles_Name_Trimmed",
                table: "ROLES",
                sql: "DATALENGTH([NAME]) = DATALENGTH(LTRIM(RTRIM([NAME])))");

            migrationBuilder.CreateIndex(
                name: "IX_UNLOCK_REQUESTS_PARENT_REQUEST_ID",
                table: "UNLOCK_REQUESTS",
                column: "PARENT_REQUEST_ID",
                unique: true,
                filter: "[PARENT_REQUEST_ID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ROLE_REQUESTS_REQUESTER_SUBJECT",
                table: "ROLE_REQUESTS",
                column: "REQUESTER_SUBJECT",
                unique: true,
                filter: "[STATUS] = N'pending'");

            migrationBuilder.CreateIndex(
                name: "IX_RELATED_PERSONS_FBA_ID",
                table: "RELATED_PERSONS",
                column: "FBA_ID",
                unique: true,
                filter: "[IS_ACTIVE] = 1 AND [FBA_ID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RELATED_PERSONS_JMBG",
                table: "RELATED_PERSONS",
                column: "JMBG",
                unique: true,
                filter: "[IS_ACTIVE] = 1 AND [JMBG] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RELATED_PERSONS_PASSPORT_NUMBER",
                table: "RELATED_PERSONS",
                column: "PASSPORT_NUMBER",
                unique: true,
                filter: "[IS_ACTIVE] = 1 AND [PASSPORT_NUMBER] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RelatedPersons_FBAId_Trimmed",
                table: "RELATED_PERSONS",
                sql: "DATALENGTH([FBA_ID]) = DATALENGTH(LTRIM(RTRIM([FBA_ID])))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RelatedPersons_PassportNumber_Trimmed",
                table: "RELATED_PERSONS",
                sql: "DATALENGTH([PASSPORT_NUMBER]) = DATALENGTH(LTRIM(RTRIM([PASSPORT_NUMBER])))");

            migrationBuilder.CreateIndex(
                name: "IX_LEGAL_ENTITIES_FBA_ID",
                table: "LEGAL_ENTITIES",
                column: "FBA_ID",
                unique: true,
                filter: "\"FBA_ID\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LEGAL_ENTITIES_TAX_NUMBER",
                table: "LEGAL_ENTITIES",
                column: "TAX_NUMBER",
                unique: true,
                filter: "\"TAX_NUMBER\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CodeListValues_Category_Trimmed",
                table: "CODE_LIST_VALUES",
                sql: "DATALENGTH([CATEGORY]) = DATALENGTH(LTRIM(RTRIM([CATEGORY])))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CodeListValues_ValueKey_Trimmed",
                table: "CODE_LIST_VALUES",
                sql: "DATALENGTH([VALUE_KEY]) = DATALENGTH(LTRIM(RTRIM([VALUE_KEY])))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CodeListDefinitions_Name_Trimmed",
                table: "CODELIST_DEFINITIONS",
                sql: "DATALENGTH([NAME]) = DATALENGTH(LTRIM(RTRIM([NAME])))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AppUsers_Email_Trimmed",
                table: "APP_USERS",
                sql: "DATALENGTH([EMAIL]) = DATALENGTH(LTRIM(RTRIM([EMAIL])))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AppUsers_Username_Trimmed",
                table: "APP_USERS",
                sql: "DATALENGTH([USERNAME]) = DATALENGTH(LTRIM(RTRIM([USERNAME])))");

            migrationBuilder.CreateIndex(
                name: "IX_CODE_LIST_LOCALIZATION_CATEGORY_LANGUAGE_CODE_RESOURCE_KEY",
                table: "CODE_LIST_LOCALIZATION",
                columns: new[] { "CATEGORY", "LANGUAGE_CODE", "RESOURCE_KEY" },
                unique: true,
                filter: "[LANGUAGE_CODE] IS NOT NULL AND [RESOURCE_KEY] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_FAMILY_MEMBERS_FAMILY_MEMBERS_PARENT_FAMILY_MEMBER_ID",
                table: "FAMILY_MEMBERS",
                column: "PARENT_FAMILY_MEMBER_ID",
                principalTable: "FAMILY_MEMBERS",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FAMILY_MEMBERS_RELATED_PERSONS_RELATED_PERSON_ID",
                table: "FAMILY_MEMBERS",
                column: "RELATED_PERSON_ID",
                principalTable: "RELATED_PERSONS",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LEGAL_ENTITY_VERIFICATION_ORIGINS_LEGAL_ENTITIES_LEGAL_ENTITY_ID",
                table: "LEGAL_ENTITY_VERIFICATION_ORIGINS",
                column: "LEGAL_ENTITY_ID",
                principalTable: "LEGAL_ENTITIES",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LIMITS_CAPITALS_CAPITAL_ID",
                table: "LIMITS",
                column: "CAPITAL_ID",
                principalTable: "CAPITALS",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LIMITS_LEGAL_ENTITIES_LEGAL_ENTITY_ID",
                table: "LIMITS",
                column: "LEGAL_ENTITY_ID",
                principalTable: "LEGAL_ENTITIES",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NOTIFICATION_DELIVERIES_NOTIFICATIONS_NOTIFICATION_ID",
                table: "NOTIFICATION_DELIVERIES",
                column: "NOTIFICATION_ID",
                principalTable: "NOTIFICATIONS",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RELATED_PERSONS_RELATED_PERSONS_RELATED_TO_PERSON_ID",
                table: "RELATED_PERSONS",
                column: "RELATED_TO_PERSON_ID",
                principalTable: "RELATED_PERSONS",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_REQUESTED_ROLE_ROLES_ROLE_ID",
                table: "REQUESTED_ROLE",
                column: "ROLE_ID",
                principalTable: "ROLES",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_REQUESTED_ROLE_ROLE_REQUESTS_REQUEST_ID",
                table: "REQUESTED_ROLE",
                column: "REQUEST_ID",
                principalTable: "ROLE_REQUESTS",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ROLE_PERMISSIONS_PERMISSIONS_PERMISSION_ID",
                table: "ROLE_PERMISSIONS",
                column: "PERMISSION_ID",
                principalTable: "PERMISSIONS",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ROLE_PERMISSIONS_ROLES_ROLE_ID",
                table: "ROLE_PERMISSIONS",
                column: "ROLE_ID",
                principalTable: "ROLES",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_USER_ROLES_APP_USERS_USER_ID",
                table: "USER_ROLES",
                column: "USER_ID",
                principalTable: "APP_USERS",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_USER_ROLES_ROLES_ROLE_ID",
                table: "USER_ROLES",
                column: "ROLE_ID",
                principalTable: "ROLES",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
            CreateOrderedTableIndexes(migrationBuilder);
            RecreateDetailsViews(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder) =>
            throw new NotSupportedException("Restore a pre-migration database backup to recover the original schema.");
    }
}
