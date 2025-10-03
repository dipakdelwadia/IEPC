/*************************************************************************************
Usage:

    EXEC SP_GetReportByTableName
    @TableName = 'Client',
    @PageSize = '',
    @PageNo = ''

*************************************************************************************/
CREATE OR ALTER PROCEDURE SP_GetReportByTableName
    @TableName NVARCHAR(128),
    @ClientId NVARCHAR(128) = NULL,
    @PageSize NVARCHAR(10) = NULL,
    @PageNo NVARCHAR(10) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Query NVARCHAR(MAX);

    IF LEN(@PageSize) = 0  -- No paging 
        SET @Query = N'SELECT * FROM ' + @TableName
    ELSE IF LEN(@PageNo) =  0 -- No PageNo
    BEGIN
        SET @Query = N'SELECT TOP (' + @PageSize + ') * 
                    FROM ' + @TableName + ' 
                    ORDER BY 1 ASC '
    END
    ELSE BEGIN
	  -- Paging with PageSize and PageNo
        SET @Query = N'SELECT * 
                    FROM ' + @TableName + ' 
                    ORDER BY 1 ASC 
                    OFFSET ' + CONVERT(NVARCHAR(20), (CONVERT(INT, @PageNo) - 1) * CONVERT(INT, @PageSize)) + ' ROWS 
                    FETCH NEXT ' + @PageSize + ' ROWS ONLY '
    END

    --Result
    EXEC(@Query)
END;
GO
