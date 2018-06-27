USE [imageuploader]
GO

/****** Object: Table [dbo].[UploadTracking] Script Date: 5/19/2017 12:53:53 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[UploadTracking] (
    [Id]                  NVARCHAR (128) NOT NULL,
    [FileName]            NVARCHAR (500) NOT NULL,
    [UploadEndPoint]      NVARCHAR (500) NOT NULL,
    [StartTimeUtc]        DATETIME       NOT NULL,
    [EndTimeUtc]          DATETIME       NOT NULL,
    [Status]              NVARCHAR (20)  NOT NULL,
    [ErrorMessage]        NVARCHAR (MAX) NULL,
    [MachineName]         NVARCHAR (50)  NULL,
    [SourceMachineName]   NVARCHAR (50)  NULL,
    [FileCreationTimeUtc] DATETIME       NOT NULL,
    [IsRetake]            BIT            NULL
);


