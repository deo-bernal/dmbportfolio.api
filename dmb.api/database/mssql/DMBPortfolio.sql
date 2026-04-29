-- Idempotent SQL Server script for DMBPortfolio
-- Safe to run multiple times against a SQL Server database.
-- Create the database first (run as a user with CREATE DATABASE permission) or remove the CREATE DATABASE block
-- if you prefer to create the database from your tooling.

IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'DMBPortfolio')
BEGIN
    CREATE DATABASE [DMBPortfolio];
END
GO

USE [DMBPortfolio];
GO

-- 1) Ensure tables exist
IF OBJECT_ID(N'dbo.[User]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[User]
    (
        UserId INT IDENTITY(1,1) PRIMARY KEY,
        Username NVARCHAR(100) NOT NULL UNIQUE,
        Password NVARCHAR(255) NOT NULL,
        FirstName NVARCHAR(100) NOT NULL,
        LastName NVARCHAR(100) NOT NULL,
        Email NVARCHAR(255) NOT NULL UNIQUE,
        ContactNo NVARCHAR(30) NULL,
        CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
    );
END
GO

IF OBJECT_ID(N'dbo.UserDetails', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserDetails
    (
        UserDetailsId INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NOT NULL,
        Description NVARCHAR(MAX) NULL,
        Skills NVARCHAR(MAX) NULL,
        CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        CONSTRAINT FK_UserDetails_User FOREIGN KEY (UserId) REFERENCES dbo.[User](UserId) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID(N'dbo.Project', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Project
    (
        ProjectId INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Type NVARCHAR(100) NOT NULL,
        ProjectDetails NVARCHAR(MAX) NULL,
        CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        CONSTRAINT FK_Project_User FOREIGN KEY (UserId) REFERENCES dbo.[User](UserId) ON DELETE CASCADE
    );
END
GO

-- 2) Ensure unique/index constraints exist
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_UserDetails_UserId' AND object_id = OBJECT_ID('dbo.UserDetails'))
BEGIN
    CREATE UNIQUE INDEX UX_UserDetails_UserId ON dbo.UserDetails(UserId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Project_User_Name_Type' AND object_id = OBJECT_ID('dbo.Project'))
BEGIN
    CREATE UNIQUE INDEX UQ_Project_User_Name_Type ON dbo.Project(UserId, Name, Type);
END
GO

-- 3) Seed Users (idempotent using MERGE)
MERGE dbo.[User] AS target
USING (VALUES
    ('deobernal', 'Password@123', 'Deo', 'Bernal', 'deo.bernal@example.com', '+1 555 0100'),
    ('portfolioadmin', 'Admin@123', 'Portfolio', 'Admin', 'admin.dmb@example.com', '+1 555 0101')
) AS src(Username, [Password], FirstName, LastName, Email, ContactNo)
ON target.Username = src.Username
WHEN MATCHED THEN
    UPDATE SET [Password] = src.[Password], FirstName = src.FirstName, LastName = src.LastName, Email = src.Email, ContactNo = src.ContactNo
WHEN NOT MATCHED THEN
    INSERT (Username, [Password], FirstName, LastName, Email, ContactNo)
    VALUES (src.Username, src.[Password], src.FirstName, src.LastName, src.Email, src.ContactNo)
;
GO

-- 4) Seed UserDetails (idempotent per UserId)
DECLARE @UserId_Deo INT = (SELECT UserId FROM dbo.[User] WHERE Username = 'deobernal');
IF @UserId_Deo IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.UserDetails WHERE UserId = @UserId_Deo)
        UPDATE dbo.UserDetails
        SET Description = 'Sign in to view Deo Bernal''s Portfolio. Full-stack developer profile with intro video, projects, and contact details.',
            Skills = 'React,.NET,TypeScript,JavaScript,HTML,CSS,Bootstrap,REST API,PostgreSQL,Git,Azure'
        WHERE UserId = @UserId_Deo;
    ELSE
        INSERT INTO dbo.UserDetails (UserId, Description, Skills)
        VALUES (@UserId_Deo, 'Sign in to view Deo Bernal''s Portfolio. Full-stack developer profile with intro video, projects, and contact details.', 'React,.NET,TypeScript,JavaScript,HTML,CSS,Bootstrap,REST API,PostgreSQL,Git,Azure');
END

DECLARE @UserId_Admin INT = (SELECT UserId FROM dbo.[User] WHERE Username = 'portfolioadmin');
IF @UserId_Admin IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.UserDetails WHERE UserId = @UserId_Admin)
        UPDATE dbo.UserDetails
        SET Description = 'Portfolio administration account for maintaining profile and project content.',
            Skills = 'Administration,Content Management,SQL,Security'
        WHERE UserId = @UserId_Admin;
    ELSE
        INSERT INTO dbo.UserDetails (UserId, Description, Skills)
        VALUES (@UserId_Admin, 'Portfolio administration account for maintaining profile and project content.', 'Administration,Content Management,SQL,Security');
