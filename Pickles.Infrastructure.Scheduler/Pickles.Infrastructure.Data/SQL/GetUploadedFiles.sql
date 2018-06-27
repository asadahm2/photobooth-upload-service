USE [imageuploader]
GO

/****** Object: SqlProcedure [dbo].[GetUploadedFiles] Script Date: 5/19/2017 12:55:53 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE Procedure GetUploadedFiles
(
@sourceMachineName nvarchar(50),
@filePath nvarchar(100)
)
AS
BEGIN
Select * From UploadTracking Where SourceMachineName = @sourceMachineName And FileName Like @filePath + '%'
END
