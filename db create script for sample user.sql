CREATE PROCEDURE GetUserByUserName
    @Username NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ClientID, UserID, Password
    FROM [dbo].clientuser
    WHERE UserID = @Username and UserStatus=1
END;
GO