END
GO

-- 5) Seed Projects (idempotent by unique UserId+Name+Type)
-- Helper MERGE per project
DECLARE @uid INT;

SET @uid = (SELECT UserId FROM dbo.[User] WHERE Username = 'deobernal');
IF @uid IS NOT NULL
BEGIN
    MERGE dbo.Project AS target
    USING (VALUES (@uid, 'DMB Portfolio Web', 'Web Application', 'React,.NET,TypeScript,Bootstrap,Responsive UI,Authentication'))
    AS src(UserId, Name, Type, ProjectDetails)
    ON target.UserId = src.UserId AND target.Name = src.Name AND target.Type = src.Type
    WHEN MATCHED THEN UPDATE SET ProjectDetails = src.ProjectDetails
    WHEN NOT MATCHED THEN INSERT (UserId, Name, Type, ProjectDetails) VALUES (src.UserId, src.Name, src.Type, src.ProjectDetails);

    MERGE dbo.Project AS target
    USING (VALUES (@uid, 'Portfolio API', 'Backend API', 'ASP.NET Core,JWT Login,Profile Endpoint,Project Endpoint'))
    AS src(UserId, Name, Type, ProjectDetails)
    ON target.UserId = src.UserId AND target.Name = src.Name AND target.Type = src.Type
    WHEN MATCHED THEN UPDATE SET ProjectDetails = src.ProjectDetails
    WHEN NOT MATCHED THEN INSERT (UserId, Name, Type, ProjectDetails) VALUES (src.UserId, src.Name, src.Type, src.ProjectDetails);

    MERGE dbo.Project AS target
    USING (VALUES (@uid, 'Skills and Projects Showcase', 'Portfolio Module', 'Skills section,Projects section,Contact section,Intro video link'))
    AS src(UserId, Name, Type, ProjectDetails)
    ON target.UserId = src.UserId AND target.Name = src.Name AND target.Type = src.Type
    WHEN MATCHED THEN UPDATE SET ProjectDetails = src.ProjectDetails
    WHEN NOT MATCHED THEN INSERT (UserId, Name, Type, ProjectDetails) VALUES (src.UserId, src.Name, src.Type, src.ProjectDetails);
END

SET @uid = (SELECT UserId FROM dbo.[User] WHERE Username = 'portfolioadmin');
IF @uid IS NOT NULL
BEGIN
    MERGE dbo.Project AS target
    USING (VALUES (@uid, 'Admin Console', 'Internal Tool', 'User management,Content updates,Audit logs'))
    AS src(UserId, Name, Type, ProjectDetails)
    ON target.UserId = src.UserId AND target.Name = src.Name AND target.Type = src.Type
    WHEN MATCHED THEN UPDATE SET ProjectDetails = src.ProjectDetails
    WHEN NOT MATCHED THEN INSERT (UserId, Name, Type, ProjectDetails) VALUES (src.UserId, src.Name, src.Type, src.ProjectDetails);
END
GO

-- 6) Verification queries
SELECT * FROM dbo.[User] ORDER BY UserId;
SELECT * FROM dbo.UserDetails ORDER BY UserDetailsId;
SELECT * FROM dbo.Project ORDER BY ProjectId;
GO
