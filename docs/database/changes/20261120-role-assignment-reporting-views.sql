-- Apply through the existing database change workflow.
-- These views present current names without copying them into assignment records.
CREATE OR ALTER VIEW dbo.RolePermissionDetails
AS
SELECT rp.RoleId,
       r.Name AS RoleName,
       rp.PermissionId,
       p.Code AS PermissionCode,
       p.Module AS PermissionModule
FROM dbo.RolePermissions AS rp
INNER JOIN dbo.Roles AS r ON r.Id = rp.RoleId
INNER JOIN dbo.Permissions AS p ON p.Id = rp.PermissionId;
GO

CREATE OR ALTER VIEW dbo.UserRoleDetails
AS
SELECT ur.Id,
       ur.UserId,
       u.Username,
       ur.RoleId,
       r.Name AS RoleName,
       ur.IsActive,
       ur.CreatedAt,
       ur.CreatedBy
FROM dbo.UserRoles AS ur
INNER JOIN dbo.AppUsers AS u ON u.Id = ur.UserId
INNER JOIN dbo.Roles AS r ON r.Id = ur.RoleId;
GO
