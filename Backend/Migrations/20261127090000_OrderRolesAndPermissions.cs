using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RBBH.ConnectedParties.DL.Persistence;

namespace RBBH.ConnectedParties.Api.Migrations;

[DbContext(typeof(ConnectedPartiesDbContext))]
[Migration("20261127090000_OrderRolesAndPermissions")]
public sealed class OrderRolesAndPermissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE dbo.ROLES_REORDERED (
                ID bigint NOT NULL,
                IS_ACTIVE bit NOT NULL,
                NAME nvarchar(100) COLLATE Latin1_General_100_CI_AS NOT NULL,
                CREATED_BY nvarchar(max) NOT NULL,
                CREATED_AT datetime2 NOT NULL,
                MODIFIED_BY nvarchar(max) NULL,
                MODIFIED_AT datetime2 NULL
            );

            CREATE TABLE dbo.PERMISSIONS_REORDERED (
                ID bigint NOT NULL,
                IS_ACTIVE bit NOT NULL,
                MODULE nvarchar(50) NOT NULL,
                CODE nvarchar(100) NOT NULL
            );

            INSERT dbo.ROLES_REORDERED (ID, IS_ACTIVE, NAME, CREATED_BY, CREATED_AT, MODIFIED_BY, MODIFIED_AT)
            SELECT ID, IS_ACTIVE, NAME, CREATED_BY, CREATED_AT, MODIFIED_BY, MODIFIED_AT
            FROM dbo.ROLES WITH (TABLOCKX, HOLDLOCK);

            INSERT dbo.PERMISSIONS_REORDERED (ID, IS_ACTIVE, MODULE, CODE)
            SELECT ID, IS_ACTIVE, MODULE, CODE
            FROM dbo.PERMISSIONS WITH (TABLOCKX, HOLDLOCK);

            ALTER TABLE dbo.REQUESTED_ROLE DROP CONSTRAINT FK_REQUESTED_ROLE_ROLES_ROLE_ID;
            ALTER TABLE dbo.USER_ROLES DROP CONSTRAINT FK_USER_ROLES_ROLES_ROLE_ID;
            ALTER TABLE dbo.ROLE_PERMISSIONS DROP CONSTRAINT FK_ROLE_PERMISSIONS_ROLES_ROLE_ID,
                FK_ROLE_PERMISSIONS_PERMISSIONS_PERMISSION_ID;

            DROP TABLE dbo.ROLES;
            DROP TABLE dbo.PERMISSIONS;
            EXEC sys.sp_rename N'dbo.ROLES_REORDERED', N'ROLES';
            EXEC sys.sp_rename N'dbo.PERMISSIONS_REORDERED', N'PERMISSIONS';

            ALTER TABLE dbo.ROLES ADD CONSTRAINT PK_ROLES PRIMARY KEY CLUSTERED (ID),
                CONSTRAINT CK_Roles_Name_Trimmed CHECK (DATALENGTH(NAME) = DATALENGTH(LTRIM(RTRIM(NAME))));
            CREATE UNIQUE INDEX IX_ROLES_NAME ON dbo.ROLES (NAME);

            ALTER TABLE dbo.PERMISSIONS ADD CONSTRAINT PK_PERMISSIONS PRIMARY KEY NONCLUSTERED (ID);
            CREATE UNIQUE CLUSTERED INDEX IX_PERMISSIONS_MODULE_CODE ON dbo.PERMISSIONS (MODULE, CODE);
            CREATE UNIQUE INDEX IX_PERMISSIONS_CODE ON dbo.PERMISSIONS (CODE);

            ALTER TABLE dbo.REQUESTED_ROLE ADD CONSTRAINT FK_REQUESTED_ROLE_ROLES_ROLE_ID
                FOREIGN KEY (ROLE_ID) REFERENCES dbo.ROLES (ID);
            ALTER TABLE dbo.USER_ROLES ADD CONSTRAINT FK_USER_ROLES_ROLES_ROLE_ID
                FOREIGN KEY (ROLE_ID) REFERENCES dbo.ROLES (ID);
            ALTER TABLE dbo.ROLE_PERMISSIONS ADD CONSTRAINT FK_ROLE_PERMISSIONS_ROLES_ROLE_ID
                FOREIGN KEY (ROLE_ID) REFERENCES dbo.ROLES (ID) ON DELETE CASCADE;
            ALTER TABLE dbo.ROLE_PERMISSIONS ADD CONSTRAINT FK_ROLE_PERMISSIONS_PERMISSIONS_PERMISSION_ID
                FOREIGN KEY (PERMISSION_ID) REFERENCES dbo.PERMISSIONS (ID) ON DELETE CASCADE;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("Restore a backup to recover the removed role fields.");
}
