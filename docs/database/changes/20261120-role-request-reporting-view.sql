-- Read-only projection of requested roles with the parent request details.
CREATE OR ALTER VIEW dbo.RoleRequestDetails
AS
SELECT request.Id,
       request.Username AS RequestedBy,
       request.CreatedAt AS RequestedAt,
       role.Id AS RoleId,
       role.Name AS RoleName,
       request.Status,
       request.Note,
       request.ProcessedAt
FROM dbo.RoleRequests AS request
INNER JOIN dbo.RequestedRole AS requested ON requested.RequestId = request.Id
INNER JOIN dbo.Roles AS role ON role.Id = requested.RoleId;
GO
